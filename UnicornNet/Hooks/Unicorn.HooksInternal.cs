namespace UnicornNet;

public partial class Unicorn
{
    internal bool TrySimulateHook(HookHandle handle, ulong address, int size)
    {
        return _hooks.TrySimulateHook(handle, address, size);
    }

    internal bool TrySimulateMemoryHook(HookHandle handle, MemoryAccessType accessType, ulong address, int size, long value)
    {
        return _hooks.TrySimulateMemoryHook(handle, accessType, address, size, value);
    }

    internal bool TrySimulateEventMem(MemoryAccessType accessType, ulong address, int size, long value)
    {
        return _hooks.TrySimulateEventMem(accessType, address, size, value);
    }

    internal bool TrySimulateInterruptHook(HookHandle handle, uint interruptNumber)
    {
        return _hooks.TrySimulateInterruptHook(handle, interruptNumber);
    }

    internal bool TrySimulateInHook(HookHandle handle, uint port, int size, out uint value)
    {
        return _hooks.TrySimulateInHook(handle, port, size, out value);
    }

    internal bool TrySimulateOutHook(HookHandle handle, uint port, int size, uint value)
    {
        return _hooks.TrySimulateOutHook(handle, port, size, value);
    }

    internal bool TrySimulateSyscallHook(HookHandle handle)
    {
        return _hooks.TrySimulateSyscallHook(handle);
    }

    internal bool TrySimulateInvalidInstructionHook(HookHandle handle, out bool continueExecution)
    {
        return _hooks.TrySimulateInvalidInstructionHook(handle, out continueExecution);
    }
}
