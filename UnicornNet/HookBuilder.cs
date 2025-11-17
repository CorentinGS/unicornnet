namespace UnicornNet;

/// <summary>
/// Fluent API for registering multiple hooks at once
/// </summary>
public sealed class HookBuilder
{
    private readonly Unicorn _engine;
    private readonly List<Unicorn.HookHandle> _handles = [];

    internal HookBuilder(Unicorn engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Add a code hook that triggers on every instruction
    /// </summary>
    public HookBuilder OnCode(Unicorn.CodeHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddCodeHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add a block hook that triggers on every basic block
    /// </summary>
    public HookBuilder OnBlock(Unicorn.BlockHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddBlockHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add a memory read hook
    /// </summary>
    public HookBuilder OnMemoryRead(Unicorn.MemoryHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddMemReadHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add a memory write hook
    /// </summary>
    public HookBuilder OnMemoryWrite(Unicorn.MemoryHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddMemWriteHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add an interrupt hook
    /// </summary>
    public HookBuilder OnInterrupt(Unicorn.InterruptHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddInterruptHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add an IN instruction hook
    /// </summary>
    public HookBuilder OnIn(Unicorn.InHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddInHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add an OUT instruction hook
    /// </summary>
    public HookBuilder OnOut(Unicorn.OutHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddOutHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add a syscall/sysenter hook
    /// </summary>
    public HookBuilder OnSyscall(Unicorn.SyscallHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddSyscallHook(callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add an event memory hook (unmapped/protected memory access)
    /// </summary>
    public HookBuilder OnEventMem(Unicorn.MemoryAccessType accessType, Unicorn.MemoryEventHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddEventMemHook(accessType, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Add an event memory hook that listens to multiple event types described by a HookType mask
    /// </summary>
    public HookBuilder OnEventMem(Unicorn.HookType eventTypes, Unicorn.MemoryEventHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _engine.AddEventMemHook(eventTypes, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    /// Returns all registered hook handles as an immutable copy
    /// </summary>
    public IReadOnlyList<Unicorn.HookHandle> GetHandles() => _handles.ToArray();
}
