using System;
using System.Collections.Generic;

namespace UnicornNet;

/// <summary>
///     Fluent API for registering multiple hooks at once
/// </summary>
public sealed class HookBuilder
{
    private readonly IHookManager _hooks;
    private readonly List<Unicorn.HookHandle> _handles = [];

    internal HookBuilder(IHookManager hooks)
    {
        _hooks = hooks;
    }

    /// <summary>
    ///     Add a code hook that triggers on every instruction
    /// </summary>
    public HookBuilder OnCode(Unicorn.CodeHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.Code, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add a block hook that triggers on every basic block
    /// </summary>
    public HookBuilder OnBlock(Unicorn.BlockHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.Block, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add a memory read hook
    /// </summary>
    public HookBuilder OnMemoryRead(Unicorn.MemoryHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.MemRead, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add a memory write hook
    /// </summary>
    public HookBuilder OnMemoryWrite(Unicorn.MemoryHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.MemWrite, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add an interrupt hook
    /// </summary>
    public HookBuilder OnInterrupt(Unicorn.InterruptHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.Interrupt, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add an IN instruction hook
    /// </summary>
    public HookBuilder OnIn(Unicorn.InHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.Instruction, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add an OUT instruction hook
    /// </summary>
    public HookBuilder OnOut(Unicorn.OutHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.Instruction, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add a syscall/sysenter hook
    /// </summary>
    public HookBuilder OnSyscall(Unicorn.SyscallHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.Instruction, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add an invalid instruction hook
    /// </summary>
    public HookBuilder OnInvalidInstruction(Unicorn.InvalidInstructionHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(Unicorn.HookType.InvalidInstruction, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Add an event memory hook that listens to multiple event types described by a HookType mask
    /// </summary>
    public HookBuilder OnEventMem(Unicorn.HookType eventTypes, Unicorn.MemoryEventHook callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var handle = _hooks.AddHook(eventTypes, callback, range, state);
        _handles.Add(handle);
        return this;
    }

    /// <summary>
    ///     Returns all registered hook handles as an immutable copy
    /// </summary>
    public IReadOnlyList<Unicorn.HookHandle> GetHandles()
    {
        return _handles.ToArray();
    }
}
