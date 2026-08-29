using System;

namespace UnicornNet;

public interface IRegisterBank
{
    void Write(int registerId, ReadOnlySpan<byte> value);

    void Write<T>(int registerId, T value)
        where T : unmanaged;

    void Read(int registerId, Span<byte> destination);

    T Read<T>(int registerId)
        where T : unmanaged;
}
