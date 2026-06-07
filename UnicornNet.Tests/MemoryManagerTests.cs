using System;
using Xunit;

namespace UnicornNet.Tests;

public sealed class MemoryManagerTests
{
    private const ulong BaseAddress = 0x10000;
    private const ulong RegionSize = 0x1000;

    [Fact]
    public void MemoryRegion_UsesMemoryManagerForReadWriteProtectAndDispose()
    {
        var memory = new FakeMemoryManager();
        var region = new MemoryRegion(memory, BaseAddress, RegionSize, Unicorn.MemoryPermissions.All);
        var data = new byte[] { 1, 2, 3 };
        Span<byte> buffer = stackalloc byte[3];

        region.Write(data, 4);
        region.Read(buffer, 4);
        region.Protect(Unicorn.MemoryPermissions.Read);
        region.Dispose();

        Assert.Equal((BaseAddress + 4, data), memory.LastWrite);
        Assert.Equal((BaseAddress + 4, 3), memory.LastRead);
        Assert.Equal((BaseAddress, RegionSize, Unicorn.MemoryPermissions.Read), memory.LastProtect);
        Assert.Equal((BaseAddress, RegionSize), memory.LastUnmap);
    }

    [Fact]
    public void MemoryRegion_RejectsWritesOutsideRegionBounds()
    {
        var memory = new FakeMemoryManager();
        var region = new MemoryRegion(memory, BaseAddress, RegionSize, Unicorn.MemoryPermissions.All);
        var data = new byte[2];

        Assert.Throws<ArgumentOutOfRangeException>(() => region.Write(data, RegionSize));
    }

    [Fact]
    public void MemoryRegion_RejectsReadsOutsideRegionBounds()
    {
        var memory = new FakeMemoryManager();
        var region = new MemoryRegion(memory, BaseAddress, RegionSize, Unicorn.MemoryPermissions.All);
        var buffer = new byte[2];

        Assert.Throws<ArgumentOutOfRangeException>(() => region.Read(buffer, RegionSize));
    }

    [Fact]
    public void MemoryManager_MapCreatesRegionAndForwardsToNativeProxy()
    {
        var native = new FakeNativeProxy();
        var manager = new MemoryManager(native, () => new IntPtr(0x1234), () => { });

        var region = manager.Map(BaseAddress, RegionSize, Unicorn.MemoryPermissions.Read);

        Assert.Equal(BaseAddress, region.Address);
        Assert.Equal(RegionSize, region.Size);
        Assert.Equal(Unicorn.MemoryPermissions.Read, region.Permissions);
        Assert.Equal((BaseAddress, RegionSize, (uint)Unicorn.MemoryPermissions.Read), native.LastMemMap);
    }

    [Fact]
    public void MemoryManager_ReadAndWriteForwardToNativeProxy()
    {
        var native = new FakeNativeProxy();
        var manager = new MemoryManager(native, () => new IntPtr(0x1234), () => { });
        var data = new byte[] { 0xAA, 0xBB };
        Span<byte> buffer = stackalloc byte[2];

        manager.Write(BaseAddress, data);
        manager.Read(BaseAddress + 8, buffer);

        Assert.True(native.LastMemWrite.HasValue);
        Assert.Equal(BaseAddress, native.LastMemWrite.Value.Address);
        Assert.Equal(data, native.LastMemWrite.Value.Data);
        Assert.Equal((BaseAddress + 8, 2), native.LastMemRead);
    }
}

internal sealed class FakeMemoryManager : IMemoryManager
{
    public (ulong Address, ulong Size, Unicorn.MemoryPermissions Permissions)? LastMap { get; private set; }

    public (ulong Address, ulong Size)? LastUnmap { get; private set; }

    public (ulong Address, ulong Size, Unicorn.MemoryPermissions Permissions)? LastProtect { get; private set; }

    public (ulong Address, byte[] Data)? LastWrite { get; private set; }

    public (ulong Address, int Length)? LastRead { get; private set; }

    public MemoryRegion Map(ulong address, ulong size, Unicorn.MemoryPermissions permissions)
    {
        LastMap = (address, size, permissions);
        return new MemoryRegion(this, address, size, permissions);
    }

    public void Unmap(ulong address, ulong size)
    {
        LastUnmap = (address, size);
    }

    public void Protect(ulong address, ulong size, Unicorn.MemoryPermissions permissions)
    {
        LastProtect = (address, size, permissions);
    }

    public void Write(ulong address, ReadOnlySpan<byte> data)
    {
        LastWrite = (address, data.ToArray());
    }

    public void Read(ulong address, Span<byte> buffer)
    {
        LastRead = (address, buffer.Length);
        buffer.Clear();
    }
}
