using System.Runtime.InteropServices;

namespace UnicornNet;

public delegate void NativeHookCallback(IntPtr engine, ulong address, uint size, IntPtr userData);

public delegate void NativeMemHookCallback(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData);

public delegate bool NativeEventMemHookCallback(IntPtr engine, uint accessType, ulong address, int size, long value, IntPtr userData);

public delegate void NativeInterruptHookCallback(IntPtr engine, uint interruptNumber, IntPtr userData);

public delegate uint NativeInstructionInHookCallback(IntPtr engine, uint port, int size, IntPtr userData);

public delegate void NativeInstructionOutHookCallback(IntPtr engine, uint port, int size, uint value, IntPtr userData);

public delegate void NativeSyscallHookCallback(IntPtr engine, IntPtr userData);

internal interface IUnicornNativeProxy
{
    int Open(int architecture, int mode, out IntPtr engine);
    int Close(IntPtr engine);
    int MemMap(IntPtr engine, ulong address, ulong size, uint permissions);
    int MemMapPtr(IntPtr engine, ulong address, ulong size, uint permissions, IntPtr pointer);
    int MemUnmap(IntPtr engine, ulong address, ulong size);
    int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions);
    int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data);
    int MemRead(IntPtr engine, ulong address, Span<byte> buffer);
    int RegWrite(IntPtr engine, int registerId, ReadOnlySpan<byte> value);
    int RegRead(IntPtr engine, int registerId, Span<byte> buffer);
    int EmuStart(IntPtr engine, ulong begin, ulong until, ulong timeout, nuint instructionCount);
    int EmuStop(IntPtr engine);
    int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId);
    int HookAddMem(IntPtr engine, Unicorn.HookType hookType, NativeMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId);
    int HookAddEventMem(IntPtr engine, Unicorn.HookType hookType, NativeEventMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId);
    int HookAddInterrupt(IntPtr engine, Unicorn.HookType hookType, NativeInterruptHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId);
    int HookAddInstructionIn(IntPtr engine, Unicorn.HookType hookType, NativeInstructionInHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId);
    int HookAddInstructionOut(IntPtr engine, Unicorn.HookType hookType, NativeInstructionOutHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId);
    int HookAddInstructionSyscall(IntPtr engine, Unicorn.HookType hookType, NativeSyscallHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId);
    int HookDel(IntPtr engine, nuint hookId);
    int Control(IntPtr engine, uint control, ReadOnlySpan<nint> arguments);
    int Errno(IntPtr engine);
}

internal sealed class NativeUnicornProxy : IUnicornNativeProxy
{
    private NativeUnicornProxy()
    {
    }
    public static NativeUnicornProxy Instance { get; } = new();

    public int Open(int architecture, int mode, out IntPtr engine)
    {
        return Unicorn.NativeMethods.UcOpen(architecture, mode, out engine);
    }

    public int Close(IntPtr engine)
    {
        return Unicorn.NativeMethods.UcClose(engine);
    }

    public int MemMap(IntPtr engine, ulong address, ulong size, uint permissions)
    {
        return Unicorn.NativeMethods.UcMemMap(engine, address, size, permissions);
    }

    public int MemMapPtr(IntPtr engine, ulong address, ulong size, uint permissions, IntPtr pointer)
    {
        return Unicorn.NativeMethods.UcMemMapPtr(engine, address, size, permissions, pointer);
    }

    public int MemUnmap(IntPtr engine, ulong address, ulong size)
    {
        return Unicorn.NativeMethods.UcMemUnmap(engine, address, size);
    }

