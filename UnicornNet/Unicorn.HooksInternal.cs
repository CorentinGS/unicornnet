using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnicornNet;

public partial class Unicorn
{
    private const HookType EventMemoryHookMask = HookType.MemReadUnmapped
                                                 | HookType.MemWriteUnmapped
                                                 | HookType.MemFetchUnmapped
                                                 | HookType.MemReadProt
                                                 | HookType.MemWriteProt
                                                 | HookType.MemFetchProt;

    private readonly ConcurrentDictionary<MemoryAccessType, ConcurrentDictionary<nuint, byte>> _eventMemRegistrations = new();
    private readonly ConcurrentDictionary<nuint, HookRegistration> _hookRegistry = new();

    internal bool TrySimulateHook(HookHandle handle, ulong address, int size)
    {
        if (!TryGetRegistration(handle, out var registration))
        {
            return false;
        }

        if (registration.Category is not (HookCategory.Code or HookCategory.Block))
            return false;

        registration.InvokeCode(address, size);
        return true;
    }

    internal bool TrySimulateMemoryHook(HookHandle handle, MemoryAccessType accessType, ulong address, int size, long value)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Memory)
        {
            return false;
        }

        registration.InvokeMemory(accessType, address, size, value);
        return true;
    }

    internal bool TrySimulateEventMem(MemoryAccessType accessType, ulong address, int size, long value)
    {
        if (!_eventMemRegistrations.TryGetValue(accessType, out var handles) || handles.IsEmpty)
        {
            return false;
        }

        var invoked = false;
        foreach (var handleEntry in handles)
        {
            if (!_hookRegistry.TryGetValue(handleEntry.Key, out var registration))
                continue;
            invoked = true;
            if (!registration.InvokeEventMemory(accessType, address, size, value))
            {
                break;
            }
        }

        return invoked;
    }

    internal bool TrySimulateInterruptHook(HookHandle handle, uint interruptNumber)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Interrupt)
        {
            return false;
        }

        registration.InvokeInterrupt(interruptNumber);
        return true;
    }

    internal bool TrySimulateInHook(HookHandle handle, uint port, int size, out uint value)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.In)
        {
            value = 0;
            return false;
        }

        value = registration.InvokeIn(port, size);
        return true;
    }

    internal bool TrySimulateOutHook(HookHandle handle, uint port, int size, uint value)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Out)
        {
            return false;
        }

        registration.InvokeOut(port, size, value);
        return true;
    }

    internal bool TrySimulateSyscallHook(HookHandle handle)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Syscall)
        {
            return false;
        }

        registration.InvokeSyscall();
        return true;
    }

    internal bool TrySimulateInvalidInstructionHook(HookHandle handle, out bool continueExecution)
    {
        continueExecution = false;
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.InvalidInstruction)
        {
            return false;
        }

        continueExecution = registration.InvokeInvalidInstruction();
        return true;
    }

    private HookHandle RegisterHook(HookType type, Delegate callback, object? state, HookRange? range)
    {
        var normalizedRange = NormalizeRange(range);
        var category = type switch
        {
            HookType.Code => HookCategory.Code,
            HookType.Block => HookCategory.Block,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        var registration = new HookRegistration(this, type, category, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAdd(engine, type, HookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterMemoryHook(HookType hookType, MemoryAccessType accessType, MemoryHook callback, HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, hookType, HookCategory.Memory, callback, state, accessType);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddMem(engine, hookType, MemHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterEventMemHook(HookType eventTypes, MemoryEventHook callback, HookRange? range, object? state)
    {
        var normalizedHookTypes = NormalizeEventHookTypes(eventTypes);
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, normalizedHookTypes, HookCategory.EventMemory, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddEventMem(engine, normalizedHookTypes, EventMemHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterInterruptHook(InterruptHook callback, HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, HookType.Interrupt, HookCategory.Interrupt, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddInterrupt(engine, HookType.Interrupt, InterruptHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterInHook(InHook callback, HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, HookType.Instruction, HookCategory.In, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddInstructionIn(engine, HookType.Instruction, InstructionInHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, X86InstructionIn, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterOutHook(OutHook callback, HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, HookType.Instruction, HookCategory.Out, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddInstructionOut(engine, HookType.Instruction, InstructionOutHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, X86InstructionOut, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterSyscallHook(SyscallHook callback, HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, HookType.Instruction, HookCategory.Syscall, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddInstructionSyscall(engine, HookType.Instruction, SyscallHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, X86InstructionSyscall, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterInvalidInstructionHook(InvalidInstructionHook callback, HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, HookType.InvalidInstruction, HookCategory.InvalidInstruction, callback, state);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddInvalidInstruction(engine, HookType.InvalidInstruction, InvalidInstructionHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
                return (err, hookId);
            });
    }

    private HookHandle RegisterHookInternal(HookRegistration registration, HookRange range, Func<IntPtr, HookRange, (int Error, nuint Handle)> registrar)
    {
        EnsureNotDisposed();

        var engine = EngineHandle;
        var (error, handle) = registrar(engine, range);
        if (error != 0)
        {
            registration.Dispose();
            throw new UnicornHookException((ErrorCode)error, "uc_hook_add");
        }

        registration.SetHandle(new HookHandle(handle));
        AddRegistration(registration);
        return registration.Handle;
    }

    private static HookRange NormalizeRange(HookRange? range)
    {
        return range ?? HookRange.All;
    }

    private bool TryGetRegistration(HookHandle handle, [NotNullWhen(true)] out HookRegistration? registration)
    {
        if (!handle.IsEmpty)
            return _hookRegistry.TryGetValue(handle.Value, out registration);
        registration = null;
        return false;
    }

    private void AddRegistration(HookRegistration registration)
    {
        _hookRegistry.TryAdd(registration.Handle.Value, registration);

        if (registration.Category != HookCategory.EventMemory)
            return;

        foreach (var accessType in GetEventAccessTypes(registration))
        {
            var handles = _eventMemRegistrations.GetOrAdd(accessType, _ => new ConcurrentDictionary<nuint, byte>());
            handles.TryAdd(registration.Handle.Value, 0);
        }
    }

    private void RemoveEventRegistration(HookRegistration registration)
    {
        if (registration.Category != HookCategory.EventMemory)
        {
            return;
        }

        foreach (var accessType in GetEventAccessTypes(registration))
        {
            if (_eventMemRegistrations.TryGetValue(accessType, out var handles))
            {
                handles.TryRemove(registration.Handle.Value, out _);
            }
        }
    }

    private static IEnumerable<MemoryAccessType> GetEventAccessTypes(HookRegistration registration)
    {
        if (registration.AccessType.HasValue)
        {
            yield return registration.AccessType.Value;
            yield break;
        }

        if ((registration.Type & HookType.MemReadUnmapped) != 0)
            yield return MemoryAccessType.ReadUnmapped;
        if ((registration.Type & HookType.MemWriteUnmapped) != 0)
            yield return MemoryAccessType.WriteUnmapped;
        if ((registration.Type & HookType.MemFetchUnmapped) != 0)
            yield return MemoryAccessType.FetchUnmapped;
        if ((registration.Type & HookType.MemReadProt) != 0)
            yield return MemoryAccessType.ReadProtected;
        if ((registration.Type & HookType.MemWriteProt) != 0)
            yield return MemoryAccessType.WriteProtected;
        if ((registration.Type & HookType.MemFetchProt) != 0)
            yield return MemoryAccessType.FetchProtected;
    }

    private static HookType NormalizeEventHookTypes(HookType eventTypes)
    {
        var normalized = eventTypes & EventMemoryHookMask;
        if (normalized == 0 || (eventTypes & ~EventMemoryHookMask) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventTypes), "Event types must consist solely of UC_HOOK_MEM_* flags.");
        }

        return normalized;
    }

    private void RemoveHookInternal(HookHandle handle, bool throwIfMissing)
    {
        if (handle.IsEmpty)
        {
            if (throwIfMissing)
            {
                throw new ArgumentException("Handle is empty", nameof(handle));
            }

            return;
        }

        if (!_hookRegistry.TryRemove(handle.Value, out var registration))
        {
            if (throwIfMissing)
            {
                throw new InvalidOperationException($"Hook {handle} was not registered.");
            }

            return;
        }

        RemoveEventRegistration(registration);

        var engine = EngineHandle;
        if (engine != IntPtr.Zero)
        {
            var err = _native.HookDel(engine, registration.Handle.Value);
            if (err != 0)
            {
                registration.Dispose();
                throw new UnicornHookException((ErrorCode)err, "uc_hook_del");
            }
        }

        registration.Dispose();
    }

    private void ClearHooks()
    {
        var engine = EngineHandle;

        foreach (var registration in _hookRegistry.Values)
        {
            if (engine != IntPtr.Zero && !registration.Handle.IsEmpty)
            {
                _native.HookDel(engine, registration.Handle.Value);
            }

            registration.Dispose();
        }

        _hookRegistry.Clear();
        _eventMemRegistrations.Clear();
    }
}