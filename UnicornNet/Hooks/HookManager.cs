using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UnicornNet;

internal sealed class HookManager : IHookManager
{
    private const Unicorn.HookType EventMemoryHookMask = Unicorn.HookType.MemReadUnmapped
                                                        | Unicorn.HookType.MemWriteUnmapped
                                                        | Unicorn.HookType.MemFetchUnmapped
                                                        | Unicorn.HookType.MemReadProt
                                                        | Unicorn.HookType.MemWriteProt
                                                        | Unicorn.HookType.MemFetchProt;

    private const int X86InstructionIn = 218;
    private const int X86InstructionOut = 500;
    private const int X86InstructionSyscall = 699;

    private static readonly NativeHookCallback HookThunk = OnNativeHook;
    private static readonly NativeMemHookCallback MemHookThunk = OnNativeMemHook;
    private static readonly NativeEventMemHookCallback EventMemHookThunk = OnNativeEventMemHook;
    private static readonly NativeInterruptHookCallback InterruptHookThunk = OnNativeInterruptHook;
    private static readonly NativeInstructionInHookCallback InstructionInHookThunk = OnNativeInstructionInHook;
    private static readonly NativeInstructionOutHookCallback InstructionOutHookThunk = OnNativeInstructionOutHook;
    private static readonly NativeSyscallHookCallback SyscallHookThunk = OnNativeSyscallHook;
    private static readonly NativeInvalidInstructionHookCallback InvalidInstructionHookThunk = OnNativeInvalidInstructionHook;

    private readonly ConcurrentDictionary<Unicorn.MemoryAccessType, ConcurrentDictionary<nuint, byte>> _eventMemRegistrations = new();
    private readonly Func<IntPtr> _getEngineHandle;
    private readonly ConcurrentDictionary<nuint, HookRegistration> _hookRegistry = new();
    private readonly IUnicornNativeProxy _native;
    private readonly Unicorn _owner;
    private readonly Action _ensureNotDisposed;

    public HookManager(Unicorn owner, IUnicornNativeProxy native, Func<IntPtr> getEngineHandle, Action ensureNotDisposed)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(getEngineHandle);
        ArgumentNullException.ThrowIfNull(ensureNotDisposed);

