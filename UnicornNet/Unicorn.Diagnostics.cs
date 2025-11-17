namespace UnicornNet;

public partial class Unicorn
{
    /// <summary>
    ///     Creates a detailed context object for an invalid instruction error.
    ///     This captures the current program counter and attempts to read the instruction bytes.
    /// </summary>
    /// <param name="maxInstructionBytes">Maximum number of instruction bytes to read (default 16).</param>
    /// <returns>
    ///     An <see cref="InvalidInstructionContext" /> containing PC, instruction bytes, and architecture info.
    /// </returns>
    /// <remarks>
    ///     This method is useful when handling InvalidInstruction hooks.
    ///     It will attempt to read the PC register and memory at PC to provide detailed diagnostic information.
    ///     If reading fails, the error will be captured in the context object.
    /// </remarks>
    public InvalidInstructionContext GetInvalidInstructionContext(int maxInstructionBytes = 16)
    {
        EnsureNotDisposed();

        ulong pc = 0;
        byte[]? instructionBytes = null;
        ErrorCode? readError = null;

        // Try to read the program counter based on architecture
        try
        {
            pc = EngineArchitecture switch
            {
                Architecture.X86 => RegRead<ulong>(GetPcRegisterX86()),
                Architecture.Arm => RegRead<uint>(GetPcRegisterArm()),
                Architecture.Arm64 => RegRead<ulong>(GetPcRegisterArm64()),
                Architecture.Mips => RegRead<ulong>(GetPcRegisterMips()),
                Architecture.Sparc => RegRead<ulong>(GetPcRegisterSparc()),
                Architecture.M68K => RegRead<uint>(GetPcRegisterM68K()),
                Architecture.PowerPc => RegRead<ulong>(GetPcRegisterPpc()),
                Architecture.RiscV => RegRead<ulong>(GetPcRegisterRiscv()),
                Architecture.S390X => RegRead<ulong>(GetPcRegisterS390X()),
                Architecture.TriCore => RegRead<uint>(GetPcRegisterTriCore()),
                _ => 0
            };
        }
        catch
        {
            // If we can't read PC, just use 0
            pc = 0;
        }

        // Try to read instruction bytes at PC
        if (pc != 0)
        {
            try
            {
                instructionBytes = new byte[maxInstructionBytes];
                MemRead(pc, instructionBytes);
            }
            catch (UnicornMemoryException ex)
            {
                readError = ex.ErrorCode;
                instructionBytes = null;
            }
            catch
            {
                readError = ErrorCode.ReadUnmapped;
                instructionBytes = null;
            }
        }

        return new InvalidInstructionContext(pc, instructionBytes, readError, EngineArchitecture, EngineMode);
    }

    private int GetPcRegisterX86()
    {
        // Check if 64-bit mode
        return (EngineMode & Mode.Mode64) != 0
            ? (int)Registers.X86.RIP
            : (int)Registers.X86.EIP;
    }

    private static int GetPcRegisterArm()
    {
        return (int)Registers.Arm.PC;
    }
    private static int GetPcRegisterArm64()
    {
        return (int)Registers.Arm64.PC;
    }
    private static int GetPcRegisterMips()
    {
        return (int)Registers.Mips.PC;
    }
    private static int GetPcRegisterSparc()
    {
        return (int)Registers.Sparc.PC;
    }
    private static int GetPcRegisterM68K()
    {
        return (int)Registers.M68k.PC;
    }
    private static int GetPcRegisterPpc()
    {
        return (int)Registers.Ppc.PC;
    }
    private static int GetPcRegisterRiscv()
    {
        return (int)Registers.Riscv.PC;
    }
    private static int GetPcRegisterS390X()
    {
        return (int)Registers.S390x.PC;
    }
    private static int GetPcRegisterTriCore()
    {
        return (int)Registers.TriCore.PC;
    }
}