namespace UnicornNet;

public partial class Unicorn
{
    /// <summary>
    /// Map a memory region and return a MemoryRegion helper for managing it
    /// </summary>
    public MemoryRegion MapRegion(ulong address, ulong size, MemoryPermissions permissions)
    {
        MemMap(address, size, permissions);
        return new MemoryRegion(this, address, size, permissions);
    }

    public void MemMap(ulong address, ulong size, MemoryPermissions permissions)
    {
        EnsureNotDisposed();
        var err = _native.MemMap(EngineHandle, address, size, (uint)permissions);
        if (err != 0)
        {
            throw new UnicornMemoryException((ErrorCode)err, "uc_mem_map", address, size);
        }
    }

    public void MemMap(ulong address, ulong size, uint permissions)
    {
        MemMap(address, size, (MemoryPermissions)permissions);
    }

    public void MemMapPtr(ulong address, ulong size, MemoryPermissions permissions, IntPtr pointer)
    {
        EnsureNotDisposed();
        if (pointer == IntPtr.Zero)
        {
            throw new ArgumentException("Pointer cannot be zero.", nameof(pointer));
        }

        var err = _native.MemMapPtr(EngineHandle, address, size, (uint)permissions, pointer);
        if (err != 0)
        {
            throw new UnicornMemoryException((ErrorCode)err, "uc_mem_map_ptr", address, size);
        }
    }

    public void MemMapPtr(ulong address, ulong size, uint permissions, IntPtr pointer)
    {
        MemMapPtr(address, size, (MemoryPermissions)permissions, pointer);
    }

    public void MemUnmap(ulong address, ulong size)
    {
        EnsureNotDisposed();
        var err = _native.MemUnmap(EngineHandle, address, size);
        if (err != 0)
        {
            throw new UnicornMemoryException((ErrorCode)err, "uc_mem_unmap", address, size);
        }
    }

    public void MemProtect(ulong address, ulong size, MemoryPermissions permissions)
    {
        EnsureNotDisposed();
        var err = _native.MemProtect(EngineHandle, address, size, (uint)permissions);
        if (err != 0)
        {
            throw new UnicornMemoryException((ErrorCode)err, "uc_mem_protect", address, size);
        }
    }

    public void MemProtect(ulong address, ulong size, uint permissions)
    {
        MemProtect(address, size, (MemoryPermissions)permissions);
    }

    public void MemWrite(ulong address, ReadOnlySpan<byte> data)
    {
        EnsureNotDisposed();
        var err = _native.MemWrite(EngineHandle, address, data);
        if (err != 0)
        {
            throw new UnicornMemoryException((ErrorCode)err, "uc_mem_write", address, (ulong)data.Length);
        }
    }

    public void MemRead(ulong address, Span<byte> buffer)
    {
        EnsureNotDisposed();
        var err = _native.MemRead(EngineHandle, address, buffer);
        if (err != 0)
        {
            throw new UnicornMemoryException((ErrorCode)err, "uc_mem_read", address, (ulong)buffer.Length);
        }
    }
}
