using System.Runtime.InteropServices;

namespace UnicornNet;

public partial class Unicorn
{
    public void RegWrite(int registerId, ReadOnlySpan<byte> value)
    {
        EnsureNotDisposed();
        if (value.IsEmpty)
        {
            throw new ArgumentException("Value span cannot be empty.", nameof(value));
        }

        var err = _native.RegWrite(EngineHandle, registerId, value);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_reg_write");
        }
    }

    public void RegWrite<T>(int registerId, T value)
        where T : unmanaged
    {
        ReadOnlySpan<T> span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
        RegWrite(registerId, MemoryMarshal.AsBytes(span));
    }

    public void RegRead(int registerId, Span<byte> destination)
    {
        EnsureNotDisposed();
        if (destination.IsEmpty)
        {
            throw new ArgumentException("Destination span cannot be empty.", nameof(destination));
        }

        var err = _native.RegRead(EngineHandle, registerId, destination);
        if (err != 0)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_reg_read");
        }
    }

    public T RegRead<T>(int registerId)
        where T : unmanaged
    {
        T value = default;
        Span<T> span = MemoryMarshal.CreateSpan(ref value, 1);
        RegRead(registerId, MemoryMarshal.AsBytes(span));
        return value;
    }
}
