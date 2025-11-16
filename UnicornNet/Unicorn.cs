using System.Runtime.InteropServices;

namespace UnicornNet;

public partial class Unicorn : IDisposable
{
    private static readonly NativeHookCallback HookThunk = OnNativeHook;
    private static readonly NativeMemHookCallback MemHookThunk = OnNativeMemHook;
    private static readonly NativeEventMemHookCallback EventMemHookThunk = OnNativeEventMemHook;
    private static readonly NativeInterruptHookCallback InterruptHookThunk = OnNativeInterruptHook;
    private static readonly NativeInstructionInHookCallback InstructionInHookThunk = OnNativeInstructionInHook;
    private static readonly NativeInstructionOutHookCallback InstructionOutHookThunk = OnNativeInstructionOutHook;
    private static readonly NativeSyscallHookCallback SyscallHookThunk = OnNativeSyscallHook;
    private readonly SafeEngineHandle _engineHandle;
    private readonly Dictionary<MemoryAccessType, HashSet<nuint>> _eventMemRegistrations = [];
    private readonly ReaderWriterLockSlim _hookLock = new(LockRecursionPolicy.NoRecursion);
    private readonly Dictionary<nuint, HookRegistration> _hookRegistry = [];
    private readonly IUnicornNativeProxy _native;
    private bool _disposed;

    public Unicorn(Architecture architecture, Mode mode)
        : this(architecture, mode, NativeUnicornProxy.Instance)
    {
    }

    public Unicorn(int architecture, int mode)
        : this((Architecture)architecture, (Mode)mode)
    {
    }

    internal Unicorn(Architecture architecture, Mode mode, IUnicornNativeProxy native)
    {
        _native = native ?? throw new ArgumentNullException(nameof(native));

        var err = _native.Open((int)architecture, (int)mode, out var handle);
        if (err != 0 || handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"uc_open failed: error {err}");
        }

