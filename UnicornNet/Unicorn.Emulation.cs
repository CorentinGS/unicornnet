namespace UnicornNet;

public partial class Unicorn
{
    public void EmuStart(ulong beginAddress, ulong untilAddress, ulong timeout = 0, ulong instructionCount = 0)
    {
        EnsureNotDisposed();
        var err = _native.EmuStart(EngineHandle, beginAddress, untilAddress, timeout, checked((nuint)instructionCount));
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_emu_start");
        }
    }

    public void EmuStop()
    {
        EnsureNotDisposed();
        var err = _native.EmuStop(EngineHandle);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_emu_stop");
        }
    }
}