using System;
using System.Collections.Generic;
using Xunit;
using MemoryAccessType = UnicornNet.Unicorn.MemoryAccessType;

namespace UnicornNet.Tests;

public sealed class HookTests
{
    [Fact]
    public void CodeHook_IsInvoked_WhenTriggered()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invocations = 0;
        object? observedState = null;

        var handle = unicorn.AddCodeHook((_, address, size, state) =>
        {
            invocations++;
            Assert.Equal(0x1000UL, address);
            Assert.Equal(4, size);
            observedState = state;
        }, state: "code");

        Assert.True(unicorn.TrySimulateHook(handle, 0x1000, 4));
        Assert.Equal(1, invocations);
        Assert.Equal("code", observedState);
    }

    [Fact]
    public void BlockHook_Removal_PreventsFurtherInvocations()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invocations = 0;
        var handle = unicorn.AddBlockHook((_, _, _, _) => invocations++);

        unicorn.RemoveHook(handle);

        Assert.False(unicorn.TrySimulateHook(handle, 0x2000, 8));
        Assert.Equal(0, invocations);
        Assert.Contains(handle.Value, native.RemovedHooks);
    }

    [Fact]
    public void HookDel_RemovesHook()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var handle = unicorn.AddCodeHook((_, _, _, _) => { });

        unicorn.HookDel(handle);

        Assert.False(unicorn.TrySimulateHook(handle, 0x3000, 2));
        Assert.Single(native.RemovedHooks);
    }

    [Fact]
    public void Dispose_RemovesAllHooks()
    {
        var native = new FakeNativeProxy();
        var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var first = unicorn.AddCodeHook((_, _, _, _) => { });
        var second = unicorn.AddBlockHook((_, _, _, _) => { });

        unicorn.Dispose();

        Assert.True(native.Closed);
        Assert.Contains(first.Value, native.RemovedHooks);
        Assert.Contains(second.Value, native.RemovedHooks);
        Assert.Empty(native.ActiveHooks);
    }

    [Fact]
    public void MemReadHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        MemoryAccessType observedType = default;
        ulong observedAddress = 0;
        var observedSize = 0;
        long observedValue = 0;
        object? observedState = null;

        var handle = unicorn.AddMemReadHook((engine, type, address, size, value, state) =>
        {
            observedType = type;
            observedAddress = address;
            observedSize = size;
            observedValue = value;
            observedState = state;
        }, state: "mem");

        Assert.True(unicorn.TrySimulateMemoryHook(handle, MemoryAccessType.Read, 0x4000, 4, 0x12));
        Assert.Equal(MemoryAccessType.Read, observedType);
        Assert.Equal(0x4000UL, observedAddress);
        Assert.Equal(4, observedSize);
        Assert.Equal(0x12, observedValue);
        Assert.Equal("mem", observedState);
    }

    [Fact]
    public void MemWriteHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invoked = false;
        var handle = unicorn.AddMemWriteHook((engine, type, address, size, value, state) =>
        {
            invoked = type == MemoryAccessType.Write && address == 0x5000 && size == 2 && value == 0xFF;
        });

        Assert.True(unicorn.TrySimulateMemoryHook(handle, MemoryAccessType.Write, 0x5000, 2, 0xFF));
        Assert.True(invoked);
    }

    [Fact]
    public void EventMemHook_TracksPerEventType()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invocations = new List<string>();
        var first = unicorn.AddEventMemHook(MemoryAccessType.ReadUnmapped, (engine, type, address, size, value, state) =>
        {
            invocations.Add($"first:{state}");
            return true;
        }, state: 1);

        var second = unicorn.AddEventMemHook(MemoryAccessType.ReadUnmapped, (engine, type, address, size, value, state) =>
        {
            invocations.Add($"second:{state}");
            return false;
        }, state: 2);

        Assert.True(unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, 0x6000, 8, 0));
        Assert.Equal(new[]
        {
            "first:1",
            "second:2"
        }, invocations);

        invocations.Clear();
        unicorn.RemoveHook(first);
        Assert.True(unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, 0x6000, 8, 0));
        Assert.Equal(new[]
        {
            "second:2"
        }, invocations);

        unicorn.RemoveHook(second);
        Assert.False(unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, 0x6000, 8, 0));
    }

    [Fact]
    public void InterruptHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        uint observed = 0;
        object? observedState = null;

        var handle = unicorn.AddInterruptHook((engine, interrupt, state) =>
        {
            observed = interrupt;
            observedState = state;
        }, state: "intr");

        Assert.True(unicorn.TrySimulateInterruptHook(handle, 0x21));
        Assert.Equal(0x21U, observed);
        Assert.Equal("intr", observedState);
    }

    [Fact]
    public void InHook_ReturnsValue()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var handle = unicorn.AddInHook((engine, port, size, state) => port + (uint)size);

        Assert.True(unicorn.TrySimulateInHook(handle, 0x33, 4, out var value));
        Assert.Equal(0x37U, value);
    }

    [Fact]
    public void OutHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var observed = new List<(uint port, int size, uint value)>();
        var handle = unicorn.AddOutHook((engine, port, size, value, state) =>
        {
            observed.Add((port, size, value));
        });

        Assert.True(unicorn.TrySimulateOutHook(handle, 0x20, 1, 0xAA));
        Assert.Single(observed);
        Assert.Equal((0x20U, 1, 0xAAU), observed[0]);
    }

    [Fact]
    public void SyscallHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        object? observed = null;
        var handle = unicorn.AddSyscallHook((engine, state) =>
        {
            observed = state;
        }, state: "sys");

        Assert.True(unicorn.TrySimulateSyscallHook(handle));
        Assert.Equal("sys", observed);
    }

    [Fact]
    public void ControlCommand_RecordsTypedInvocation()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var command = Unicorn.ControlCommand.Read(Unicorn.ControlType.EngineMode, 1);
        nint argument = 1234;
        unicorn.Control(command, argument);

        Assert.True(native.LastControl.HasValue);
        Assert.Equal(command.Value, native.LastControl.Value.Control);
        Assert.Equal(new[] { argument }, native.LastControl.Value.Arguments);
    }

    [Fact]
    public void Control_TypeOverload_InfersArgumentCount()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        nint ptr = 5678;
        unicorn.Control(Unicorn.ControlType.PageSize, Unicorn.ControlIo.Read, ptr);

        var expected = Unicorn.ControlCommand.Read(Unicorn.ControlType.PageSize, 1);
        Assert.True(native.LastControl.HasValue);
        Assert.Equal(expected.Value, native.LastControl.Value.Control);
        Assert.Equal(new[] { ptr }, native.LastControl.Value.Arguments);
    }

    [Fact]
    public void ControlNone_IssuesZeroArgumentCommand()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        unicorn.ControlNone(Unicorn.ControlType.TlbFlush);

        var expected = Unicorn.ControlCommand.None(Unicorn.ControlType.TlbFlush, 0);
        Assert.True(native.LastControl.HasValue);
        Assert.Equal(expected.Value, native.LastControl.Value.Control);
        Assert.Empty(native.LastControl.Value.Arguments);
    }

    private sealed class FakeNativeProxy : IUnicornNativeProxy
    {
        private nuint _nextHandle;

        public bool Closed { get; private set; }

        public List<nuint> ActiveHooks { get; } = [];

        public List<nuint> RemovedHooks { get; } = [];

        public (uint Control, nint[] Arguments)? LastControl { get; private set; }

        public int Close(IntPtr engine)
        {
            Closed = true;
            return 0;
        }

        public int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookAddMem(IntPtr engine, Unicorn.HookType hookType, NativeMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookAddEventMem(IntPtr engine, Unicorn.HookType hookType, NativeEventMemHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookAddInterrupt(IntPtr engine, Unicorn.HookType hookType, NativeInterruptHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookAddInstructionIn(IntPtr engine, Unicorn.HookType hookType, NativeInstructionInHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookAddInstructionOut(IntPtr engine, Unicorn.HookType hookType, NativeInstructionOutHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookAddInstructionSyscall(IntPtr engine, Unicorn.HookType hookType, NativeSyscallHookCallback callback, IntPtr userData, ulong begin, ulong end, int instructionId, out nuint hookId)
        {
            return RegisterHook(out hookId);
        }

        public int HookDel(IntPtr engine, nuint hookId)
        {
            ActiveHooks.Remove(hookId);
            RemovedHooks.Add(hookId);
            return 0;
        }

        public int MemMap(IntPtr engine, ulong address, ulong size, uint permissions)
        {
            return 0;
        }

        public int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions)
        {
            return 0;
        }

        public int MemRead(IntPtr engine, ulong address, Span<byte> buffer)
        {
            buffer.Clear();
            return 0;
        }

        public int MemUnmap(IntPtr engine, ulong address, ulong size)
        {
            return 0;
        }

        public int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data)
        {
            return 0;
        }

        public int Open(int architecture, int mode, out IntPtr engine)
        {
            engine = new IntPtr(0x1234);
            return 0;
        }

        public int Control(IntPtr engine, uint control, ReadOnlySpan<nint> arguments)
        {
            var args = new nint[arguments.Length];
            arguments.CopyTo(args);
            LastControl = (control, args);
            return 0;
        }

        public int Errno(IntPtr engine) => 0;

        private int RegisterHook(out nuint hookId)
        {
            hookId = ++_nextHandle;
            ActiveHooks.Add(hookId);
            return 0;
        }
    }
}
