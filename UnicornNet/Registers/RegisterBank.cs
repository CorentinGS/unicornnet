using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnicornNet;

internal sealed class RegisterBank : IRegisterBank
{
    private readonly Action _ensureNotDisposed;
    private readonly Func<IntPtr> _getEngineHandle;
    private readonly IUnicornNativeProxy _native;

    public RegisterBank(IUnicornNativeProxy native, Func<IntPtr> getEngineHandle, Action ensureNotDisposed)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(getEngineHandle);
        ArgumentNullException.ThrowIfNull(ensureNotDisposed);

        _native = native;
        _getEngineHandle = getEngineHandle;
        _ensureNotDisposed = ensureNotDisposed;
    }

    public void Write(int registerId, ReadOnlySpan<byte> value)
    {
        _ensureNotDisposed();
        if (value.IsEmpty)
        {
            throw new ArgumentException("Value span cannot be empty.", nameof(value));
        }

        var err = _native.RegWrite(_getEngineHandle(), registerId, value);
        if (err != 0)
        {
            throw new UnicornEngineException((Unicorn.ErrorCode)err, "uc_reg_write");
        }
    }

    public void Write<TRegister>(TRegister register, ReadOnlySpan<byte> value)
        where TRegister : struct, Enum
    {
        Write(NormalizeRegisterId(register), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(int registerId, T value)
        where T : unmanaged
    {
        var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
        Write(registerId, MemoryMarshal.AsBytes(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<TRegister, TValue>(TRegister register, TValue value)
        where TRegister : struct, Enum
        where TValue : unmanaged
    {
        Write(NormalizeRegisterId(register), value);
    }

    public void Read(int registerId, Span<byte> destination)
    {
        _ensureNotDisposed();
        if (destination.IsEmpty)
        {
            throw new ArgumentException("Destination span cannot be empty.", nameof(destination));
        }

        var err = _native.RegRead(_getEngineHandle(), registerId, destination);
        if (err != 0)
        {
            throw new UnicornEngineException((Unicorn.ErrorCode)err, "uc_reg_read");
        }
    }

    public void Read<TRegister>(TRegister register, Span<byte> destination)
        where TRegister : struct, Enum
    {
        Read(NormalizeRegisterId(register), destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>(int registerId)
        where T : unmanaged
    {
        T value = default;
        var span = MemoryMarshal.CreateSpan(ref value, 1);
        Read(registerId, MemoryMarshal.AsBytes(span));
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Read<TRegister, TValue>(TRegister register)
        where TRegister : struct, Enum
        where TValue : unmanaged
    {
        return Read<TValue>(NormalizeRegisterId(register));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NormalizeRegisterId<TRegister>(TRegister register)
        where TRegister : struct, Enum
    {
        return Unsafe.As<TRegister, int>(ref register);
    }
}
