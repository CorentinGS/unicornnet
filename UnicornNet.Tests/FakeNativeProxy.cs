using System;
using System.Collections.Generic;

namespace UnicornNet.Tests;

/// <summary>
///     Test double for IUnicornNativeProxy that tracks method calls and provides controllable behavior.
/// </summary>
internal class FakeNativeProxy : IUnicornNativeProxy
{
    private nuint _nextHookHandle;

    public bool Closed { get; private set; }

    public List<nuint> ActiveHooks { get; } = [];

    public List<nuint> RemovedHooks { get; } = [];

    public (uint Control, nint[] Arguments)? LastControl { get; private set; }

    public List<(ulong Address, ulong Size, uint Permissions, IntPtr Pointer)> MemMapPtrRequests { get; } = [];

    public (int RegisterId, byte[] Value)? LastRegisterWrite { get; private set; }

    public Dictionary<int, byte[]> RegisterValues { get; } = new();

    public (ulong Begin, ulong Until, ulong Timeout, nuint Count)? LastEmuStart { get; private set; }

    public bool EmulationStopped { get; private set; }

    public virtual int Open(int architecture, int mode, out IntPtr engine)
    {
        engine = new IntPtr(0x1234);
        return 0;
    }

    public virtual int Close(IntPtr engine)
    {
        Closed = true;
        return 0;
    }

    public virtual int MemMap(IntPtr engine, ulong address, ulong size, uint permissions)
    {
        return 0;
    }

    public virtual int MemMapPtr(IntPtr engine, ulong address, ulong size, uint permissions, IntPtr pointer)
    {
        MemMapPtrRequests.Add((address, size, permissions, pointer));
        return 0;
    }

    public virtual int MemUnmap(IntPtr engine, ulong address, ulong size)
    {
        return 0;
    }

    public virtual int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions)
    {
        return 0;
    }

    public virtual int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data)
    {
        return 0;
    }

    public virtual int MemRead(IntPtr engine, ulong address, Span<byte> buffer)
    {
        buffer.Clear();
        return 0;
    }

    public virtual int RegWrite(IntPtr engine, int registerId, ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return 0;
        }

        var buffer = value.ToArray();
        RegisterValues[registerId] = buffer;
        LastRegisterWrite = (registerId, buffer);
        return 0;
    }

    public virtual int RegRead(IntPtr engine, int registerId, Span<byte> buffer)
    {
        if (!RegisterValues.TryGetValue(registerId, out var stored))
        {
            buffer.Clear();
            return 0;
        }

        var bytesToCopy = Math.Min(buffer.Length, stored.Length);
        stored.AsSpan(0, bytesToCopy).CopyTo(buffer);
        if (bytesToCopy < buffer.Length)
        {
            buffer.Slice(bytesToCopy).Clear();
        }

        return 0;
    }

    public virtual int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddMem(IntPtr engine, Unicorn.HookType hookType, NativeMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddEventMem(IntPtr engine, Unicorn.HookType hookType, NativeEventMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddInterrupt(IntPtr engine, Unicorn.HookType hookType, NativeInterruptHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddInstructionIn(IntPtr engine, Unicorn.HookType hookType, NativeInstructionInHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddInstructionOut(IntPtr engine, Unicorn.HookType hookType, NativeInstructionOutHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddInstructionSyscall(IntPtr engine, Unicorn.HookType hookType, NativeSyscallHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookAddInvalidInstruction(IntPtr engine, Unicorn.HookType hookType, NativeInvalidInstructionHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
    {
        return RegisterHook(out hookId);
    }

    public virtual int HookDel(IntPtr engine, nuint hookId)
    {
        ActiveHooks.Remove(hookId);
        RemovedHooks.Add(hookId);
        return 0;
    }

    public virtual int EmuStart(IntPtr engine, ulong begin, ulong until, ulong timeout, nuint instructionCount)
    {
        LastEmuStart = (begin, until, timeout, instructionCount);
        return 0;
    }

    public virtual int EmuStop(IntPtr engine)
    {
        EmulationStopped = true;
        return 0;
    }

    public virtual int Control(IntPtr engine, uint control, ReadOnlySpan<nint> arguments)
    {
        var args = new nint[arguments.Length];
        arguments.CopyTo(args);
        LastControl = (control, args);
        return 0;
    }

    public virtual int Errno(IntPtr engine)
    {
        return 0;
    }

    private int RegisterHook(out nuint hookId)
    {
        hookId = ++_nextHookHandle;
        ActiveHooks.Add(hookId);
        return 0;
    }
}