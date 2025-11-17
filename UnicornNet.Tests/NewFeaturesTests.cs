using System;
using Xunit;

namespace UnicornNet.Tests;

/// <summary>
/// Tests for new features added to UnicornNet, including memory regions, exceptions, and advanced operations.
/// </summary>
public sealed class NewFeaturesTests
{
    private const ulong BaseAddress = 0x10000;
    private const ulong RegionSize = 0x1000;
    [Fact]
    public void MemoryRegion_TracksAddressAndSize()
    {
        var region = new MemoryRegion(null!, BaseAddress, RegionSize, Unicorn.MemoryPermissions.All);
        var expectedEndAddress = BaseAddress + RegionSize;

        Assert.Equal(BaseAddress, region.Address);
        Assert.Equal(RegionSize, region.Size);
        Assert.Equal(expectedEndAddress, region.EndAddress);
        Assert.Equal(Unicorn.MemoryPermissions.All, region.Permissions);
    }

    [Fact]
    public void MemoryRegion_ContainsCheck_ReturnsCorrectResult()
    {
        var region = new MemoryRegion(null!, BaseAddress, RegionSize, Unicorn.MemoryPermissions.All);
        var middleAddress = BaseAddress + 0x500;
        var lastAddress = BaseAddress + RegionSize - 1;
        var beforeStart = BaseAddress - 1;
        var afterEnd = BaseAddress + RegionSize;

        Assert.True(region.Contains(BaseAddress));
        Assert.True(region.Contains(middleAddress));
        Assert.True(region.Contains(lastAddress));
        Assert.False(region.Contains(beforeStart));
        Assert.False(region.Contains(afterEnd));
    }

    [Fact]
    public void MemoryRegion_ContainsRange_ReturnsCorrectResult()
    {
        var region = new MemoryRegion(null!, BaseAddress, RegionSize, Unicorn.MemoryPermissions.All);
        const ulong smallSize = 0x100;
        const ulong exceedsRegionByOne = RegionSize + 1;
        const ulong largeSize = 0x200;

        Assert.True(region.Contains(BaseAddress, smallSize));
        Assert.True(region.Contains(BaseAddress, RegionSize));
        Assert.False(region.Contains(BaseAddress, exceedsRegionByOne));
        Assert.False(region.Contains(BaseAddress - 1, smallSize));
        var overlapsEndAddress = BaseAddress + RegionSize - (largeSize / 2);
        Assert.False(region.Contains(overlapsEndAddress, largeSize));
    }

    [Fact]
    public void CustomException_ContainsErrorCode()
    {
        const string operationName = "test_operation";
        var exception = new UnicornEngineException(Unicorn.ErrorCode.Arch, operationName);

        Assert.Equal(Unicorn.ErrorCode.Arch, exception.ErrorCode);
        Assert.Equal(operationName, exception.Operation);
        Assert.Contains("Arch", exception.Message);
        Assert.Contains(operationName, exception.Message);
    }

    [Fact]
    public void MemoryException_ContainsAddressInfo()
    {
        const ulong testAddress = 0xDEADBEEF;
        const ulong testSize = 0x100;
        const string operationName = "uc_mem_write";
        
        var exception = new UnicornMemoryException(
            Unicorn.ErrorCode.WriteUnmapped,
            operationName,
            address: testAddress,
            size: testSize);

        Assert.Equal(Unicorn.ErrorCode.WriteUnmapped, exception.ErrorCode);
        Assert.Equal(testAddress, exception.Address);
        Assert.Equal(testSize, exception.Size);
        Assert.Contains("DEADBEEF", exception.Message);
    }

    [Fact]
    public void HookException_ContainsHookType()
    {
        const string operationName = "uc_hook_add";
        const Unicorn.HookType expectedHookType = Unicorn.HookType.Code;
        
        var exception = new UnicornHookException(
            Unicorn.ErrorCode.Hook,
            operationName,
            hookType: expectedHookType);

        Assert.Equal(Unicorn.ErrorCode.Hook, exception.ErrorCode);
        Assert.Equal(expectedHookType, exception.HookType);
        Assert.Contains("Code", exception.Message);
    }

    [Fact]
    public void MemMapPtr_RecordsRequest()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const ulong address = 0x4000;
        const ulong size = 0x2000;
        const Unicorn.MemoryPermissions permissions = Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write;
        var pointer = new IntPtr(0xABCDEF);
        
        unicorn.MemMapPtr(address, size, permissions, pointer);

