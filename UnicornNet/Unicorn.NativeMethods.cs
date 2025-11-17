using System.Runtime.InteropServices;

namespace UnicornNet;

public partial class Unicorn
{
    public static partial class NativeMethods
    {
        [LibraryImport("unicorn", EntryPoint = "uc_mem_map")]
        public static partial int UcMemMap(IntPtr engine, ulong address, ulong size, uint perms);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_map_ptr")]
        public static partial int UcMemMapPtr(IntPtr engine, ulong address, ulong size, uint perms, IntPtr pointer);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_unmap")]
        public static partial int UcMemUnmap(IntPtr engine, ulong address, ulong size);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_protect")]
        public static partial int UcMemProtect(IntPtr engine, ulong address, ulong size, uint perms);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_write")]
        public static partial int UcMemWrite(IntPtr engine, ulong address, ref byte bytes, nuint size);

        [LibraryImport("unicorn", EntryPoint = "uc_mem_read")]
        public static partial int UcMemRead(IntPtr engine, ulong address, ref byte buffer, nuint size);

        [LibraryImport("unicorn", EntryPoint = "uc_reg_write")]
        public static partial int UcRegWrite(IntPtr engine, int registerId, ref byte value);

        [LibraryImport("unicorn", EntryPoint = "uc_reg_read")]
        public static partial int UcRegRead(IntPtr engine, int registerId, ref byte buffer);

        [LibraryImport("unicorn", EntryPoint = "uc_emu_start")]
        public static partial int UcEmuStart(IntPtr engine, ulong begin, ulong until, ulong timeout, nuint count);

        [LibraryImport("unicorn", EntryPoint = "uc_emu_stop")]
        public static partial int UcEmuStop(IntPtr engine);

        [LibraryImport("unicorn", EntryPoint = "uc_open")]
        public static partial int UcOpen(int arch, int mode, out IntPtr engine);

        [LibraryImport("unicorn", EntryPoint = "uc_close")]
        public static partial int UcClose(IntPtr engine);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAdd(IntPtr engine, out nuint hook, uint hookType, NativeHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddMem(IntPtr engine, out nuint hook, uint hookType, NativeMemHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddEventMem(IntPtr engine, out nuint hook, uint hookType, NativeEventMemHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInterrupt(IntPtr engine, out nuint hook, uint hookType, NativeInterruptHookCallback callback, IntPtr userData, ulong begin, ulong end);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInstructionIn(IntPtr engine, out nuint hook, uint hookType, NativeInstructionInHookCallback callback, IntPtr userData, ulong begin, ulong end, int instruction);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInstructionOut(IntPtr engine, out nuint hook, uint hookType, NativeInstructionOutHookCallback callback, IntPtr userData, ulong begin, ulong end, int instruction);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_add")]
        public static partial int UcHookAddInstructionSyscall(IntPtr engine, out nuint hook, uint hookType, NativeSyscallHookCallback callback, IntPtr userData, ulong begin, ulong end, int instruction);

        [LibraryImport("unicorn", EntryPoint = "uc_hook_del")]
        public static partial int UcHookDel(IntPtr engine, nuint hook);

        [LibraryImport("unicorn", EntryPoint = "uc_ctl")]
        public static partial int UcCtl0(IntPtr engine, uint control);

        [LibraryImport("unicorn", EntryPoint = "uc_ctl")]
        public static partial int UcCtl1(IntPtr engine, uint control, nint arg0);

        [LibraryImport("unicorn", EntryPoint = "uc_ctl")]
        public static partial int UcCtl2(IntPtr engine, uint control, nint arg0, nint arg1);

        [LibraryImport("unicorn", EntryPoint = "uc_ctl")]
        public static partial int UcCtl3(IntPtr engine, uint control, nint arg0, nint arg1, nint arg2);

        [LibraryImport("unicorn", EntryPoint = "uc_ctl")]
        public static partial int UcCtl4(IntPtr engine, uint control, nint arg0, nint arg1, nint arg2, nint arg3);

        [LibraryImport("unicorn", EntryPoint = "uc_errno")]
        public static partial int UcErrno(IntPtr engine);
    }
}
