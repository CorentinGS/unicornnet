using System;

namespace UnicornNet;

internal sealed class ControlEngine : IControlEngine
{
    private readonly Action _ensureNotDisposed;
    private readonly Func<IntPtr> _getEngineHandle;
    private readonly IUnicornNativeProxy _native;

    public ControlEngine(IUnicornNativeProxy native, Func<IntPtr> getEngineHandle, Action ensureNotDisposed)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(getEngineHandle);
        ArgumentNullException.ThrowIfNull(ensureNotDisposed);

        _native = native;
        _getEngineHandle = getEngineHandle;
        _ensureNotDisposed = ensureNotDisposed;
    }

    public void Control(Unicorn.ControlCommand command)
    {
        _ensureNotDisposed();
        var err = _native.Control(_getEngineHandle(), command.Value, command.Arguments);
        if (err != 0)
        {
            throw new UnicornEngineException((Unicorn.ErrorCode)err, "uc_ctl");
        }
    }
}
