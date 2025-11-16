using System.Runtime.InteropServices;

namespace UnicornNet;

public partial class Unicorn
{
    private static readonly NativeHookCallback HookThunk = OnNativeHook;
    private static readonly NativeMemHookCallback MemHookThunk = OnNativeMemHook;
    private static readonly NativeEventMemHookCallback EventMemHookThunk = OnNativeEventMemHook;
    private static readonly NativeInterruptHookCallback InterruptHookThunk = OnNativeInterruptHook;
    private static readonly NativeInstructionInHookCallback InstructionInHookThunk = OnNativeInstructionInHook;
    private static readonly NativeInstructionOutHookCallback InstructionOutHookThunk = OnNativeInstructionOutHook;
    private static readonly NativeSyscallHookCallback SyscallHookThunk = OnNativeSyscallHook;

    private static void OnNativeHook(IntPtr engine, ulong address, uint size, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeCode(address, (int)size);
    }

    private static void OnNativeMemHook(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeMemory((MemoryAccessType)accessType, address, size, value);
    }

    private static bool OnNativeEventMemHook(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData)
    {
        return TryResolveRegistration(userData, out var registration) && registration!.InvokeEventMemory((MemoryAccessType)accessType, address, size, value);
    }

    private static void OnNativeInterruptHook(IntPtr engine, uint interruptNumber, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeInterrupt(interruptNumber);
    }

    private static uint OnNativeInstructionInHook(IntPtr engine, uint port, int size, IntPtr userData)
    {
        return !TryResolveRegistration(userData, out var registration) ? 0 : registration!.InvokeIn(port, size);
    }

    private static void OnNativeInstructionOutHook(IntPtr engine, uint port, int size, uint value, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeOut(port, size, value);
    }

    private static void OnNativeSyscallHook(IntPtr engine, IntPtr userData)
    {
        if (!TryResolveRegistration(userData, out var registration))
        {
            return;
        }

        registration!.InvokeSyscall();
    }

    private static bool TryResolveRegistration(IntPtr userData, out HookRegistration? registration)
    {
        registration = null;
        if (userData == IntPtr.Zero)
        {
            return false;
        }

        var gcHandle = GCHandle.FromIntPtr(userData);
        registration = gcHandle.Target as HookRegistration;
        return registration is not null;
    }
}