        Assert.Single(native.MemMapPtrRequests);
        var request = native.MemMapPtrRequests[0];
        Assert.Equal(address, request.Address);
        Assert.Equal(size, request.Size);
        Assert.Equal((uint)permissions, request.Permissions);
        Assert.Equal(pointer, request.Pointer);
    }

    [Fact]
    public void MemMapPtr_ThrowsOnNullPointer()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const ulong address = 0x4000;
        const ulong size = 0x1000;
        
        Assert.Throws<ArgumentException>(() => 
            unicorn.MemMapPtr(address, size, Unicorn.MemoryPermissions.All, IntPtr.Zero));
    }

    [Fact]
    public void RegWrite_CopiesBytesToNativeProxy()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const int registerId = 2;
        var testData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        
        unicorn.RegWrite(registerId, testData);

        Assert.True(native.LastRegisterWrite.HasValue);
        Assert.Equal(registerId, native.LastRegisterWrite.Value.RegisterId);
        Assert.Equal(testData, native.LastRegisterWrite.Value.Value);
    }

    [Fact]
    public void RegWrite_GenericOverloadSerializesValue()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var register = Unicorn.Registers.X86.RAX;
        const ulong testValue = 0x1122334455667788UL;
        
        unicorn.RegWrite(register, testValue);

        Assert.True(native.LastRegisterWrite.HasValue);
        Assert.Equal((int)register, native.LastRegisterWrite.Value.RegisterId);
        Assert.Equal(BitConverter.GetBytes(testValue), native.LastRegisterWrite.Value.Value);
    }

    [Fact]
    public void RegRead_FillsDestinationBuffer()
    {
        var native = new FakeNativeProxy();
        const int registerId = 5;
        var expectedData = new byte[] { 1, 2, 3, 4 };
        native.RegisterValues[registerId] = expectedData;
        
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        Span<byte> buffer = stackalloc byte[4];
        unicorn.RegRead(registerId, buffer);

        Assert.Equal(expectedData, buffer.ToArray());
    }

    [Fact]
    public void RegRead_GenericReturnsTypedValue()
    {
        var native = new FakeNativeProxy();
        var register = Unicorn.Registers.X86.RBX;
        const long expectedValue = 0x0102030405060708;
        native.RegisterValues[(int)register] = BitConverter.GetBytes(expectedValue);
        
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        var actualValue = unicorn.RegRead<Unicorn.Registers.X86, long>(register);

        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void RegWrite_ThrowsWhenNativeFails()
    {
        var native = new FailingRegWriteProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const int registerId = 1;
        var testData = new byte[] { 0x1 };
        
        Assert.Throws<UnicornEngineException>(() => unicorn.RegWrite(registerId, testData));
    }

    [Fact]
    public void RegRead_ThrowsWhenNativeFails()
    {
        var native = new FailingRegReadProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const int registerId = 1;
        var buffer = new byte[8];
        
        Assert.Throws<UnicornEngineException>(() => unicorn.RegRead(registerId, buffer));
    }

    [Fact]
    public void MemMapPtr_ThrowsWhenNativeFails()
    {
        var native = new FailingMemMapPtrProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const ulong address = 0x0;
        const ulong size = 0x1000;
        var pointer = new IntPtr(1);
        
        Assert.Throws<UnicornMemoryException>(() => 
            unicorn.MemMapPtr(address, size, Unicorn.MemoryPermissions.Read, pointer));
    }

    [Fact]
    public void EmuStart_ForwardsArgumentsToNativeProxy()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        const ulong begin = 0x1000;
        const ulong end = 0x2000;
        const ulong timeout = 10;
        const ulong count = 5;

        unicorn.EmuStart(begin, end, timeout, count);

        Assert.True(native.LastEmuStart.HasValue);
        Assert.Equal((begin, end, timeout, (nuint)count), native.LastEmuStart.Value);
    }

    [Fact]
    public void EmuStop_SetsFlagOnNativeProxy()
    {
        var native = new FakeNativeProxy();
        using var unicorn = new Unicorn(Unicorn.Architecture.X86, Unicorn.Mode.Mode32, native);

        unicorn.EmuStop();

        Assert.True(native.EmulationStopped);
    }

    private sealed class FailingRegWriteProxy : FakeNativeProxy
    {
        public override int RegWrite(IntPtr engine, int registerId, ReadOnlySpan<byte> value)
        {
            return (int)Unicorn.ErrorCode.Argument;
        }
    }

    private sealed class FailingRegReadProxy : FakeNativeProxy
    {
        public override int RegRead(IntPtr engine, int registerId, Span<byte> buffer)
        {
            return (int)Unicorn.ErrorCode.Resource;
        }
    }

    private sealed class FailingMemMapPtrProxy : FakeNativeProxy
    {
        public override int MemMapPtr(IntPtr engine, ulong address, ulong size, uint permissions, IntPtr pointer)
        {
            return (int)Unicorn.ErrorCode.Map;
        }
    }
}
