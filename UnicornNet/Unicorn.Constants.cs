using System.Globalization;

namespace UnicornNet;

public partial class Unicorn
{
    public delegate void BlockHook(Unicorn engine, ulong address, int size, object? state);

    public delegate void CodeHook(Unicorn engine, ulong address, int size, object? state);

    public delegate uint InHook(Unicorn engine, uint port, int size, object? state);

    public delegate void InterruptHook(Unicorn engine, uint interruptNumber, object? state);

    public delegate bool MemoryEventHook(Unicorn engine, MemoryAccessType accessType, ulong address, int size, long value, object? state);

    public delegate void MemoryHook(Unicorn engine, MemoryAccessType accessType, ulong address, int size, long value, object? state);

    public delegate void OutHook(Unicorn engine, uint port, int size, uint value, object? state);

    public delegate void SyscallHook(Unicorn engine, object? state);

    public enum Architecture : uint
    {
        Arm = 1,
        Arm64 = 2,
        Mips = 3,
        X86 = 4,
        PowerPc = 5,
        Sparc = 6,
        M68K = 7,
        RiscV = 8,
        S390X = 9,
        TriCore = 10
    }

    [Flags]
    public enum ControlIo : uint
    {
        None = 0,
        Write = 1,
        Read = 2,
        ReadWrite = Read | Write
    }

    public enum ControlType : uint
    {
        EngineMode = 0,
        PageSize = 1,
        Architecture = 2,
        Timeout = 3,
        UseExits = 4,
        ExitCount = 5,
        Exits = 6,
        CpuModel = 7,
        TranslationBlockRequest = 8,
        TranslationBlockRemove = 9,
        TranslationBlockFlush = 10,
        TlbFlush = 11,
        TlbType = 12,
        TcgBufferSize = 13,
        ContextMode = 14
    }

    public enum ErrorCode
    {
        Ok = 0,
        NoMem = 1,
        Arch = 2,
        Handle = 3,
        Mode = 4,
        Version = 5,
        ReadUnmapped = 6,
        WriteUnmapped = 7,
        FetchUnmapped = 8,
        Hook = 9,
        InvalidInstruction = 10,
        Map = 11,
        WriteProtected = 12,
        ReadProtected = 13,
        FetchProtected = 14,
        Argument = 15,
        ReadUnaligned = 16,
        WriteUnaligned = 17,
        FetchUnaligned = 18,
        HookExists = 19,
        Resource = 20,
        Exception = 21,
        Overflow = 22
    }

    [Flags]
    public enum HookType : uint
    {
        Interrupt = 1,
        Instruction = 2,
        Code = 4,
        Block = 8,
        MemReadUnmapped = 16,
        MemWriteUnmapped = 32,
        MemFetchUnmapped = 64,
        MemReadProt = 128,
        MemWriteProt = 256,
        MemFetchProt = 512,
        MemRead = 1024,
        MemWrite = 2048,
        MemFetch = 4096,
        MemReadAfter = 8192,
        InvalidInstruction = 16384,
        EdgeGenerated = 32768,
        TcgOpcode = 65536,
        TlbFill = 131072,
        MemUnmapped = MemReadUnmapped | MemWriteUnmapped | MemFetchUnmapped,
        MemProt = MemReadProt | MemWriteProt | MemFetchProt,
        MemReadInvalid = MemReadUnmapped | MemReadProt,
        MemWriteInvalid = MemWriteUnmapped | MemWriteProt,
        MemFetchInvalid = MemFetchUnmapped | MemFetchProt,
        MemInvalid = MemUnmapped | MemProt,
        MemValid = MemRead | MemWrite | MemFetch
    }

    public enum MemoryAccessType : uint
    {
        Read = 16,
        Write = 17,
        Fetch = 18,
        ReadUnmapped = 19,
        WriteUnmapped = 20,
        FetchUnmapped = 21,
        WriteProtected = 22,
        ReadProtected = 23,
        FetchProtected = 24,
        ReadAfter = 25
    }

    [Flags]
    public enum MemoryPermissions : uint
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        All = Read | Write | Execute
    }

    [Flags]
    public enum Mode : uint
    {
        LittleEndian = 0,
        BigEndian = 1u << 30,
        Arm = 0,
        Thumb = 1u << 4,
        MClass = 1u << 5,
        V8 = 1u << 6,
        ArmBe8 = 1u << 10,
        Arm926 = 1u << 7,
        Arm946 = 1u << 8,
        Arm1176 = 1u << 9,
        Micro = Thumb,
        Mips3 = 1u << 5,
        Mips32R6 = 1u << 6,
        Mips32 = 1u << 2,
        Mips64 = 1u << 3,
        Mode16 = 1u << 1,
        Mode32 = 1u << 2,
        Mode64 = 1u << 3,
        Ppc32 = 1u << 2,
        Ppc64 = 1u << 3,
        Qpx = 1u << 4,
        Sparc32 = 1u << 2,
        Sparc64 = 1u << 3,
        V9 = 1u << 4,
        RiscV32 = 1u << 2,
        RiscV64 = 1u << 3
    }

    private const ulong DefaultHookBegin = 1;
    private const ulong DefaultHookEnd = 0;

    private const int X86InstructionIn = 218;
    private const int X86InstructionOut = 500;
    private const int X86InstructionSyscall = 699;

    public readonly record struct ControlCommand(uint Value)
    {
        private const int ArgumentCountShift = 26;
        private const int IoShift = 30;
        private const uint TypeMask = (1u << ArgumentCountShift) - 1;
        private const uint ArgumentMask = 0xFu;
        private const uint IoMask = 0x3u;

        public ControlType Type
        {
            get => (ControlType)(Value & TypeMask);
        }

        public int ArgumentCount
        {
            get => (int)(Value >> ArgumentCountShift & ArgumentMask);
        }

        public ControlIo Access
        {
            get => (ControlIo)(Value >> IoShift & IoMask);
        }

        public static ControlCommand Create(ControlType type, int argumentCount, ControlIo access)
        {
            if ((uint)argumentCount > ArgumentMask)
            {
                throw new ArgumentOutOfRangeException(nameof(argumentCount), "Argument count must be between 0 and 15.");
            }

            var value = (uint)type;
            value |= ((uint)argumentCount & ArgumentMask) << ArgumentCountShift;
            value |= ((uint)access & IoMask) << IoShift;
            return new ControlCommand(value);
        }

        public static ControlCommand None(ControlType type, int argumentCount)
        {
            return Create(type, argumentCount, ControlIo.None);
        }

        public static ControlCommand Read(ControlType type, int argumentCount)
        {
            return Create(type, argumentCount, ControlIo.Read);
        }

        public static ControlCommand Write(ControlType type, int argumentCount)
        {
            return Create(type, argumentCount, ControlIo.Write);
        }

        public static ControlCommand ReadWrite(ControlType type, int argumentCount)
        {
            return Create(type, argumentCount, ControlIo.ReadWrite);
        }

        public static implicit operator ControlCommand(uint value)
        {
            return new ControlCommand(value);
        }
    }

    public readonly record struct HookRange(ulong Begin, ulong End)
    {
        public static HookRange All
        {
            get => new(DefaultHookBegin, DefaultHookEnd);
        }
    }

    public readonly record struct HookHandle(nuint Value)
    {
        public bool IsEmpty
        {
            get => Value == 0;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}