using System;
using System.Collections.Generic;
using Xunit;
using MemoryAccessType = UnicornNet.Unicorn.MemoryAccessType;

namespace UnicornNet.Tests;

/// <summary>
/// Tests for Unicorn hook registration, invocation, and lifecycle management.
/// </summary>
public sealed class HookTests
{
    private const ulong TestAddress = 0x1000;
    private const int TestCodeSize = 4;
    private const string TestState = "test_state";
    [Fact]
    public void CodeHook_IsInvoked_WhenTriggered()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invocationCount = 0;
        object? observedState = null;
        const string expectedState = "code";
        const ulong expectedAddress = 0x1000;
        const int expectedSize = 4;

        var handle = unicorn.AddCodeHook((_, address, size, state) =>
        {
            invocationCount++;
            Assert.Equal(expectedAddress, address);
            Assert.Equal(expectedSize, size);
            observedState = state;
        }, state: expectedState);

        var simulationSucceeded = unicorn.TrySimulateHook(handle, expectedAddress, expectedSize);
        
        Assert.True(simulationSucceeded);
        Assert.Equal(1, invocationCount);
        Assert.Equal(expectedState, observedState);
    }

    [Fact]
    public void BlockHook_Removal_PreventsFurtherInvocations()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invocationCount = 0;
        var handle = unicorn.AddBlockHook((_, _, _, _) => invocationCount++);

        unicorn.RemoveHook(handle);

        var simulationAttempted = unicorn.TrySimulateHook(handle, 0x2000, 8);
        
        Assert.False(simulationAttempted);
        Assert.Equal(0, invocationCount);
        Assert.Contains(handle.Value, native.RemovedHooks);
    }

    [Fact]
    public void HookDel_RemovesHook()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var handle = unicorn.AddCodeHook((_, _, _, _) => { });

        unicorn.HookDel(handle);

        var simulationAttempted = unicorn.TrySimulateHook(handle, 0x3000, 2);
        
        Assert.False(simulationAttempted);
        Assert.Single(native.RemovedHooks);
        Assert.Contains(handle.Value, native.RemovedHooks);
    }

    [Fact]
    public void Dispose_RemovesAllHooks()
    {
        var native = new FakeNativeProxy();
        var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var firstHook = unicorn.AddCodeHook((_, _, _, _) => { });
        var secondHook = unicorn.AddBlockHook((_, _, _, _) => { });

        unicorn.Dispose();

        Assert.True(native.Closed);
        Assert.Contains(firstHook.Value, native.RemovedHooks);
        Assert.Contains(secondHook.Value, native.RemovedHooks);
        Assert.Equal(2, native.RemovedHooks.Count);
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
        
        const string expectedState = "mem";
        const ulong expectedAddress = 0x4000;
        const int expectedSize = 4;
        const long expectedValue = 0x12;

        var handle = unicorn.AddMemReadHook((engine, type, address, size, value, state) =>
        {
            observedType = type;
            observedAddress = address;
            observedSize = size;
            observedValue = value;
            observedState = state;
        }, state: expectedState);

        var simulationSucceeded = unicorn.TrySimulateMemoryHook(handle, MemoryAccessType.Read, expectedAddress, expectedSize, expectedValue);
        
        Assert.True(simulationSucceeded);
        Assert.Equal(MemoryAccessType.Read, observedType);
        Assert.Equal(expectedAddress, observedAddress);
        Assert.Equal(expectedSize, observedSize);
        Assert.Equal(expectedValue, observedValue);
        Assert.Equal(expectedState, observedState);
    }

    [Fact]
    public void MemWriteHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var wasInvokedCorrectly = false;
        const ulong expectedAddress = 0x5000;
        const int expectedSize = 2;
        const long expectedValue = 0xFF;
        
        var handle = unicorn.AddMemWriteHook((engine, type, address, size, value, state) =>
        {
            wasInvokedCorrectly = type == MemoryAccessType.Write 
                && address == expectedAddress 
                && size == expectedSize 
                && value == expectedValue;
        });

        var simulationSucceeded = unicorn.TrySimulateMemoryHook(handle, MemoryAccessType.Write, expectedAddress, expectedSize, expectedValue);
        
        Assert.True(simulationSucceeded);
        Assert.True(wasInvokedCorrectly);
    }

    [Fact]
    public void EventMemHook_TracksPerEventType()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invocations = new List<string>();
        const int firstState = 1;
        const int secondState = 2;
        const ulong testAddress = 0x6000;
        const int testSize = 8;
        const long testValue = 0;
        
        var firstHook = unicorn.AddEventMemHook(Unicorn.HookType.MemReadUnmapped,
            (engine, type, address, size, value, state) =>
            {
                invocations.Add($"first:{state}");
                return true;
            }, state: firstState);

        var secondHook = unicorn.AddEventMemHook(Unicorn.HookType.MemReadUnmapped,
            (engine, type, address, size, value, state) =>
            {
                invocations.Add($"second:{state}");
                return false;
            }, state: secondState);

        var bothHooksInvoked = unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, testAddress, testSize, testValue);
        Assert.True(bothHooksInvoked);
        Assert.Equal(new[] { "first:1", "second:2" }, invocations);

        invocations.Clear();
        unicorn.RemoveHook(firstHook);
        
        var secondHookInvoked = unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, testAddress, testSize, testValue);
        Assert.True(secondHookInvoked);
        Assert.Equal(new[] { "second:2" }, invocations);

        unicorn.RemoveHook(secondHook);
        var noHooksRemain = unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, testAddress, testSize, testValue);
        Assert.False(noHooksRemain);
    }

    [Fact]
    public void EventMemHook_AllowsHookTypeMaskRegistration()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var observedTypes = new List<MemoryAccessType>();
        var mask = Unicorn.HookType.MemUnmapped | Unicorn.HookType.MemProt;
        var handle = unicorn.AddEventMemHook(mask, (engine, type, address, size, value, state) =>
        {
            observedTypes.Add(type);
            return true;
        });

        var unmappedTriggered = unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, TestAddress, TestCodeSize, 0);
        var protectedTriggered = unicorn.TrySimulateEventMem(MemoryAccessType.WriteProtected, TestAddress, TestCodeSize, 0);

        Assert.True(unmappedTriggered);
        Assert.True(protectedTriggered);
        Assert.Contains(MemoryAccessType.ReadUnmapped, observedTypes);
        Assert.Contains(MemoryAccessType.WriteProtected, observedTypes);

        unicorn.RemoveHook(handle);

        var noEventTriggered = unicorn.TrySimulateEventMem(MemoryAccessType.ReadUnmapped, TestAddress, TestCodeSize, 0);
        Assert.False(noEventTriggered);
    }

    [Fact]
    public void InterruptHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        uint observedInterrupt = 0;
        object? observedState = null;
        const string expectedState = "intr";
        const uint expectedInterrupt = 0x21;

        var handle = unicorn.AddInterruptHook((engine, interrupt, state) =>
        {
            observedInterrupt = interrupt;
            observedState = state;
        }, state: expectedState);

        var simulationSucceeded = unicorn.TrySimulateInterruptHook(handle, expectedInterrupt);
        
        Assert.True(simulationSucceeded);
        Assert.Equal(expectedInterrupt, observedInterrupt);
        Assert.Equal(expectedState, observedState);
    }

    [Fact]
    public void InHook_ReturnsValue()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const uint testPort = 0x33;
        const int testSize = 4;
        const uint expectedValue = testPort + testSize;
        
        var handle = unicorn.AddInHook((engine, port, size, state) => port + (uint)size);

        var simulationSucceeded = unicorn.TrySimulateInHook(handle, testPort, testSize, out var actualValue);
        
        Assert.True(simulationSucceeded);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void OutHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var capturedCalls = new List<(uint port, int size, uint value)>();
        const uint testPort = 0x20;
        const int testSize = 1;
        const uint testValue = 0xAA;
        
        var handle = unicorn.AddOutHook((engine, port, size, value, state) =>
        {
            capturedCalls.Add((port, size, value));
        });

        var simulationSucceeded = unicorn.TrySimulateOutHook(handle, testPort, testSize, testValue);
        
        Assert.True(simulationSucceeded);
        Assert.Single(capturedCalls);
        Assert.Equal((testPort, testSize, testValue), capturedCalls[0]);
    }

    [Fact]
    public void SyscallHook_IsInvoked()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        object? observedState = null;
        const string expectedState = "sys";

        var handle = unicorn.AddSyscallHook((engine, state) =>
        {
            observedState = state;
        }, state: expectedState);

        var simulationSucceeded = unicorn.TrySimulateSyscallHook(handle);
        
        Assert.True(simulationSucceeded);
        Assert.Equal(expectedState, observedState);
    }

    [Fact]
    public void ControlCommand_RecordsTypedInvocation()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const int argumentCount = 1;
        var command = Unicorn.ControlCommand.Read(Unicorn.ControlType.EngineMode, argumentCount);
        nint expectedArgument = 1234;
        
        unicorn.Control(command, expectedArgument);

        Assert.True(native.LastControl.HasValue);
        var (actualCommand, actualArguments) = native.LastControl.Value;
        Assert.Equal(command.Value, actualCommand);
        Assert.Equal(new[] { expectedArgument }, actualArguments);
    }

    [Fact]
    public void Control_TypeOverload_InfersArgumentCount()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        nint expectedArgument = 5678;
        const int expectedArgumentCount = 1;
        
        unicorn.Control(Unicorn.ControlType.PageSize, Unicorn.ControlIo.Read, expectedArgument);

        var expectedCommand = Unicorn.ControlCommand.Read(Unicorn.ControlType.PageSize, expectedArgumentCount);
        Assert.True(native.LastControl.HasValue);
        var (actualCommand, actualArguments) = native.LastControl.Value;
        Assert.Equal(expectedCommand.Value, actualCommand);
        Assert.Equal(new[] { expectedArgument }, actualArguments);
    }

    [Fact]
    public void ControlNone_IssuesZeroArgumentCommand()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const int expectedArgumentCount = 0;
        unicorn.ControlNone(Unicorn.ControlType.TlbFlush);

        var expectedCommand = Unicorn.ControlCommand.None(Unicorn.ControlType.TlbFlush, expectedArgumentCount);
        Assert.True(native.LastControl.HasValue);
        var (actualCommand, actualArguments) = native.LastControl.Value;
        Assert.Equal(expectedCommand.Value, actualCommand);
        Assert.Empty(actualArguments);
    }

    [Fact]
    public void RemoveCache_InvokesTranslationBlockRemoveControl()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const ulong startAddress = 0x1000;
        const ulong endAddress = 0x2000;

        unicorn.RemoveCache(startAddress, endAddress);

        var expectedCommand = Unicorn.ControlCommand.Write(Unicorn.ControlType.TranslationBlockRemove, 2);
        Assert.True(native.LastControl.HasValue);
        var (actualCommand, actualArguments) = native.LastControl.Value;
        Assert.Equal(expectedCommand.Value, actualCommand);
        Assert.Equal(new[] { (nint)startAddress, (nint)endAddress }, actualArguments);
    }

    [Fact]
    public void FlushCache_InvokesTranslationBlockFlushControl()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        unicorn.FlushCache();

        var expectedCommand = Unicorn.ControlCommand.Write(Unicorn.ControlType.TranslationBlockFlush, 0);
        Assert.True(native.LastControl.HasValue);
        var (actualCommand, actualArguments) = native.LastControl.Value;
        Assert.Equal(expectedCommand.Value, actualCommand);
        Assert.Empty(actualArguments);
    }

    [Fact]
    public void InvalidInstructionHook_ReturnsTrue_ContinuesExecution()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invoked = false;
        object? observedState = null;
        const string expectedState = "invalid_insn";

        var handle = unicorn.AddInvalidInstructionHook((engine, state) =>
        {
            invoked = true;
            observedState = state;
            return true; // Continue execution
        }, state: expectedState);

        var simulationSucceeded = unicorn.TrySimulateInvalidInstructionHook(handle, out var continueExecution);

        Assert.True(simulationSucceeded);
        Assert.True(invoked);
        Assert.Equal(expectedState, observedState);
        Assert.True(continueExecution);
    }

    [Fact]
    public void InvalidInstructionHook_ReturnsFalse_StopsExecution()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var invoked = false;

        var handle = unicorn.AddInvalidInstructionHook((engine, state) =>
        {
            invoked = true;
            return false; // Stop execution
        });

        var simulationSucceeded = unicorn.TrySimulateInvalidInstructionHook(handle, out var continueExecution);

        Assert.True(simulationSucceeded);
        Assert.True(invoked);
        Assert.False(continueExecution);
    }
}
