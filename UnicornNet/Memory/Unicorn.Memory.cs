using System;

namespace UnicornNet;

public partial class Unicorn
{
    /// <summary>
    ///     Map a memory region and return a MemoryRegion helper for managing it
    /// </summary>
    public MemoryRegion MapRegion(ulong address, ulong size, MemoryPermissions permissions)
    {
        return _memory.Map(address, size, permissions);
    }

    public void MemMap(ulong address, ulong size, MemoryPermissions permissions)
    {
        _memory.Map(address, size, permissions);
    }

    public void MemMap(ulong address, ulong size, uint permissions)
    {
        MemMap(address, size, (MemoryPermissions)permissions);
    }

    public void MemMapPtr(ulong address, ulong size, MemoryPermissions permissions, IntPtr pointer)
    {
        _memory.MapPtr(address, size, permissions, pointer);
    }

    public void MemMapPtr(ulong address, ulong size, uint permissions, IntPtr pointer)
    {
        MemMapPtr(address, size, (MemoryPermissions)permissions, pointer);
    }

    public void MemUnmap(ulong address, ulong size)
    {
        _memory.Unmap(address, size);
    }

    public void MemProtect(ulong address, ulong size, MemoryPermissions permissions)
    {
        _memory.Protect(address, size, permissions);
    }

    public void MemProtect(ulong address, ulong size, uint permissions)
    {
        MemProtect(address, size, (MemoryPermissions)permissions);
    }

    public void MemWrite(ulong address, ReadOnlySpan<byte> data)
    {
        _memory.Write(address, data);
    }

    public void MemRead(ulong address, Span<byte> buffer)
    {
        _memory.Read(address, buffer);
    }
}
