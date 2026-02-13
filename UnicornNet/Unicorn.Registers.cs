using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnicornNet;

public partial class Unicorn
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NormalizeRegisterId<TRegister>(TRegister register)
        where TRegister : struct, Enum
    {
        return Unsafe.As<TRegister, int>(ref register);
    }

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

    public void RegWrite<TRegister>(TRegister register, ReadOnlySpan<byte> value)
        where TRegister : struct, Enum
    {
        RegWrite(NormalizeRegisterId(register), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegWrite<T>(int registerId, T value)
        where T : unmanaged
    {
        var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
        RegWrite(registerId, MemoryMarshal.AsBytes(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegWrite<TRegister, TValue>(TRegister register, TValue value)
        where TRegister : struct, Enum
        where TValue : unmanaged
    {
        RegWrite(NormalizeRegisterId(register), value);
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

    public void RegRead<TRegister>(TRegister register, Span<byte> destination)
        where TRegister : struct, Enum
    {
        RegRead(NormalizeRegisterId(register), destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T RegRead<T>(int registerId)
        where T : unmanaged
    {
        T value = default;
        var span = MemoryMarshal.CreateSpan(ref value, 1);
        RegRead(registerId, MemoryMarshal.AsBytes(span));
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue RegRead<TRegister, TValue>(TRegister register)
        where TRegister : struct, Enum
        where TValue : unmanaged
    {
        return RegRead<TValue>(NormalizeRegisterId(register));
    }

    #region Register Enum Overloads

    public void RegWrite(Registers.Arm register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.Arm register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.Arm register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.Arm register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.Arm64 register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.Arm64 register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.Arm64 register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.Arm64 register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.M68k register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.M68k register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.M68k register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.M68k register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.Mips register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.Mips register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.Mips register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.Mips register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.Ppc register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.Ppc register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.Ppc register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.Ppc register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.Riscv register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.Riscv register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.Riscv register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.Riscv register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.S390x register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.S390x register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.S390x register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.S390x register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.Sparc register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.Sparc register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.Sparc register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.Sparc register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.TriCore register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.TriCore register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.TriCore register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.TriCore register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    public void RegWrite(Registers.X86 register, ReadOnlySpan<byte> value)
    {
        RegWrite((int)register, value);
    }
    public void RegWrite<T>(Registers.X86 register, T value) where T : unmanaged
    {
        RegWrite((int)register, value);
    }
    public void RegRead(Registers.X86 register, Span<byte> destination)
    {
        RegRead((int)register, destination);
    }
    public T RegRead<T>(Registers.X86 register) where T : unmanaged
    {
        return RegRead<T>((int)register);
    }

    #endregion
}