using System;

namespace UnicornNet;

public partial class Unicorn
{
    /// <summary>
    ///     Returns a fluent builder for registering multiple hooks
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
    ///     Adds a code hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds a block hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds a memory read hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds a memory write hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds an interrupt hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds an IN instruction hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds an OUT instruction hook with a strongly-typed state parameter to avoid boxing
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
    ///     Adds a syscall hook with a strongly-typed state parameter to avoid boxing
    /// </summary>
    public HookHandle AddSyscallHook<TState>(SyscallHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        SyscallHook wrapper = (engine, boxedState) => callback(engine, (TState)boxedState!);
        return RegisterSyscallHook(wrapper, range, state);
    }

    /// <summary>
    ///     Adds a hook that will be called when the emulator encounters an invalid instruction.
    /// </summary>
    /// <param name="callback">
    ///     The callback to invoke when an invalid instruction is encountered.
    ///     Should return true to continue emulation, false to stop.
    /// </param>
    /// <param name="range">The address range for which this hook is active (null for all addresses).</param>
    /// <param name="state">Optional user state to pass to the callback.</param>
    /// <returns>A handle that can be used to remove this hook later.</returns>
    /// <remarks>
    ///     <para>
    ///         <b>Error Handling:</b>
    ///     </para>
    ///     <para>
    ///         If your callback throws an exception, the behavior depends on
    ///         <see cref="UnicornOptions.CallbackExceptionHandling" />:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <see cref="CallbackExceptionHandling.Throw" /> (default): The exception is rethrown
    ///                     immediately
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="CallbackExceptionHandling.LogAndThrow" />: The exception is logged then
    ///                     rethrown
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="CallbackExceptionHandling.LogAndContinue" />: The exception is logged and the
    ///                     hook returns false (stops emulation)
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Diagnostic Context:</b>
    ///     </para>
    ///     <para>
    ///         To get detailed information about the invalid instruction (PC, instruction bytes),
    ///         call <see cref="GetInvalidInstructionContext" /> from within your callback.
    ///     </para>
    ///     <para>
    ///         <b>Example:</b>
    ///     </para>
    ///     <code>
    /// unicorn.AddInvalidInstructionHook((engine, state) =>
    /// {
    ///     var context = engine.GetInvalidInstructionContext();
    ///     Console.WriteLine($"Invalid instruction: {context.GetDetailedMessage()}");
    ///     return false; // Stop emulation
    /// });
    /// </code>
    /// </remarks>
    public HookHandle AddInvalidInstructionHook(InvalidInstructionHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterInvalidInstructionHook(callback, range, state);
    }

    /// <summary>
    ///     Adds an invalid instruction hook with a strongly-typed state parameter to avoid boxing.
    /// </summary>
    /// <typeparam name="TState">The type of the state parameter.</typeparam>
    /// <param name="callback">
    ///     The callback to invoke when an invalid instruction is encountered.
    ///     Should return true to continue emulation, false to stop.
    /// </param>
    /// <param name="range">The address range for which this hook is active (null for all addresses).</param>
    /// <param name="state">Optional user state to pass to the callback.</param>
    /// <returns>A handle that can be used to remove this hook later.</returns>
    /// <remarks>
    ///     See <see cref="AddInvalidInstructionHook(InvalidInstructionHook, HookRange?, object?)" /> for detailed
    ///     documentation
    ///     on error handling and diagnostic capabilities.
    /// </remarks>
    public HookHandle AddInvalidInstructionHook<TState>(InvalidInstructionHook<TState> callback, HookRange? range = null, TState? state = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        InvalidInstructionHook wrapper = (engine, boxedState) => callback(engine, (TState)boxedState!);
        return RegisterInvalidInstructionHook(wrapper, range, state);
    }

    /// <summary>
    ///     Adds an event memory hook that can monitor multiple event types using a hook-type bitmask
    /// </summary>
    public HookHandle AddEventMemHook(HookType eventTypes, MemoryEventHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return RegisterEventMemHook(eventTypes, callback, range, state);
    }

    /// <summary>
    ///     Adds a memory event hook with a strongly-typed state parameter that can listen to multiple hook types
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