        _owner = owner;
        _native = native;
        _getEngineHandle = getEngineHandle;
        _ensureNotDisposed = ensureNotDisposed;
    }

    public Unicorn.HookHandle AddHook(Unicorn.HookType type, Delegate callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return type switch
        {
            Unicorn.HookType.Code when callback is Unicorn.CodeHook => RegisterCodeOrBlockHook(type, callback, state, range),
            Unicorn.HookType.Block when callback is Unicorn.BlockHook => RegisterCodeOrBlockHook(type, callback, state, range),
            Unicorn.HookType.MemRead when callback is Unicorn.MemoryHook memoryHook => RegisterMemoryHook(type, Unicorn.MemoryAccessType.Read, memoryHook, range, state),
            Unicorn.HookType.MemWrite when callback is Unicorn.MemoryHook memoryHook => RegisterMemoryHook(type, Unicorn.MemoryAccessType.Write, memoryHook, range, state),
            Unicorn.HookType.Interrupt when callback is Unicorn.InterruptHook interruptHook => RegisterInterruptHook(interruptHook, range, state),
            Unicorn.HookType.Instruction when callback is Unicorn.InHook inHook => RegisterInHook(inHook, range, state),
            Unicorn.HookType.Instruction when callback is Unicorn.OutHook outHook => RegisterOutHook(outHook, range, state),
            Unicorn.HookType.Instruction when callback is Unicorn.SyscallHook syscallHook => RegisterSyscallHook(syscallHook, range, state),
            Unicorn.HookType.InvalidInstruction when callback is Unicorn.InvalidInstructionHook invalidInstructionHook => RegisterInvalidInstructionHook(invalidInstructionHook, range, state),
            _ when callback is Unicorn.MemoryEventHook eventHook => RegisterEventMemHook(type, eventHook, range, state),
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Hook type and callback delegate are not a supported combination.")
        };
    }

    public void RemoveHook(Unicorn.HookHandle handle)
    {
        _ensureNotDisposed();
        RemoveHookInternal(handle);
    }

    public void Dispose()
    {
        var engine = _getEngineHandle();

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

    internal (bool IsHealthy, int DisposedCount, int TotalCount) Validate()
    {
        var totalCount = _hookRegistry.Count;
        var disposedCount = 0;

        foreach (var registration in _hookRegistry.Values)
        {
            if (registration.IsDisposed || registration.Handle.IsEmpty)
            {
                disposedCount++;
            }
        }

        return (disposedCount == 0, disposedCount, totalCount);
    }

    internal bool TrySimulateHook(Unicorn.HookHandle handle, ulong address, int size)
    {
        if (!TryGetRegistration(handle, out var registration))
        {
            return false;
        }

        if (registration.Category is not (HookCategory.Code or HookCategory.Block))
        {
            return false;
        }

        registration.InvokeCode(address, size);
        return true;
    }

    internal bool TrySimulateMemoryHook(Unicorn.HookHandle handle, Unicorn.MemoryAccessType accessType, ulong address, int size, long value)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Memory)
        {
            return false;
        }

        registration.InvokeMemory(accessType, address, size, value);
        return true;
    }

    internal bool TrySimulateEventMem(Unicorn.MemoryAccessType accessType, ulong address, int size, long value)
    {
        if (!_eventMemRegistrations.TryGetValue(accessType, out var handles) || handles.IsEmpty)
        {
            return false;
        }

        var invoked = false;
        foreach (var handleEntry in handles)
        {
            if (!_hookRegistry.TryGetValue(handleEntry.Key, out var registration))
            {
                continue;
            }

            invoked = true;
            if (!registration.InvokeEventMemory(accessType, address, size, value))
            {
                break;
            }
        }

        return invoked;
    }

    internal bool TrySimulateInterruptHook(Unicorn.HookHandle handle, uint interruptNumber)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Interrupt)
        {
            return false;
        }

        registration.InvokeInterrupt(interruptNumber);
        return true;
    }

    internal bool TrySimulateInHook(Unicorn.HookHandle handle, uint port, int size, out uint value)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.In)
        {
            value = 0;
            return false;
        }

        value = registration.InvokeIn(port, size);
        return true;
    }

    internal bool TrySimulateOutHook(Unicorn.HookHandle handle, uint port, int size, uint value)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Out)
        {
            return false;
        }

        registration.InvokeOut(port, size, value);
        return true;
    }

    internal bool TrySimulateSyscallHook(Unicorn.HookHandle handle)
    {
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.Syscall)
        {
            return false;
        }

        registration.InvokeSyscall();
        return true;
    }

    internal bool TrySimulateInvalidInstructionHook(Unicorn.HookHandle handle, out bool continueExecution)
    {
        continueExecution = false;
        if (!TryGetRegistration(handle, out var registration) || registration.Category != HookCategory.InvalidInstruction)
        {
            return false;
        }

        continueExecution = registration.InvokeInvalidInstruction();
        return true;
    }

    private static void OnNativeHook(IntPtr engine, ulong address, uint size, IntPtr userData)
    {
        if (TryResolveRegistration(userData, out var registration))
        {
            registration.InvokeCode(address, (int)size);
        }
    }

    private static void OnNativeMemHook(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData)
    {
        if (TryResolveRegistration(userData, out var registration))
        {
            registration.InvokeMemory((Unicorn.MemoryAccessType)accessType, address, size, value);
        }
    }

    private static bool OnNativeEventMemHook(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData)
    {
        return TryResolveRegistration(userData, out var registration) && registration.InvokeEventMemory((Unicorn.MemoryAccessType)accessType, address, size, value);
    }

    private static void OnNativeInterruptHook(IntPtr engine, uint interruptNumber, IntPtr userData)
    {
        if (TryResolveRegistration(userData, out var registration))
        {
            registration.InvokeInterrupt(interruptNumber);
        }
    }

    private static uint OnNativeInstructionInHook(IntPtr engine, uint port, int size, IntPtr userData)
    {
        return TryResolveRegistration(userData, out var registration) ? registration.InvokeIn(port, size) : 0;
    }

    private static void OnNativeInstructionOutHook(IntPtr engine, uint port, int size, uint value, IntPtr userData)
    {
        if (TryResolveRegistration(userData, out var registration))
        {
            registration.InvokeOut(port, size, value);
        }
    }

    private static void OnNativeSyscallHook(IntPtr engine, IntPtr userData)
    {
        if (TryResolveRegistration(userData, out var registration))
        {
            registration.InvokeSyscall();
        }
    }

    private static bool OnNativeInvalidInstructionHook(IntPtr engine, IntPtr userData)
    {
        return TryResolveRegistration(userData, out var registration) && registration.InvokeInvalidInstruction();
    }

    private static bool TryResolveRegistration(IntPtr userData, [NotNullWhen(true)] out HookRegistration? registration)
    {
        registration = null;
        if (userData == IntPtr.Zero)
        {
            return false;
        }

        var gcHandle = GCHandle.FromIntPtr(userData);
        registration = gcHandle.Target as HookRegistration;
        return registration is not null;
    }

    private Unicorn.HookHandle RegisterCodeOrBlockHook(Unicorn.HookType type, Delegate callback, object? state, Unicorn.HookRange? range)
    {
        var normalizedRange = NormalizeRange(range);
        var category = type switch
        {
            Unicorn.HookType.Code => HookCategory.Code,
            Unicorn.HookType.Block => HookCategory.Block,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        var registration = new HookRegistration(_owner, type, category, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAdd(engine, type, HookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterMemoryHook(Unicorn.HookType hookType, Unicorn.MemoryAccessType accessType, Unicorn.MemoryHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, hookType, HookCategory.Memory, callback, state, accessType);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddMem(engine, hookType, MemHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterEventMemHook(Unicorn.HookType eventTypes, Unicorn.MemoryEventHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedHookTypes = NormalizeEventHookTypes(eventTypes);
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, normalizedHookTypes, HookCategory.EventMemory, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddEventMem(engine, normalizedHookTypes, EventMemHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterInterruptHook(Unicorn.InterruptHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, Unicorn.HookType.Interrupt, HookCategory.Interrupt, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddInterrupt(engine, Unicorn.HookType.Interrupt, InterruptHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterInHook(Unicorn.InHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, Unicorn.HookType.Instruction, HookCategory.In, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddInstructionIn(engine, Unicorn.HookType.Instruction, InstructionInHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, X86InstructionIn, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterOutHook(Unicorn.OutHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, Unicorn.HookType.Instruction, HookCategory.Out, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddInstructionOut(engine, Unicorn.HookType.Instruction, InstructionOutHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, X86InstructionOut, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterSyscallHook(Unicorn.SyscallHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, Unicorn.HookType.Instruction, HookCategory.Syscall, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddInstructionSyscall(engine, Unicorn.HookType.Instruction, SyscallHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, X86InstructionSyscall, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterInvalidInstructionHook(Unicorn.InvalidInstructionHook callback, Unicorn.HookRange? range, object? state)
    {
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(_owner, Unicorn.HookType.InvalidInstruction, HookCategory.InvalidInstruction, callback, state);
        return RegisterHookInternal(registration, normalizedRange, (engine, hookRange) =>
        {
            var err = _native.HookAddInvalidInstruction(engine, Unicorn.HookType.InvalidInstruction, InvalidInstructionHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
            return (err, hookId);
        });
    }

    private Unicorn.HookHandle RegisterHookInternal(HookRegistration registration, Unicorn.HookRange range, Func<IntPtr, Unicorn.HookRange, (int Error, nuint Handle)> registrar)
    {
        _ensureNotDisposed();

        var engine = _getEngineHandle();
        var (error, handle) = registrar(engine, range);
        if (error != 0)
        {
            registration.Dispose();
            throw new UnicornHookException((Unicorn.ErrorCode)error, "uc_hook_add");
        }

        registration.SetHandle(new Unicorn.HookHandle(handle));
        AddRegistration(registration);
        return registration.Handle;
    }

    private static Unicorn.HookRange NormalizeRange(Unicorn.HookRange? range)
    {
        return range ?? Unicorn.HookRange.All;
    }

    private bool TryGetRegistration(Unicorn.HookHandle handle, [NotNullWhen(true)] out HookRegistration? registration)
    {
        if (!handle.IsEmpty)
        {
            return _hookRegistry.TryGetValue(handle.Value, out registration);
        }

        registration = null;
        return false;
    }

    private void AddRegistration(HookRegistration registration)
    {
        _hookRegistry.TryAdd(registration.Handle.Value, registration);

        if (registration.Category != HookCategory.EventMemory)
        {
            return;
        }

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

    private static IEnumerable<Unicorn.MemoryAccessType> GetEventAccessTypes(HookRegistration registration)
    {
        if (registration.AccessType.HasValue)
        {
            yield return registration.AccessType.Value;
            yield break;
        }

        if ((registration.Type & Unicorn.HookType.MemReadUnmapped) != 0)
        {
            yield return Unicorn.MemoryAccessType.ReadUnmapped;
        }

        if ((registration.Type & Unicorn.HookType.MemWriteUnmapped) != 0)
        {
            yield return Unicorn.MemoryAccessType.WriteUnmapped;
        }

        if ((registration.Type & Unicorn.HookType.MemFetchUnmapped) != 0)
        {
            yield return Unicorn.MemoryAccessType.FetchUnmapped;
        }

        if ((registration.Type & Unicorn.HookType.MemReadProt) != 0)
        {
            yield return Unicorn.MemoryAccessType.ReadProtected;
        }

        if ((registration.Type & Unicorn.HookType.MemWriteProt) != 0)
        {
            yield return Unicorn.MemoryAccessType.WriteProtected;
        }

        if ((registration.Type & Unicorn.HookType.MemFetchProt) != 0)
        {
            yield return Unicorn.MemoryAccessType.FetchProtected;
        }
    }

    private static Unicorn.HookType NormalizeEventHookTypes(Unicorn.HookType eventTypes)
    {
        var normalized = eventTypes & EventMemoryHookMask;
        if (normalized == 0 || (eventTypes & ~EventMemoryHookMask) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventTypes), "Event types must consist solely of UC_HOOK_MEM_* flags.");
        }

        return normalized;
    }

    private void RemoveHookInternal(Unicorn.HookHandle handle)
    {
        if (handle.IsEmpty)
        {
            return;
        }

        if (!_hookRegistry.TryRemove(handle.Value, out var registration))
        {
            return;
        }

        RemoveEventRegistration(registration);

        var engine = _getEngineHandle();
        if (engine != IntPtr.Zero)
        {
            var err = _native.HookDel(engine, registration.Handle.Value);
            if (err != 0)
            {
                registration.Dispose();
                throw new UnicornHookException((Unicorn.ErrorCode)err, "uc_hook_del");
            }
        }

        registration.Dispose();
    }

    private enum HookCategory
    {
        Code,
        Block,
        Memory,
        EventMemory,
        Interrupt,
        In,
        Out,
        Syscall,
        InvalidInstruction
    }

    private sealed class HookRegistration : IDisposable
    {
        private readonly Delegate _callback;
        private readonly Unicorn _owner;
        private readonly object? _state;
        private GCHandle _gcHandle;

        public HookRegistration(Unicorn owner, Unicorn.HookType type, HookCategory category, Delegate callback, object? state, Unicorn.MemoryAccessType? accessType = null)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _owner = owner;
            Type = type;
            Category = category;
            _callback = callback;
            _state = state;
            AccessType = accessType;
            _gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        public Unicorn.HookHandle Handle { get; private set; }

        public HookCategory Category { get; }

        public Unicorn.HookType Type { get; }

        public Unicorn.MemoryAccessType? AccessType { get; }

        public bool IsDisposed { get; private set; }

        public IntPtr UserDataPointer
        {
            get => GCHandle.ToIntPtr(_gcHandle);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            IsDisposed = true;
        }

        public void SetHandle(Unicorn.HookHandle handle)
        {
            Handle = handle;
        }

        public void InvokeCode(ulong address, int size)
        {
            if (IsDisposed)
            {
                LogDisposedHook("Code/Block");
                return;
            }

            try
            {
                switch (Category)
                {
                    case HookCategory.Code when _callback is Unicorn.CodeHook codeHook:
                        codeHook(_owner, address, size, _state);
                        break;
                    case HookCategory.Block when _callback is Unicorn.BlockHook blockHook:
                        blockHook(_owner, address, size, _state);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported hook category {Category} for code invocation.");
                }
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, Category == HookCategory.Code ? "Code" : "Block");
            }
        }

        public void InvokeMemory(Unicorn.MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (IsDisposed)
            {
                LogDisposedHook("Memory");
                return;
            }

            if (_callback is not Unicorn.MemoryHook memHook)
            {
                LogInvalidCallback("Memory", nameof(Unicorn.MemoryHook));
                return;
            }

            try
            {
                memHook(_owner, accessType, address, size, value, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Memory");
            }
        }

        public bool InvokeEventMemory(Unicorn.MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (IsDisposed)
            {
                LogDisposedHook("EventMemory");
                return true;
            }

            if (_callback is not Unicorn.MemoryEventHook eventHook)
            {
                LogInvalidCallback("EventMemory", nameof(Unicorn.MemoryEventHook));
                return true;
            }

            try
            {
                return eventHook(_owner, accessType, address, size, value, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "EventMemory");
                return true;
            }
        }

        public void InvokeInterrupt(uint interruptNumber)
        {
            if (IsDisposed)
            {
                LogDisposedHook("Interrupt");
                return;
            }

            if (_callback is not Unicorn.InterruptHook interruptHook)
            {
                LogInvalidCallback("Interrupt", nameof(Unicorn.InterruptHook));
                return;
            }

            try
            {
                interruptHook(_owner, interruptNumber, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Interrupt");
            }
        }

        public uint InvokeIn(uint port, int size)
        {
            if (IsDisposed)
            {
                LogDisposedHook("In");
                return 0;
            }

            if (_callback is not Unicorn.InHook inHook)
            {
                LogInvalidCallback("In", nameof(Unicorn.InHook));
                return 0;
            }

            try
            {
                return inHook(_owner, port, size, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "In");
                return 0;
            }
        }

        public void InvokeOut(uint port, int size, uint value)
        {
            if (IsDisposed)
            {
                LogDisposedHook("Out");
                return;
            }

            if (_callback is not Unicorn.OutHook outHook)
            {
                LogInvalidCallback("Out", nameof(Unicorn.OutHook));
                return;
            }

            try
            {
                outHook(_owner, port, size, value, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Out");
            }
        }

        public void InvokeSyscall()
        {
            if (IsDisposed)
            {
                LogDisposedHook("Syscall");
                return;
            }

            if (_callback is not Unicorn.SyscallHook syscallHook)
            {
                LogInvalidCallback("Syscall", nameof(Unicorn.SyscallHook));
                return;
            }

            try
            {
                syscallHook(_owner, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Syscall");
            }
        }

        public bool InvokeInvalidInstruction()
        {
            if (IsDisposed)
            {
                LogDisposedHook("InvalidInstruction");
                return false;
            }

            if (_callback is not Unicorn.InvalidInstructionHook invalidInsnHook)
            {
                LogInvalidCallback("InvalidInstruction", nameof(Unicorn.InvalidInstructionHook));
                return false;
            }

            try
            {
                return invalidInsnHook(_owner, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "InvalidInstruction");
                return false;
            }
        }

        private void HandleCallbackException(Exception exception, string hookType)
        {
            var logger = _owner.Logger;
            var handling = _owner.Options.CallbackExceptionHandling;

            if (handling != CallbackExceptionHandling.Throw && logger == null)
            {
                handling = CallbackExceptionHandling.Throw;
            }

            switch (handling)
            {
                case CallbackExceptionHandling.Throw:
                    throw exception;
                case CallbackExceptionHandling.LogAndThrow:
                    logger!.LogError($"Exception in {hookType} hook callback", exception);
                    throw exception;
                case CallbackExceptionHandling.LogAndContinue:
                    logger!.LogError($"Exception in {hookType} hook callback (continuing execution)", exception);
                    break;
            }
        }

        private void LogDisposedHook(string hookType)
        {
            if (_owner.Options.EnableVerboseDiagnostics)
            {
                _owner.Logger?.LogWarning($"{hookType} hook invoked but registration is disposed");
            }
        }

        private void LogInvalidCallback(string hookType, string expectedType)
        {
            if (_owner.Options.EnableVerboseDiagnostics)
            {
                _owner.Logger?.LogWarning($"{hookType} hook invoked but callback is not of expected type {expectedType}");
            }
        }
    }
}
