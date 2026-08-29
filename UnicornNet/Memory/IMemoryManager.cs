using System;

namespace UnicornNet;

public interface IMemoryManager
{
    MemoryRegion Map(ulong address, ulong size, Unicorn.MemoryPermissions permissions);

    void Unmap(ulong address, ulong size);

    void Protect(ulong address, ulong size, Unicorn.MemoryPermissions permissions);

    void Write(ulong address, ReadOnlySpan<byte> data);

    void Read(ulong address, Span<byte> buffer);
}
