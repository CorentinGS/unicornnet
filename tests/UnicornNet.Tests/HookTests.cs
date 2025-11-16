using System;
using System.Collections.Generic;
using UnicornNet;
using Xunit;

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

    private sealed class FakeNativeProxy : IUnicornNativeProxy
    {
        private nuint _nextHandle = 0;

        public bool Closed { get; private set; }

        public List<nuint> ActiveHooks { get; } = [];

        public List<nuint> RemovedHooks { get; } = [];

        public int Close(IntPtr engine)
        {
            Closed = true;
            return 0;
        }

        public int HookAdd(IntPtr engine, Unicorn.HookType hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end, out nuint hookId)
        {
            hookId = ++_nextHandle;
            ActiveHooks.Add(hookId);
            return 0;
        }

        public int HookDel(IntPtr engine, nuint hookId)
        {
            ActiveHooks.Remove(hookId);
            RemovedHooks.Add(hookId);
            return 0;
        }

        public int MemMap(IntPtr engine, ulong address, ulong size, uint permissions) => 0;

        public int MemProtect(IntPtr engine, ulong address, ulong size, uint permissions) => 0;

        public int MemRead(IntPtr engine, ulong address, Span<byte> buffer)
        {
            buffer.Clear();
            return 0;
        }

        public int MemUnmap(IntPtr engine, ulong address, ulong size) => 0;

        public int MemWrite(IntPtr engine, ulong address, ReadOnlySpan<byte> data) => 0;

        public int Open(int architecture, int mode, out IntPtr engine)
        {
            engine = new IntPtr(0x1234);
            return 0;
        }
    }
}
