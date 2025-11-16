namespace UnicornNet;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public partial class Unicorn : IDisposable
{
    private readonly SafeEngineHandle _engineHandle;
    private readonly IUnicornNativeProxy _native;
    private readonly ReaderWriterLockSlim _hookLock = new(LockRecursionPolicy.NoRecursion);
    private readonly Dictionary<nuint, HookRegistration> _hookRegistry = [];
    private bool _disposed;

    private static readonly NativeHookCallback HookThunk = OnNativeHook;

    public Unicorn(Architecture architecture, Mode mode)
        : this(architecture, mode, NativeUnicornProxy.Instance)
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

    private IntPtr EngineHandle
    {
        get => _engineHandle?.DangerousGetHandle() ?? IntPtr.Zero;
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
        => RegisterHook(HookType.Code, callback ?? throw new ArgumentNullException(nameof(callback)), state, range);

    public HookHandle AddBlockHook(BlockHook callback, HookRange? range = null, object? state = null)
        => RegisterHook(HookType.Block, callback ?? throw new ArgumentNullException(nameof(callback)), state, range);

    public void RemoveHook(HookHandle handle)
    {
        EnsureNotDisposed();
        RemoveHookInternal(handle, false);
    }

    public void HookDel(HookHandle handle) => RemoveHook(handle);

    internal bool TrySimulateHook(HookHandle handle, ulong address, int size)
    {
        HookRegistration? registration;
        _hookLock.EnterReadLock();
        try
        {
            _hookRegistry.TryGetValue(handle.Value, out registration);
        }
        finally
        {
            _hookLock.ExitReadLock();
        }

        if (registration is null)
        {
            return false;
        }

        registration.Invoke(address, size);
        return true;
    }

    private HookHandle RegisterHook(HookType type, Delegate callback, object? state, HookRange? range)
    {
        EnsureNotDisposed();

        var normalizedRange = range ?? HookRange.All;
        var registration = new HookRegistration(this, type, callback, state);

        var err = _native.HookAdd(EngineHandle, type, HookThunk, registration.UserDataPointer, normalizedRange.Begin, normalizedRange.End, out var hookId);
        if (err != 0)
        {
            registration.Dispose();
            throw new InvalidOperationException($"uc_hook_add failed: error {err}");
        }

        registration.SetHandle(new HookHandle(hookId));

        _hookLock.EnterWriteLock();
        try
        {
            _hookRegistry.Add(hookId, registration);
        }
        finally
        {
            _hookLock.ExitWriteLock();
        }

        return registration.Handle;
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
            var err = _native.HookDel(engine, handle.Value);
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
        if (userData == IntPtr.Zero)
        {
            return;
        }

        var gcHandle = GCHandle.FromIntPtr(userData);
        if (gcHandle.Target is HookRegistration registration)
        {
            registration.Invoke(address, (int)size);
        }
    }

    private sealed class HookRegistration : IDisposable
    {
        private readonly Unicorn _owner;
        private readonly Delegate _callback;
        private readonly object? _state;
        private readonly HookType _type;
        private GCHandle _gcHandle;
        private bool _disposed;

        public HookRegistration(Unicorn owner, HookType type, Delegate callback, object? state)
        {
            _owner = owner;
            _type = type;
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _state = state;
            _gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        public HookHandle Handle { get; private set; }

        public IntPtr UserDataPointer
        {
            get => GCHandle.ToIntPtr(_gcHandle);
        }

        public void SetHandle(HookHandle handle) => Handle = handle;

        public void Invoke(ulong address, int size)
        {
            if (_disposed)
            {
                return;
            }

            switch (_callback)
            {
                case CodeHook codeHook when _type == HookType.Code:
                    codeHook(_owner, address, size, _state);
                    break;
                case BlockHook blockHook when _type == HookType.Block:
                    blockHook(_owner, address, size, _state);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported hook type {_type}.");
            }
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

        [LibraryImport("unicorn", EntryPoint = "uc_hook_del")]
        public static partial int UcHookDel(IntPtr engine, nuint hook);
    }
}