        _engineHandle = new SafeEngineHandle(handle, _native);
    }

    private IntPtr EngineHandle
    {
        get => _engineHandle?.DangerousGetHandle() ?? IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearHooks();
        _engineHandle?.Dispose();
        _hookLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~Unicorn()
    {
        Dispose();
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Unicorn));
    }

    public void MemMap(ulong address, ulong size, MemoryPermissions permissions)
    {
        EnsureNotDisposed();
        var err = _native.MemMap(EngineHandle, address, size, (uint)permissions);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_map failed: error {err}");
        }
    }

    public void MemMap(ulong address, ulong size, uint permissions)
    {
        MemMap(address, size, (MemoryPermissions)permissions);
    }

    public void MemUnmap(ulong address, ulong size)
    {
        EnsureNotDisposed();
        var err = _native.MemUnmap(EngineHandle, address, size);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_unmap failed: error {err}");
        }
    }

    public void MemProtect(ulong address, ulong size, MemoryPermissions permissions)
    {
        EnsureNotDisposed();
        var err = _native.MemProtect(EngineHandle, address, size, (uint)permissions);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_protect failed: error {err}");
        }
    }

    public void MemProtect(ulong address, ulong size, uint permissions)
    {
        MemProtect(address, size, (MemoryPermissions)permissions);
    }

    public void MemWrite(ulong address, ReadOnlySpan<byte> data)
    {
        EnsureNotDisposed();
        var err = _native.MemWrite(EngineHandle, address, data);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_write failed: error {err}");
        }
    }

    public void MemRead(ulong address, Span<byte> buffer)
    {
        EnsureNotDisposed();
        var err = _native.MemRead(EngineHandle, address, buffer);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_read failed: error {err}");
        }
    }

    public HookHandle AddCodeHook(CodeHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterHook(HookType.Code, callback ?? throw new ArgumentNullException(nameof(callback)), state, range);
    }

    public HookHandle AddBlockHook(BlockHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterHook(HookType.Block, callback ?? throw new ArgumentNullException(nameof(callback)), state, range);
    }

    public HookHandle AddMemReadHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterMemoryHook(HookType.MemRead, MemoryAccessType.Read, callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public HookHandle AddMemWriteHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterMemoryHook(HookType.MemWrite, MemoryAccessType.Write, callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public HookHandle AddInterruptHook(InterruptHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterInterruptHook(callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public HookHandle AddInHook(InHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterInHook(callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public HookHandle AddOutHook(OutHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterOutHook(callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public HookHandle AddSyscallHook(SyscallHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterSyscallHook(callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public HookHandle AddEventMemHook(MemoryAccessType accessType, MemoryEventHook callback, HookRange? range = null, object? state = null)
    {
        return RegisterEventMemHook(accessType, callback ?? throw new ArgumentNullException(nameof(callback)), range, state);
    }

    public void RemoveHook(HookHandle handle)
    {
        EnsureNotDisposed();
        RemoveHookInternal(handle, false);
    }

    public void HookDel(HookHandle handle)
    {
        RemoveHook(handle);
    }

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

    private static void OnNativeHook(IntPtr engine, ulong address, uint size, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeCode(address, (int)size);
    }

    private static void OnNativeMemHook(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeMemory((MemoryAccessType)accessType, address, size, value);
    }

    private static bool OnNativeEventMemHook(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData)
    {
        return TryResolveRegistration(userData, out var registration) && registration!.InvokeEventMemory((MemoryAccessType)accessType, address, size, value);
    }

    private static void OnNativeInterruptHook(IntPtr engine, uint interruptNumber, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeInterrupt(interruptNumber);
    }

    private static uint OnNativeInstructionInHook(IntPtr engine, uint port, int size, IntPtr userData)
    {
        return !TryResolveRegistration(userData, out var registration) ? 0 : registration!.InvokeIn(port, size);
    }

    private static void OnNativeInstructionOutHook(IntPtr engine, uint port, int size, uint value, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeOut(port, size, value);
    }

    private static void OnNativeSyscallHook(IntPtr engine, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeSyscall();
    }

    private static bool TryResolveRegistration(IntPtr userData, out HookRegistration? registration)
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

    private enum HookCategory
    {
        Code,
        Block,
        Memory,
        EventMemory,
        Interrupt,
        In,
        Out,
        Syscall
    }

    private sealed class HookRegistration : IDisposable
    {
        private readonly Delegate _callback;
        private GCHandle _gcHandle;
        private readonly Unicorn _owner;
        private readonly object? _state;
        private readonly HookType _type;
        private bool _disposed;

        public HookRegistration(Unicorn owner, HookType type, HookCategory category, Delegate callback, object? state, MemoryAccessType? accessType = null)
        {
            _owner = owner;
            _type = type;
            Category = category;
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _state = state;
            AccessType = accessType;
            _gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        public HookHandle Handle { get; private set; }

        public HookCategory Category { get; }

        public MemoryAccessType? AccessType { get; }

        public IntPtr UserDataPointer
        {
            get => GCHandle.ToIntPtr(_gcHandle);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            _disposed = true;
        }

        public void SetHandle(HookHandle handle)
        {
            Handle = handle;
        }

        public void InvokeCode(ulong address, int size)
        {
            if (_disposed)
            {
                return;
            }

            switch (Category)
            {
                case HookCategory.Code when _callback is CodeHook codeHook:
                    codeHook(_owner, address, size, _state);
                    break;
                case HookCategory.Block when _callback is BlockHook blockHook:
                    blockHook(_owner, address, size, _state);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported hook category {Category} for code invocation.");
            }
        }

        public void InvokeMemory(MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (_disposed || _callback is not MemoryHook memHook)
            {
                return;
            }

            memHook(_owner, accessType, address, size, value, _state);
        }

        public bool InvokeEventMemory(MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (_disposed || _callback is not MemoryEventHook eventHook)
            {
                return true;
            }

            return eventHook(_owner, accessType, address, size, value, _state);
        }

        public void InvokeInterrupt(uint interruptNumber)
        {
            if (_disposed || _callback is not InterruptHook interruptHook)
            {
                return;
            }

            interruptHook(_owner, interruptNumber, _state);
        }

        public uint InvokeIn(uint port, int size)
        {
            if (_disposed || _callback is not InHook inHook)
            {
                return 0;
            }

            return inHook(_owner, port, size, _state);
        }

        public void InvokeOut(uint port, int size, uint value)
        {
            if (_disposed || _callback is not OutHook outHook)
            {
                return;
            }

            outHook(_owner, port, size, value, _state);
        }

        public void InvokeSyscall()
        {
            if (_disposed || _callback is not SyscallHook syscallHook)
            {
                return;
            }

            syscallHook(_owner, _state);
        }
    }

    public static partial class NativeMethods
    {
        [LibraryImport("unicorn", EntryPoint = "uc_mem_map")]
        public static partial int UcMemMap(IntPtr engine, ulong address, ulong size, uint perms);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_unmap")]
        public static partial int UcMemUnmap(IntPtr engine, ulong address, ulong size);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_protect")]
        public static partial int UcMemProtect(IntPtr engine, ulong address, ulong size, uint perms);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_write")]
        public static partial int UcMemWrite(IntPtr engine, ulong address, ref byte bytes, nuint size);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_read")]
        public static partial int UcMemRead(IntPtr engine, ulong address, ref byte buffer, nuint size);

        [LibraryImport("unicorn", EntryPoint = "uc_open")]
        public static partial int UcOpen(int arch, int mode, out IntPtr engine);

        [LibraryImport("unicorn", EntryPoint = "uc_close")]
        public static partial int UcClose(IntPtr engine);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAdd(IntPtr engine, out nuint hook, uint hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddMem(IntPtr engine, out nuint hook, uint hookType, NativeMemHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddEventMem(IntPtr engine, out nuint hook, uint hookType, NativeEventMemHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInterrupt(IntPtr engine, out nuint hook, uint hookType, NativeInterruptHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInstructionIn(IntPtr engine, out nuint hook, uint hookType, NativeInstructionInHookCallback callback, IntPtr userData, ulong begin, ulong end, int instruction);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInstructionOut(IntPtr engine, out nuint hook, uint hookType, NativeInstructionOutHookCallback callback, IntPtr userData, ulong begin, ulong end, int instruction);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInstructionSyscall(IntPtr engine, out nuint hook, uint hookType, NativeSyscallHookCallback callback, IntPtr userData, ulong begin, ulong end, int instruction);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_del")]
        public static partial int UcHookDel(IntPtr engine, nuint hook);
    }
}