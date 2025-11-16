namespace UnicornNet;

/// <summary>
/// Represents a mapped memory region with RAII semantics
/// </summary>
public readonly struct MemoryRegion : IDisposable
{
    private readonly Unicorn _engine;

    /// <summary>
    /// The base address of the memory region
    /// </summary>
    public ulong Address { get; }

    /// <summary>
    /// The size of the memory region in bytes
    /// </summary>
    public ulong Size { get; }

    /// <summary>
    /// The permissions for this memory region
    /// </summary>
    public Unicorn.MemoryPermissions Permissions { get; }

    internal MemoryRegion(Unicorn engine, ulong address, ulong size, Unicorn.MemoryPermissions permissions)
    {
        _engine = engine;
        Address = address;
        Size = size;
        Permissions = permissions;
    }

    /// <summary>
    /// Write data to this memory region
    /// </summary>
    public void Write(ReadOnlySpan<byte> data, ulong offset = 0)
    {
        if (offset + (ulong)data.Length > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Data exceeds region bounds");
        }

        _engine.MemWrite(Address + offset, data);
    }

    /// <summary>
    /// Read data from this memory region
    /// </summary>
    public void Read(Span<byte> buffer, ulong offset = 0)
    {
        if (offset + (ulong)buffer.Length > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer exceeds region bounds");
        }

        _engine.MemRead(Address + offset, buffer);
    }

    /// <summary>
    /// Change the permissions of this memory region
    /// </summary>
    public void Protect(Unicorn.MemoryPermissions permissions)
    {
        _engine.MemProtect(Address, Size, permissions);
    }

    /// <summary>
    /// Unmap this memory region
    /// </summary>
    public void Dispose()
    {
        _engine?.MemUnmap(Address, Size);
    }

    /// <summary>
    /// Returns the end address (Address + Size)
    /// </summary>
    public ulong EndAddress
    {
        get => Address + Size;
    }

    /// <summary>
    /// Check if an address is within this region
    /// </summary>
    public bool Contains(ulong address)
    {
        return address >= Address && address < EndAddress;
    }

    /// <summary>
    /// Check if an address range is fully contained within this region
    /// </summary>
    public bool Contains(ulong address, ulong size)
    {
        return address >= Address && (address + size) <= EndAddress;
    }
}
