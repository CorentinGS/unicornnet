using System.Runtime.InteropServices;

namespace UnicornNet;

public partial class Unicorn
{
    private enum HookCategory
    {
        Code,
        Block,
        Memory,
        EventMemory,
        Interrupt,
        In,
        Out,
        Syscall
    }

    private sealed class HookRegistration : IDisposable
    {
        private readonly Delegate _callback;
        private readonly Unicorn _owner;
        private readonly object? _state;
        private readonly HookType _type;
        private bool _disposed;
        private GCHandle _gcHandle;

        public HookRegistration(Unicorn owner, HookType type, HookCategory category, Delegate callback, object? state, MemoryAccessType? accessType = null)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _owner = owner;
            _type = type;
            Category = category;
            _callback = callback;
            _state = state;
            AccessType = accessType;
            _gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        public HookHandle Handle { get; private set; }

        public HookCategory Category { get; }

        public MemoryAccessType? AccessType { get; }

        public IntPtr UserDataPointer
        {
            get => GCHandle.ToIntPtr(_gcHandle);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            _disposed = true;
        }

        public void SetHandle(HookHandle handle)
        {
            Handle = handle;
        }

        public void InvokeCode(ulong address, int size)
        {
            if (_disposed)
            {
                return;
            }

            switch (Category)
            {
                case HookCategory.Code when _callback is CodeHook codeHook:
                    codeHook(_owner, address, size, _state);
                    break;
                case HookCategory.Block when _callback is BlockHook blockHook:
                    blockHook(_owner, address, size, _state);
                    break;
                case HookCategory.Memory:
                case HookCategory.EventMemory:
                case HookCategory.Interrupt:
                case HookCategory.In:
                case HookCategory.Out:
                case HookCategory.Syscall:
                default:
                    throw new InvalidOperationException($"Unsupported hook category {Category} for code invocation.");
            }
        }

        public void InvokeMemory(MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (_disposed || _callback is not MemoryHook memHook)
            {
                return;
            }

            memHook(_owner, accessType, address, size, value, _state);
        }

        public bool InvokeEventMemory(MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (_disposed || _callback is not MemoryEventHook eventHook)
            {
                return true;
            }

            return eventHook(_owner, accessType, address, size, value, _state);
        }

        public void InvokeInterrupt(uint interruptNumber)
        {
            if (_disposed || _callback is not InterruptHook interruptHook)
            {
                return;
            }

            interruptHook(_owner, interruptNumber, _state);
        }

        public uint InvokeIn(uint port, int size)
        {
            if (_disposed || _callback is not InHook inHook)
            {
                return 0;
            }

            return inHook(_owner, port, size, _state);
        }

        public void InvokeOut(uint port, int size, uint value)
        {
            if (_disposed || _callback is not OutHook outHook)
            {
                return;
            }

            outHook(_owner, port, size, value, _state);
        }

        public void InvokeSyscall()
        {
            if (_disposed || _callback is not SyscallHook syscallHook)
            {
                return;
            }

            syscallHook(_owner, _state);
        }
    }
}