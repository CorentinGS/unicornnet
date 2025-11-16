namespace UnicornNet;

public partial class Unicorn : IDisposable
{
    private readonly SafeEngineHandle _engineHandle;
    private readonly IUnicornNativeProxy _native;
    private bool _disposed;

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

    public void Control(ControlCommand command, params nint[] arguments)
    {
        EnsureNotDisposed();
        var span = arguments ?? [];
        var err = _native.Control(EngineHandle, command.Value, span);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_ctl failed: error {err}");
        }
    }

    public void Control(ControlType type, ControlIo access, params nint[] arguments)
    {
        var command = ControlCommand.Create(type, arguments.Length, access);
        Control(command, arguments);
    }

    public void ControlRead(ControlType type, params nint[] arguments)
    {
        Control(type, ControlIo.Read, arguments);
    }

    public void ControlWrite(ControlType type, params nint[] arguments)
    {
        Control(type, ControlIo.Write, arguments);
    }

    public void ControlReadWrite(ControlType type, params nint[] arguments)
    {
        Control(type, ControlIo.ReadWrite, arguments);
    }

    public void ControlNone(ControlType type)
    {
        Control(type, ControlIo.None);
    }

    public ErrorCode GetLastError()
    {
        EnsureNotDisposed();
        return (ErrorCode)_native.Errno(EngineHandle);
    }
}