    public int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions)
    {
        return Unicorn.NativeMethods.UcMemProtect(engine, address, size, permissions);
    }

    public int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data)
    {
        return data.IsEmpty ? 0 : Unicorn.NativeMethods.UcMemWrite(engine, address, ref MemoryMarshal.GetReference(data), (nuint)data.Length);
    }

    public int MemRead(IntPtr engine, ulong address, Span<byte> buffer)
    {
        return buffer.IsEmpty ? 0 : Unicorn.NativeMethods.UcMemRead(engine, address, ref MemoryMarshal.GetReference(buffer), (nuint)buffer.Length);
    }

    public int RegWrite(IntPtr engine, int registerId, ReadOnlySpan<byte> value)
    {
        return value.IsEmpty ? 0 : Unicorn.NativeMethods.UcRegWrite(engine, registerId, ref MemoryMarshal.GetReference(value));
    }

    public int RegRead(IntPtr engine, int registerId, Span<byte> buffer)
    {
        return buffer.IsEmpty ? 0 : Unicorn.NativeMethods.UcRegRead(engine, registerId, ref MemoryMarshal.GetReference(buffer));
    }

    public int EmuStart(IntPtr engine, ulong begin, ulong until, ulong timeout, nuint instructionCount)
    {
        return Unicorn.NativeMethods.UcEmuStart(engine, begin, until, timeout, instructionCount);
    }

    public int EmuStop(IntPtr engine)
    {
        return Unicorn.NativeMethods.UcEmuStop(engine);
    }

    public int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAdd(engine, out hookId, (uint)hookType, callback, userData, begin, end);
    }

    public int HookAddMem(IntPtr engine, Unicorn.HookType hookType, NativeMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAddMem(engine, out hookId, (uint)hookType, callback, userData, begin, end);
    }

    public int HookAddEventMem(IntPtr engine, Unicorn.HookType hookType, NativeEventMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAddEventMem(engine, out hookId, (uint)hookType, callback, userData, begin, end);
    }

    public int HookAddInterrupt(IntPtr engine, Unicorn.HookType hookType, NativeInterruptHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAddInterrupt(engine, out hookId, (uint)hookType, callback, userData, begin, end);
    }

    public int HookAddInstructionIn(IntPtr engine, Unicorn.HookType hookType, NativeInstructionInHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAddInstructionIn(engine, out hookId, (uint)hookType, callback, userData, begin, end, instructionId);
    }

    public int HookAddInstructionOut(IntPtr engine, Unicorn.HookType hookType, NativeInstructionOutHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAddInstructionOut(engine, out hookId, (uint)hookType, callback, userData, begin, end, instructionId);
    }

    public int HookAddInstructionSyscall(IntPtr engine, Unicorn.HookType hookType, NativeSyscallHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookAddInstructionSyscall(engine, out hookId, (uint)hookType, callback, userData, begin, end, instructionId);
    }

    public int HookDel(IntPtr engine, nuint hookId)
    {
        return Unicorn.NativeMethods.UcHookDel(engine, hookId);
    }

    public int Control(IntPtr engine, uint control, ReadOnlySpan<nint> arguments)
    {
        return arguments.Length switch
        {
            0 => Unicorn.NativeMethods.UcCtl0(engine, control),
            1 => Unicorn.NativeMethods.UcCtl1(engine, control, arguments[0]),
            2 => Unicorn.NativeMethods.UcCtl2(engine, control, arguments[0], arguments[1]),
            3 => Unicorn.NativeMethods.UcCtl3(engine, control, arguments[0], arguments[1], arguments[2]),
            4 => Unicorn.NativeMethods.UcCtl4(engine, control, arguments[0], arguments[1], arguments[2], arguments[3]),
            _ => throw new ArgumentOutOfRangeException(nameof(arguments), "uc_ctl supports up to 4 arguments in this binding.")
        };
    }

    public int Errno(IntPtr engine)
    {
        return Unicorn.NativeMethods.UcErrno(engine);
    }
}

internal sealed class SafeEngineHandle : SafeHandle
{
    private readonly IUnicornNativeProxy _native;

    public SafeEngineHandle(IntPtr preexistingHandle, IUnicornNativeProxy native)
        : base(IntPtr.Zero, true)
    {
        ArgumentNullException.ThrowIfNull(native);
        _native = native;
        SetHandle(preexistingHandle);
    }

    public override bool IsInvalid
    {
        get => handle == IntPtr.Zero;
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            _native.Close(handle);
        }

        return true;
    }
}
