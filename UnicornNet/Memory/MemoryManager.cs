using System;

namespace UnicornNet;

internal sealed class MemoryManager : IMemoryManager
{
    private readonly Action _ensureNotDisposed;
    private readonly Func<IntPtr> _getEngineHandle;
    private readonly IUnicornNativeProxy _native;

    public MemoryManager(IUnicornNativeProxy native, Func<IntPtr> getEngineHandle, Action ensureNotDisposed)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(getEngineHandle);
        ArgumentNullException.ThrowIfNull(ensureNotDisposed);

        _native = native;
        _getEngineHandle = getEngineHandle;
        _ensureNotDisposed = ensureNotDisposed;
    }

    public MemoryRegion Map(ulong address, ulong size, Unicorn.MemoryPermissions permissions)
    {
        _ensureNotDisposed();
        var err = _native.MemMap(_getEngineHandle(), address, size, (uint)permissions);
        if (err != 0)
        {
            throw new UnicornMemoryException((Unicorn.ErrorCode)err, "uc_mem_map", address, size);
        }

        return new MemoryRegion(this, address, size, permissions);
    }

    public void MapPtr(ulong address, ulong size, Unicorn.MemoryPermissions permissions, IntPtr pointer)
    {
        _ensureNotDisposed();
        if (pointer == IntPtr.Zero)
        {
            throw new ArgumentException("Pointer cannot be zero.", nameof(pointer));
        }

        var err = _native.MemMapPtr(_getEngineHandle(), address, size, (uint)permissions, pointer);
        if (err != 0)
        {
            throw new UnicornMemoryException((Unicorn.ErrorCode)err, "uc_mem_map_ptr", address, size);
        }
    }

    public void Unmap(ulong address, ulong size)
    {
        _ensureNotDisposed();
        var err = _native.MemUnmap(_getEngineHandle(), address, size);
        if (err != 0)
        {
            throw new UnicornMemoryException((Unicorn.ErrorCode)err, "uc_mem_unmap", address, size);
        }
    }

    public void Protect(ulong address, ulong size, Unicorn.MemoryPermissions permissions)
    {
        _ensureNotDisposed();
        var err = _native.MemProtect(_getEngineHandle(), address, size, (uint)permissions);
        if (err != 0)
        {
            throw new UnicornMemoryException((Unicorn.ErrorCode)err, "uc_mem_protect", address, size);
        }
    }

    public void Write(ulong address, ReadOnlySpan<byte> data)
    {
        _ensureNotDisposed();
        var err = _native.MemWrite(_getEngineHandle(), address, data);
        if (err != 0)
        {
            throw new UnicornMemoryException((Unicorn.ErrorCode)err, "uc_mem_write", address, (ulong)data.Length);
        }
    }

    public void Read(ulong address, Span<byte> buffer)
    {
        _ensureNotDisposed();
        var err = _native.MemRead(_getEngineHandle(), address, buffer);
        if (err != 0)
        {
            throw new UnicornMemoryException((Unicorn.ErrorCode)err, "uc_mem_read", address, (ulong)buffer.Length);
        }
    }
}
