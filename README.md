# UnicornNet

A modern, type-safe .NET binding for the [Unicorn CPU Emulator Engine](https://www.unicorn-engine.org/). UnicornNet enables .NET developers to leverage the powerful multi-architecture CPU emulation capabilities of Unicorn from C# and other .NET languages.

## Features

- **Multi-Architecture Support**: ARM, ARM64 (AArch64), MIPS, x86/x64, PowerPC, SPARC, M68K, RISC-V, S390X, and TriCore
- **Memory Management**: Map, protect, read, and write emulated memory regions
- **Register Access**: Type-safe register manipulation for all supported architectures
- **Hook System**: Intercept code execution, memory access, interrupts, syscalls, and more
- **Fluent API**: Chain hook registrations with the `HookBuilder` pattern
- **Modern .NET**: Built for .NET 10 with nullable reference types, span-based APIs, and source generators
- **Comprehensive Error Handling**: Detailed exception hierarchy with contextual information

## Installation

### Via NuGet (Coming Soon)

```bash
dotnet add package UnicornNet
```

### Manual Build

1. Clone the repository:
   ```bash
   git clone https://github.com/corentings/UnicornNet.git
   cd UnicornNet
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Add a reference to your project:
   ```bash
   dotnet add reference path/to/UnicornNet/UnicornNet.csproj
   ```

### Native Library

UnicornNet requires the native Unicorn library (`unicorn.dll` on Windows, `libunicorn.so` on Linux, `libunicorn.dylib` on macOS). You can:

- Download pre-built binaries from the [Unicorn releases](https://github.com/unicorn-engine/unicorn/releases)
- Build from source following the [Unicorn documentation](https://github.com/unicorn-engine/unicorn)

Ensure the native library is in your application's search path or copy it to your output directory.

## Quick Start

### Basic Emulation

```csharp
using UnicornNet;

// Create an x86 32-bit emulator
using var unicorn = new Unicorn(
    Unicorn.Architecture.X86,
    Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian
);

// Map a memory region
const ulong address = 0x1000;
unicorn.MemMap(address, 0x1000, Unicorn.MemoryPermissions.All);

// Write code (NOP, NOP, RET)
var code = new byte[] { 0x90, 0x90, 0xC3 };
unicorn.MemWrite(address, code);

// Execute
unicorn.EmuStart(address, address + (ulong)code.Length);
```

### Working with Registers

```csharp
using UnicornNet;

using var unicorn = new Unicorn(
    Unicorn.Architecture.X86,
    Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian
);

// Map memory
unicorn.MemMap(0x1000, 0x1000, Unicorn.MemoryPermissions.All);

// Set register values
unicorn.RegWrite(Unicorn.X86.Register.EAX, 0x12345678);
unicorn.RegWrite(Unicorn.X86.Register.EBX, 0xDEADBEEF);

// Read register values
uint eax = unicorn.RegRead<uint>(Unicorn.X86.Register.EAX);
```

### Using Hooks

```csharp
using UnicornNet;

using var unicorn = new Unicorn(
    Unicorn.Architecture.X86,
    Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian
);

unicorn.MemMap(0x1000, 0x1000, Unicorn.MemoryPermissions.All);

// Add a code hook
var hook = unicorn.AddCodeHook((engine, address, size, state) =>
{
    Console.WriteLine($"Executing at 0x{address:X}, size: {size}");
});

// Add a memory hook
var memHook = unicorn.AddMemWriteHook((engine, access, address, size, value, state) =>
{
    Console.WriteLine($"Memory write at 0x{address:X}, value: 0x{value:X}");
});

var code = new byte[] { 0x90, 0x90, 0xC3 };
unicorn.MemWrite(0x1000, code);
unicorn.EmuStart(0x1000, 0x1003);

// Clean up hooks
unicorn.HookDel(hook);
unicorn.HookDel(memHook);
```

### Fluent Hook Builder

```csharp
using UnicornNet;

using var unicorn = new Unicorn(
    Unicorn.Architecture.X86,
    Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian
);

unicorn.MemMap(0x1000, 0x1000, Unicorn.MemoryPermissions.All);

// Register multiple hooks fluently
var handles = unicorn.Hooks()
    .OnCode((engine, address, size, _) => 
        Console.WriteLine($"Code: 0x{address:X}"))
    .OnMemoryRead((engine, access, address, size, value, _) => 
        Console.WriteLine($"Read: 0x{address:X}"))
    .OnMemoryWrite((engine, access, address, size, value, _) => 
        Console.WriteLine($"Write: 0x{address:X}"))
    .OnInterrupt((engine, intno, _) => 
        Console.WriteLine($"Interrupt: {intno}"))
    .GetHandles();

var code = new byte[] { 0x90, 0x90, 0xC3 };
unicorn.MemWrite(0x1000, code);
unicorn.EmuStart(0x1000, 0x1003);
```

### Extension Methods

```csharp
using UnicornNet;

using var unicorn = new Unicorn(
    Unicorn.Architecture.X86,
    Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian
);

// Convenient mapping helpers
unicorn.MapStack(0x7FFF0000, 0x10000);
unicorn.MapHeap(0x00400000, 0x10000);
unicorn.MapCode(0x00410000, new byte[] { 0x90, 0x90, 0xC3 });
unicorn.MapReadOnlyData(0x00420000, Encoding.ASCII.GetBytes("Hello, World!"));

// Easy byte operations
unicorn.WriteBytes(0x00410000, 0xB8, 0x01, 0x00, 0x00, 0x00); // MOV EAX, 1
var bytes = unicorn.ReadBytes(0x00410000, 5);
```

### Exception Handling

```csharp
using UnicornNet;

try
{
    using var unicorn = new Unicorn(
        Unicorn.Architecture.X86,
        Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian
);
    
    unicorn.MemMap(0x1000, 0x1000, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Execute);
    
    // This will throw - memory is not writable
    unicorn.MemWrite(0x1000, new byte[] { 0x90 });
}
catch (UnicornMemoryException ex)
{
    Console.WriteLine($"Memory error at 0x{ex.Address:X}, size: {ex.Size}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
}
catch (UnicornEngineException ex)
{
    Console.WriteLine($"Engine error: {ex.Message}");
    Console.WriteLine($"Operation: {ex.Operation}");
}
```

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `Unicorn` | Main emulator class implementing `IDisposable` |
| `Architecture` | CPU architecture enum (Arm, Arm64, X86, Mips, etc.) |
| `Mode` | CPU mode flags (LittleEndian, BigEndian, Mode32, Mode64, etc.) |
| `MemoryPermissions` | Memory protection flags (Read, Write, Execute) |
| `HookType` | Hook type flags for various interception points |

### Memory Operations

| Method | Description |
|--------|-------------|
| `MemMap(address, size, permissions)` | Map a memory region |
| `MemUnmap(address, size)` | Unmap a memory region |
| `MemProtect(address, size, permissions)` | Change memory protections |
| `MemWrite(address, data)` | Write bytes to memory |
| `MemRead(address, buffer)` | Read bytes from memory |
| `MapRegion(address, size, permissions)` | Create an RAII memory region wrapper |

### Register Operations

| Method | Description |
|--------|-------------|
| `RegWrite(register, value)` | Write to a register |
| `RegRead<T>(register)` | Read a register value |
| `RegRead(register, buffer)` | Read register bytes to a buffer |

### Hook Types

| Hook Type | Delegate | Description |
|-----------|----------|-------------|
| Code | `CodeHook` | Called on every instruction |
| Block | `BlockHook` | Called on every basic block |
| Memory Read | `MemoryHook` | Called on memory reads |
| Memory Write | `MemoryHook` | Called on memory writes |
| Interrupt | `InterruptHook` | Called on interrupts |
| Syscall | `SyscallHook` | Called on syscalls/sysenter |
| IN Instruction | `InHook` | Called on IN instructions |
| OUT Instruction | `OutHook` | Called on OUT instructions |
| Invalid Instruction | `InvalidInstructionHook` | Called on invalid instructions |

## Supported Architectures

| Architecture | Modes | Register Enum |
|--------------|-------|---------------|
| ARM | ARM, Thumb, Little/Big Endian | `Unicorn.Arm.Register` |
| ARM64 | Little/Big Endian | `Unicorn.Arm64.Register` |
| x86 | 16/32/64-bit | `Unicorn.X86.Register` |
| MIPS | 32/64-bit, Little/Big Endian | `Unicorn.Mips.Register` |
| PowerPC | 32/64-bit, Little/Big Endian | `Unicorn.Ppc.Register` |
| SPARC | 32/64-bit | `Unicorn.Sparc.Register` |
| M68K | Various modes | `Unicorn.M68k.Register` |
| RISC-V | 32/64-bit | `Unicorn.RiscV.Register` |
| S390X | - | `Unicorn.S390x.Register` |
| TriCore | - | `Unicorn.TriCore.Register` |

## Requirements

- .NET 10.0 or later
- Native Unicorn library (2.0.0 or later recommended)

## Building

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Create a NuGet package
dotnet pack -c Release
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Unicorn Engine](https://www.unicorn-engine.org/) - The underlying CPU emulator
- [QEMU](https://www.qemu.org/) - The foundation upon which Unicorn is built

## Related Projects

- [Unicorn Engine](https://github.com/unicorn-engine/unicorn) - The official Unicorn repository
- [Capstone](https://www.capstone-engine.org/) - Multi-architecture disassembly framework
- [Keystone](https://www.keystone-engine.org/) - Multi-architecture assembler framework
