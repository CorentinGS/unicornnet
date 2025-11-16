namespace UnicornNet;

public partial class Unicorn
{
    private readonly Dictionary<MemoryAccessType, HashSet<nuint>> _eventMemRegistrations = [];
    private readonly ReaderWriterLockSlim _hookLock = new(LockRecursionPolicy.NoRecursion);
    private readonly Dictionary<nuint, HookRegistration> _hookRegistry = [];

    internal bool TrySimulateHook(HookHandle handle, ulong address, int size)
    {
        if (!TryGetRegistration(handle, out var registration))
        {
            return false;
        }

        if (registration.Category is HookCategory.Code or HookCategory.Block)
        {
            registration.InvokeCode(address, size);
            return true;
        }

        return false;
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
        List<HookRegistration> registrations;
        _hookLock.EnterReadLock();
        try
        {
            if (!_eventMemRegistrations.TryGetValue(accessType, out var handles) || handles.Count == 0)
            {
                return false;
            }

            registrations = new List<HookRegistration>(handles.Count);
            foreach (var handle in handles)
            {
                if (_hookRegistry.TryGetValue(handle, out var registration))
                {
                    registrations.Add(registration);
                }
            }
        }
        finally
        {
            _hookLock.ExitReadLock();
        }

        var invoked = false;
        foreach (var registration in registrations)
        {
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

    private HookHandle RegisterEventMemHook(MemoryAccessType accessType, MemoryEventHook callback, HookRange? range, object? state)
    {
        if (!IsEventMemoryType(accessType))
        {
            throw new ArgumentOutOfRangeException(nameof(accessType), "Only unmapped and protected accesses are valid event types.");
        }

        var hookType = GetHookTypeForEvent(accessType);
        var normalizedRange = NormalizeRange(range);
        var registration = new HookRegistration(this, hookType, HookCategory.EventMemory, callback, state, accessType);
        return RegisterHookInternal(
            registration,
            normalizedRange,
            (engine, hookRange) =>
            {
                var err = _native.HookAddEventMem(engine, hookType, EventMemHookThunk, registration.UserDataPointer, hookRange.Begin, hookRange.End, out var hookId);
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

    private HookHandle RegisterHookInternal(HookRegistration registration, HookRange range, Func<IntPtr, HookRange, (int Error, nuint Handle)> registrar)
    {
        EnsureNotDisposed();

        var engine = EngineHandle;
        var (error, handle) = registrar(engine, range);
        if (error != 0)
        {
            registration.Dispose();
            throw new InvalidOperationException($"uc_hook_add failed: error {error}");
        }

        registration.SetHandle(new HookHandle(handle));
        AddRegistration(registration);
        return registration.Handle;
    }

    private static HookRange NormalizeRange(HookRange? range)
    {
        return range ?? HookRange.All;
    }

    private bool TryGetRegistration(HookHandle handle, out HookRegistration registration)
    {
        registration = null!;
        if (handle.IsEmpty)
        {
            return false;
        }

        _hookLock.EnterReadLock();
        try
        {
            if (!_hookRegistry.TryGetValue(handle.Value, out var existing))
                return false;

            registration = existing;
            return true;
        }
        finally
        {
            _hookLock.ExitReadLock();
        }
    }

    private void AddRegistration(HookRegistration registration)
    {
        _hookLock.EnterWriteLock();
        try
        {
            _hookRegistry.Add(registration.Handle.Value, registration);
            if (registration.Category != HookCategory.EventMemory || !registration.AccessType.HasValue)
                return;

            if (!_eventMemRegistrations.TryGetValue(registration.AccessType.Value, out var handles))
            {
                handles = [];
                _eventMemRegistrations.Add(registration.AccessType.Value, handles);
            }

            handles.Add(registration.Handle.Value);
        }
        finally
        {
            _hookLock.ExitWriteLock();
        }
    }

    private void RemoveEventRegistration(HookRegistration registration)
    {
        if (registration.Category != HookCategory.EventMemory || !registration.AccessType.HasValue)
        {
            return;
        }

        if (!_eventMemRegistrations.TryGetValue(registration.AccessType.Value, out var handles))
            return;

        handles.Remove(registration.Handle.Value);
        if (handles.Count == 0)
        {
            _eventMemRegistrations.Remove(registration.AccessType.Value);
        }
    }

    private static bool IsEventMemoryType(MemoryAccessType accessType)
    {
        return accessType is MemoryAccessType.ReadUnmapped
            or MemoryAccessType.WriteUnmapped
            or MemoryAccessType.FetchUnmapped
            or MemoryAccessType.ReadProtected
            or MemoryAccessType.WriteProtected
            or MemoryAccessType.FetchProtected;
    }

    private static HookType GetHookTypeForEvent(MemoryAccessType accessType)
    {
        return accessType switch
        {
            MemoryAccessType.ReadUnmapped => HookType.MemReadUnmapped,
            MemoryAccessType.WriteUnmapped => HookType.MemWriteUnmapped,
            MemoryAccessType.FetchUnmapped => HookType.MemFetchUnmapped,
            MemoryAccessType.ReadProtected => HookType.MemReadProt,
            MemoryAccessType.WriteProtected => HookType.MemWriteProt,
            MemoryAccessType.FetchProtected => HookType.MemFetchProt,
            _ => throw new ArgumentOutOfRangeException(nameof(accessType))
        };
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

        HookRegistration? registration;
        _hookLock.EnterWriteLock();
        try
        {
            if (_hookRegistry.Remove(handle.Value, out registration))
            {
                RemoveEventRegistration(registration);
            }
            else if (throwIfMissing)
            {
                throw new InvalidOperationException($"Hook {handle} was not registered.");
            }
        }
        finally
        {
            _hookLock.ExitWriteLock();
        }

        if (registration is null)
        {
            return;
        }

        var engine = EngineHandle;
        if (engine != IntPtr.Zero)
        {
            var err = _native.HookDel(engine, registration.Handle.Value);
            if (err != 0)
            {
                registration.Dispose();
                throw new InvalidOperationException($"uc_hook_del failed: error {err}");
            }
        }

        registration.Dispose();
    }

    private void ClearHooks()
    {
        List<HookRegistration> registrations;
        _hookLock.EnterWriteLock();
        try
        {
            registrations = new List<HookRegistration>(_hookRegistry.Values);
            _hookRegistry.Clear();
            _eventMemRegistrations.Clear();
        }
        finally
        {
            _hookLock.ExitWriteLock();
        }

        var engine = EngineHandle;
        foreach (var registration in registrations)
        {
            if (engine != IntPtr.Zero && !registration.Handle.IsEmpty)
            {
                _native.HookDel(engine, registration.Handle.Value);
            }

            registration.Dispose();
        }
    }
}