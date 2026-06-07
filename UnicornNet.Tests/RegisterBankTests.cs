using System;
using Xunit;

namespace UnicornNet.Tests;

public sealed class RegisterBankTests
{
    [Fact]
    public void RegisterBank_WriteSerializesValueToNativeProxy()
    {
        var native = new FakeNativeProxy();
        var bank = new RegisterBank(native, () => new IntPtr(0x1234), () => { });
        const ulong value = 0x1122334455667788UL;

        bank.Write((int)Unicorn.Registers.X86.RAX, value);

        Assert.True(native.LastRegisterWrite.HasValue);
        Assert.Equal((int)Unicorn.Registers.X86.RAX, native.LastRegisterWrite.Value.RegisterId);
        Assert.Equal(BitConverter.GetBytes(value), native.LastRegisterWrite.Value.Value);
    }

    [Fact]
    public void RegisterBank_ReadDeserializesValueFromNativeProxy()
    {
        var native = new FakeNativeProxy();
        const int registerId = (int)Unicorn.Registers.Arm.R0;
        const uint expected = 0xAABBCCDD;
        native.RegisterValues[registerId] = BitConverter.GetBytes(expected);
        var bank = new RegisterBank(native, () => new IntPtr(0x1234), () => { });

        var actual = bank.Read<uint>(registerId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RegisterBank_NormalizesMultipleArchitectureRegisterEnums()
    {
        var native = new FakeNativeProxy();
        var bank = new RegisterBank(native, () => new IntPtr(0x1234), () => { });

        bank.Write(Unicorn.Registers.X86.RBX, 1UL);
        Assert.Equal((int)Unicorn.Registers.X86.RBX, native.LastRegisterWrite!.Value.RegisterId);

        bank.Write(Unicorn.Registers.Arm.R1, 2U);
        Assert.Equal((int)Unicorn.Registers.Arm.R1, native.LastRegisterWrite!.Value.RegisterId);
    }
}
