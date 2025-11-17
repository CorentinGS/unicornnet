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

    /// <summary>
    /// Adds a code hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddCodeHook<TState>(CodeHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        // Wrap the generic callback to bridge to the non-generic internal implementation
        CodeHook wrapper = (engine, address, size, boxedState) => callback(engine, address, size, (TState)boxedState!);
        return RegisterHook(HookType.Code, wrapper, state, range);
    }

    public HookHandle AddBlockHook(BlockHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterHook(HookType.Block, callback, state, range);
    }

    /// <summary>
    /// Adds a block hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddBlockHook<TState>(BlockHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        BlockHook wrapper = (engine, address, size, boxedState) => callback(engine, address, size, (TState)boxedState!);
        return RegisterHook(HookType.Block, wrapper, state, range);
    }

    public HookHandle AddMemReadHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterMemoryHook(HookType.MemRead, MemoryAccessType.Read, callback, range, state);
    }

    /// <summary>
    /// Adds a memory read hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddMemReadHook<TState>(MemoryHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        MemoryHook wrapper = (engine, accessType, address, size, value, boxedState) => callback(engine, accessType, address, size, value, (TState)boxedState!);
        return RegisterMemoryHook(HookType.MemRead, MemoryAccessType.Read, wrapper, range, state);
    }

    public HookHandle AddMemWriteHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterMemoryHook(HookType.MemWrite, MemoryAccessType.Write, callback, range, state);
    }

    /// <summary>
    /// Adds a memory write hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddMemWriteHook<TState>(MemoryHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        MemoryHook wrapper = (engine, accessType, address, size, value, boxedState) => callback(engine, accessType, address, size, value, (TState)boxedState!);
        return RegisterMemoryHook(HookType.MemWrite, MemoryAccessType.Write, wrapper, range, state);
    }

    public HookHandle AddInterruptHook(InterruptHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterInterruptHook(callback, range, state);
    }

    /// <summary>
    /// Adds an interrupt hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddInterruptHook<TState>(InterruptHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        InterruptHook wrapper = (engine, interruptNumber, boxedState) => callback(engine, interruptNumber, (TState)boxedState!);
        return RegisterInterruptHook(wrapper, range, state);
    }

    public HookHandle AddInHook(InHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterInHook(callback, range, state);
    }

    /// <summary>
    /// Adds an IN instruction hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddInHook<TState>(InHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        InHook wrapper = (engine, port, size, boxedState) => callback(engine, port, size, (TState)boxedState!);
        return RegisterInHook(wrapper, range, state);
    }

    public HookHandle AddOutHook(OutHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterOutHook(callback, range, state);
    }

    /// <summary>
    /// Adds an OUT instruction hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddOutHook<TState>(OutHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        OutHook wrapper = (engine, port, size, value, boxedState) => callback(engine, port, size, value, (TState)boxedState!);
        return RegisterOutHook(wrapper, range, state);
    }

    public HookHandle AddSyscallHook(SyscallHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterSyscallHook(callback, range, state);
    }

    /// <summary>
    /// Adds a syscall hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddSyscallHook<TState>(SyscallHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        SyscallHook wrapper = (engine, boxedState) => callback(engine, (TState)boxedState!);
        return RegisterSyscallHook(wrapper, range, state);
    }

    /// <summary>
    /// Adds an event memory hook that can monitor multiple event types using a hook-type bitmask
    /// </summary>
    public HookHandle AddEventMemHook(HookType eventTypes, MemoryEventHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterEventMemHook(eventTypes, callback, range, state);
    }

    /// <summary>
    /// Adds a memory event hook with a strongly-typed state parameter that can listen to multiple hook types
    /// </summary>
    public HookHandle AddEventMemHook<TState>(HookType eventTypes, MemoryEventHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        MemoryEventHook wrapper = (engine, accessType2, address, size, value, boxedState) => callback(engine, accessType2, address, size, value, (TState)boxedState!);
        return RegisterEventMemHook(eventTypes, wrapper, range, state);
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
