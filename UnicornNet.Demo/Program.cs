using System;
using System.Reflection;
using UnicornNet;

const ulong mappingAddress = 0x1000;
const ulong mappingSize = 0x1000;

Console.WriteLine("=== Unicorn Demo ===");

const Unicorn.Architecture architecture = Unicorn.Architecture.X86;
const Unicorn.Mode mode = Unicorn.Mode.Mode32 | Unicorn.Mode.LittleEndian;

using var unicorn = new Unicorn(architecture, mode);
Console.WriteLine($"[engine] Created {architecture} engine in {mode} mode.");

Console.WriteLine($"[mem] Mapping 0x{mappingSize:X} bytes @ 0x{mappingAddress:X} with RW permissions.");
unicorn.MemMap(mappingAddress, mappingSize, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);

var payload = new byte[]
{
    0x90,
    0x90,
    0xC3
}; // NOP, NOP, RET
Console.WriteLine($"[mem] Writing payload: {FormatBytes(payload)}");
unicorn.MemWrite(mappingAddress, payload);

var buffer = new byte[payload.Length];
unicorn.MemRead(mappingAddress, buffer);
Console.WriteLine($"[mem] Read back payload: {FormatBytes(buffer)}");

Console.WriteLine("[hook] Registering code hook for demo output...");
var hookHandle = unicorn.AddCodeHook((_, address, size, state) =>
{
    Console.WriteLine($"[hook:{state}] executed block at 0x{address:X} (size {size} bytes).");
}, state: "code");

if (!TriggerHookForDemo(unicorn, hookHandle, mappingAddress, payload.Length))
{
    Console.WriteLine("[hook] Unable to trigger hook in demo mode. Hooks fire automatically when uc_emu_start executes real code.");
}

unicorn.HookDel(hookHandle);
Console.WriteLine("[hook] Demo hook removed.");

Console.WriteLine("=== Demo complete ===");
return;

static string FormatBytes(ReadOnlySpan<byte> bytes)
{
    return BitConverter.ToString(bytes.ToArray());
}

static bool TriggerHookForDemo(Unicorn unicorn, Unicorn.HookHandle handle, ulong address, int size)
{
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
    var method = typeof(Unicorn).GetMethod("TrySimulateHook", flags);

    var result = method?.Invoke(unicorn, [handle, address, size]);
    return result is bool and true;
}