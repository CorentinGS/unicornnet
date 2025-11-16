using System;
using Xunit;

namespace UnicornNet.Tests;

public sealed class NewFeaturesTests
{
    [Fact]
    public void MemoryRegion_TracksAddressAndSize()
    {
        // Create a memory region directly to test its properties
        var region = new MemoryRegion(null!, 0x10000, 0x1000, Unicorn.MemoryPermissions.All);

        Assert.Equal(0x10000UL, region.Address);
        Assert.Equal(0x1000UL, region.Size);
        Assert.Equal(0x11000UL, region.EndAddress);
        Assert.Equal(Unicorn.MemoryPermissions.All, region.Permissions);
    }

    [Fact]
    public void MemoryRegion_ContainsCheck_ReturnsCorrectResult()
    {
        var region = new MemoryRegion(null!, 0x10000, 0x1000, Unicorn.MemoryPermissions.All);

        Assert.True(region.Contains(0x10000));
        Assert.True(region.Contains(0x10500));
        Assert.True(region.Contains(0x10FFF));
        Assert.False(region.Contains(0x0FFF));
        Assert.False(region.Contains(0x11000));
    }

    [Fact]
    public void MemoryRegion_ContainsRange_ReturnsCorrectResult()
    {
        var region = new MemoryRegion(null!, 0x10000, 0x1000, Unicorn.MemoryPermissions.All);

        Assert.True(region.Contains(0x10000, 0x100));
        Assert.True(region.Contains(0x10000, 0x1000));
        Assert.False(region.Contains(0x10000, 0x1001));
        Assert.False(region.Contains(0x0FFF, 0x100));
        Assert.False(region.Contains(0x10F00, 0x200));
    }

    [Fact]
    public void CustomException_ContainsErrorCode()
    {
        var exception = new UnicornEngineException(Unicorn.ErrorCode.Arch, "test_operation");

        Assert.Equal(Unicorn.ErrorCode.Arch, exception.ErrorCode);
        Assert.Equal("test_operation", exception.Operation);
        Assert.Contains("Arch", exception.Message);
        Assert.Contains("test_operation", exception.Message);
    }

    [Fact]
    public void MemoryException_ContainsAddressInfo()
    {
        var exception = new UnicornMemoryException(
            Unicorn.ErrorCode.WriteUnmapped,
            "uc_mem_write",
            address: 0xDEADBEEF,
            size: 0x100);

        Assert.Equal(Unicorn.ErrorCode.WriteUnmapped, exception.ErrorCode);
        Assert.Equal(0xDEADBEEFUL, exception.Address);
        Assert.Equal(0x100UL, exception.Size);
        Assert.Contains("DEADBEEF", exception.Message);
    }

    [Fact]
    public void HookException_ContainsHookType()
    {
        var exception = new UnicornHookException(
            Unicorn.ErrorCode.Hook,
            "uc_hook_add",
            hookType: Unicorn.HookType.Code);

        Assert.Equal(Unicorn.ErrorCode.Hook, exception.ErrorCode);
        Assert.Equal(Unicorn.HookType.Code, exception.HookType);
        Assert.Contains("Code", exception.Message);
    }

}
