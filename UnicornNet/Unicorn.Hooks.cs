namespace UnicornNet;

public partial class Unicorn
{
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
}