namespace UnicornNet;

using System;
using System.Runtime.InteropServices;

public delegate void NativeHookCallback(IntPtr engine, ulong address, uint size, IntPtr userData);

internal interface IUnicornNativeProxy
{
    int Open(int architecture, int mode, out IntPtr engine);
    int Close(IntPtr engine);
    int MemMap(IntPtr engine, ulong address, ulong size, uint permissions);
    int MemUnmap(IntPtr engine, ulong address, ulong size);
    int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions);
    int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data);
    int MemRead(IntPtr engine, ulong address, Span<byte> buffer);
    int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId);
    int HookDel(IntPtr engine, nuint hookId);
}

internal sealed class NativeUnicornProxy : IUnicornNativeProxy
{
    public static NativeUnicornProxy Instance { get; } = new();

    private NativeUnicornProxy()
    {
    }

    public int Open(int architecture, int mode, out IntPtr engine) => Unicorn.NativeMethods.UcOpen(architecture, mode, out engine);

    public int Close(IntPtr engine) => Unicorn.NativeMethods.UcClose(engine);

    public int MemMap(IntPtr engine, ulong address, ulong size, uint permissions) => Unicorn.NativeMethods.UcMemMap(engine, address, size, permissions);

    public int MemUnmap(IntPtr engine, ulong address, ulong size) => Unicorn.NativeMethods.UcMemUnmap(engine, address, size);

    public int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions) => Unicorn.NativeMethods.UcMemProtect(engine, address, size, permissions);

    public int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        return Unicorn.NativeMethods.UcMemWrite(engine, address, ref MemoryMarshal.GetReference(data), (nuint)data.Length);
    }

    public int MemRead(IntPtr engine, ulong address, Span<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }

        return Unicorn.NativeMethods.UcMemRead(engine, address, ref MemoryMarshal.GetReference(buffer), (nuint)buffer.Length);
    }

    public int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId) => Unicorn.NativeMethods.UcHookAdd(engine, out hookId, (uint)hookType, callback, userData, begin, end);

    public int HookDel(IntPtr engine, nuint hookId) => Unicorn.NativeMethods.UcHookDel(engine, hookId);
}

internal sealed class SafeEngineHandle : SafeHandle
{
    private readonly IUnicornNativeProxy _native;

    public SafeEngineHandle(IntPtr preexistingHandle, IUnicornNativeProxy native)
        : base(IntPtr.Zero, true)
    {
        _native = native ?? throw new ArgumentNullException(nameof(native));
        SetHandle(preexistingHandle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            _native.Close(handle);
        }

        return true;
    }
}
