using System;

namespace UnicornNet;

public partial class Unicorn
{
    /// <summary>
    ///     Provides detailed context about an invalid instruction error.
    ///     This class captures the engine state at the time of the invalid instruction,
    ///     including the program counter and instruction bytes.
    /// </summary>
    public sealed class InvalidInstructionContext
    {
        internal InvalidInstructionContext(
            ulong programCounter,
            byte[]? instructionBytes,
            ErrorCode? readError,
            Architecture architecture,
            Mode mode)
        {
            ProgramCounter = programCounter;
            InstructionBytes = instructionBytes;
            ReadError = readError;
            EngineArchitecture = architecture;
            EngineMode = mode;
        }

        /// <summary>
        ///     Gets the program counter (PC) at the time of the invalid instruction.
        /// </summary>
        public ulong ProgramCounter { get; }

        /// <summary>
        ///     Gets the instruction bytes at the PC location, if available.
        ///     May be null if memory could not be read.
        /// </summary>
        public byte[]? InstructionBytes { get; }

        /// <summary>
        ///     Gets the error that occurred while trying to read instruction bytes, if any.
        /// </summary>
        public ErrorCode? ReadError { get; }

        /// <summary>
        ///     Gets the architecture of the engine.
        /// </summary>
        public Architecture EngineArchitecture { get; }

        /// <summary>
        ///     Gets the mode of the engine.
        /// </summary>
        public Mode EngineMode { get; }

        /// <summary>
        ///     Gets a formatted string representation of the instruction bytes in hexadecimal.
        /// </summary>
        /// <returns>Hex string like "48 8B 05" or "[unable to read]" if bytes are not available.</returns>
        public string GetInstructionBytesHex()
        {
            if (InstructionBytes == null || InstructionBytes.Length == 0)
            {
                return ReadError.HasValue
                    ? $"[unable to read: {ReadError.Value}]"
                    : "[no bytes]";
            }

            return BitConverter.ToString(InstructionBytes).Replace("-", " ");
        }

        /// <summary>
        ///     Gets a detailed error message describing the invalid instruction.
        /// </summary>
        public string GetDetailedMessage()
        {
            return $"Invalid instruction at PC=0x{ProgramCounter:X} ({EngineArchitecture}/{EngineMode}): {GetInstructionBytesHex()}";
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetDetailedMessage();
        }
    }
}