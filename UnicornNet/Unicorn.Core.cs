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
        ArgumentNullException.ThrowIfNull(native);
        _native = native;

        var err = _native.Open((int)architecture, (int)mode, out var handle);
        if (err != 0 || handle == IntPtr.Zero)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_open");
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

    public void Control(ControlCommand command)
    {
        EnsureNotDisposed();
        var err = _native.Control(EngineHandle, command.Value, ReadOnlySpan<nint>.Empty);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_ctl");
        }
    }

    public void Control(ControlCommand command, nint arg1)
    {
        EnsureNotDisposed();
        ReadOnlySpan<nint> args = [arg1];
        var err = _native.Control(EngineHandle, command.Value, args);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_ctl");
        }
    }

    public void Control(ControlCommand command, nint arg1, nint arg2)
    {
        EnsureNotDisposed();
        ReadOnlySpan<nint> args = [arg1, arg2];
        var err = _native.Control(EngineHandle, command.Value, args);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_ctl");
        }
    }

    public void Control(ControlCommand command, nint arg1, nint arg2, nint arg3)
    {
        EnsureNotDisposed();
        ReadOnlySpan<nint> args = [arg1, arg2, arg3];
        var err = _native.Control(EngineHandle, command.Value, args);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_ctl");
        }
    }

    public void Control(ControlCommand command, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        EnsureNotDisposed();
        ReadOnlySpan<nint> args = [arg1, arg2, arg3, arg4];
        var err = _native.Control(EngineHandle, command.Value, args);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_ctl");
        }
    }

    public void Control(ControlCommand command, ReadOnlySpan<nint> arguments)
    {
        EnsureNotDisposed();
        var err = _native.Control(EngineHandle, command.Value, arguments);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_ctl");
        }
    }

    public void Control(ControlType type, ControlIo access)
    {
        var command = ControlCommand.Create(type, 0, access);
        Control(command);
    }

    public void Control(ControlType type, ControlIo access, nint arg1)
    {
        var command = ControlCommand.Create(type, 1, access);
        Control(command, arg1);
    }

    public void Control(ControlType type, ControlIo access, nint arg1, nint arg2)
    {
        var command = ControlCommand.Create(type, 2, access);
        Control(command, arg1, arg2);
    }

    public void Control(ControlType type, ControlIo access, nint arg1, nint arg2, nint arg3)
    {
        var command = ControlCommand.Create(type, 3, access);
        Control(command, arg1, arg2, arg3);
    }

    public void Control(ControlType type, ControlIo access, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        var command = ControlCommand.Create(type, 4, access);
        Control(command, arg1, arg2, arg3, arg4);
    }

    public void Control(ControlType type, ControlIo access, ReadOnlySpan<nint> arguments)
    {
        var command = ControlCommand.Create(type, arguments.Length, access);
        Control(command, arguments);
    }

    public void ControlRead(ControlType type)
    {
        Control(type, ControlIo.Read);
    }

    public void ControlRead(ControlType type, nint arg1)
    {
        Control(type, ControlIo.Read, arg1);
    }

    public void ControlRead(ControlType type, nint arg1, nint arg2)
    {
        Control(type, ControlIo.Read, arg1, arg2);
    }

    public void ControlRead(ControlType type, nint arg1, nint arg2, nint arg3)
    {
        Control(type, ControlIo.Read, arg1, arg2, arg3);
    }

    public void ControlRead(ControlType type, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(type, ControlIo.Read, arg1, arg2, arg3, arg4);
    }

    public void ControlRead(ControlType type, ReadOnlySpan<nint> arguments)
    {
        Control(type, ControlIo.Read, arguments);
    }

    public void ControlWrite(ControlType type)
    {
        Control(type, ControlIo.Write);
    }

    public void ControlWrite(ControlType type, nint arg1)
    {
        Control(type, ControlIo.Write, arg1);
    }

    public void ControlWrite(ControlType type, nint arg1, nint arg2)
    {
        Control(type, ControlIo.Write, arg1, arg2);
    }

    public void ControlWrite(ControlType type, nint arg1, nint arg2, nint arg3)
    {
        Control(type, ControlIo.Write, arg1, arg2, arg3);
    }

    public void ControlWrite(ControlType type, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(type, ControlIo.Write, arg1, arg2, arg3, arg4);
    }

    public void ControlWrite(ControlType type, ReadOnlySpan<nint> arguments)
    {
        Control(type, ControlIo.Write, arguments);
    }

    public void ControlReadWrite(ControlType type)
    {
        Control(type, ControlIo.ReadWrite);
    }

    public void ControlReadWrite(ControlType type, nint arg1)
    {
        Control(type, ControlIo.ReadWrite, arg1);
    }

    public void ControlReadWrite(ControlType type, nint arg1, nint arg2)
    {
        Control(type, ControlIo.ReadWrite, arg1, arg2);
    }

    public void ControlReadWrite(ControlType type, nint arg1, nint arg2, nint arg3)
    {
        Control(type, ControlIo.ReadWrite, arg1, arg2, arg3);
    }

    public void ControlReadWrite(ControlType type, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(type, ControlIo.ReadWrite, arg1, arg2, arg3, arg4);
    }

    public void ControlReadWrite(ControlType type, ReadOnlySpan<nint> arguments)
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