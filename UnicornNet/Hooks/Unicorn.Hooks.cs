using System;

namespace UnicornNet;

public partial class Unicorn
{
    // Hook state is stored as object? because native callbacks return through IntPtr userData;
    // value-type state is boxed, so generic hook overloads are intentionally not exposed.

    /// <summary>
    ///     Returns a fluent builder for registering multiple hooks
    /// </summary>
    public HookBuilder Hooks()
    {
        EnsureNotDisposed();
        return new HookBuilder(_hooks);
    }

    public HookHandle AddCodeHook(CodeHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.Code, callback, range, state);
    }

    public HookHandle AddBlockHook(BlockHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.Block, callback, range, state);
    }

    public HookHandle AddMemReadHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.MemRead, callback, range, state);
    }

    public HookHandle AddMemWriteHook(MemoryHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.MemWrite, callback, range, state);
    }

    public HookHandle AddInterruptHook(InterruptHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.Interrupt, callback, range, state);
    }

    public HookHandle AddInHook(InHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.Instruction, callback, range, state);
    }

    public HookHandle AddOutHook(OutHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.Instruction, callback, range, state);
    }

    public HookHandle AddSyscallHook(SyscallHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(HookType.Instruction, callback, range, state);
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
        return _hooks.AddHook(HookType.InvalidInstruction, callback, range, state);
    }

    /// <summary>
    ///     Adds an event memory hook that can monitor multiple event types using a hook-type bitmask
    /// </summary>
    public HookHandle AddEventMemHook(HookType eventTypes, MemoryEventHook callback, HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _hooks.AddHook(eventTypes, callback, range, state);
    }

    public void RemoveHook(HookHandle handle)
    {
        EnsureNotDisposed();
        _hooks.RemoveHook(handle);
    }

    public void HookDel(HookHandle handle)
    {
        RemoveHook(handle);
    }
}
