namespace UnicornNet;

public partial class Unicorn
{
    public void MemMap(ulong address, ulong size, MemoryPermissions permissions)
    {
        EnsureNotDisposed();
        var err = _native.MemMap(EngineHandle, address, size, (uint)permissions);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_map failed: error {err}");
        }
    }

    public void MemMap(ulong address, ulong size, uint permissions)
    {
        MemMap(address, size, (MemoryPermissions)permissions);
    }

    public void MemUnmap(ulong address, ulong size)
    {
        EnsureNotDisposed();
        var err = _native.MemUnmap(EngineHandle, address, size);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_unmap failed: error {err}");
        }
    }

    public void MemProtect(ulong address, ulong size, MemoryPermissions permissions)
    {
        EnsureNotDisposed();
        var err = _native.MemProtect(EngineHandle, address, size, (uint)permissions);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_protect failed: error {err}");
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
            throw new InvalidOperationException($"uc_mem_write failed: error {err}");
        }
    }

    public void MemRead(ulong address, Span<byte> buffer)
    {
        EnsureNotDisposed();
        var err = _native.MemRead(EngineHandle, address, buffer);
        if (err != 0)
        {
            throw new InvalidOperationException($"uc_mem_read failed: error {err}");
        }
    }
}