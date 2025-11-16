namespace UnicornNet;

public partial class Unicorn
{
    /// <summary>
    /// Returns a fluent builder for registering multiple hooks
    /// </summary>
    public HookBuilder Hooks()
    {
        EnsureNotDisposed();
        return new HookBuilder(this);
    }

    public HookHandle AddCodeHook(CodeHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterHook(HookType.Code, callback, state, range);
    }

    public HookHandle AddBlockHook(BlockHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterHook(HookType.Block, callback, state, range);
    }

    public HookHandle AddMemReadHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterMemoryHook(HookType.MemRead, MemoryAccessType.Read, callback, range, state);
    }

    public HookHandle AddMemWriteHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterMemoryHook(HookType.MemWrite, MemoryAccessType.Write, callback, range, state);
    }

    public HookHandle AddInterruptHook(InterruptHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterInterruptHook(callback, range, state);
    }

    public HookHandle AddInHook(InHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterInHook(callback, range, state);
    }

    public HookHandle AddOutHook(OutHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterOutHook(callback, range, state);
    }

    public HookHandle AddSyscallHook(SyscallHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterSyscallHook(callback, range, state);
    }

    public HookHandle AddEventMemHook(MemoryAccessType accessType, MemoryEventHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterEventMemHook(accessType, callback, range, state);
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