using System;
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
        Syscall,
        InvalidInstruction
    }

    /// <summary>
    ///     Manages the lifecycle of a hook registration.
    ///     WARNING: This class is NOT thread-safe. Hook callbacks and disposal must not be called concurrently.
    ///     If you need to use hooks from multiple threads, ensure proper synchronization externally.
    /// </summary>
    private sealed class HookRegistration : IDisposable
    {
        private readonly Delegate _callback;
        private readonly Unicorn _owner;
        private readonly object? _state;
        private bool _disposed;
        private GCHandle _gcHandle;

        public HookRegistration(Unicorn owner, HookType type, HookCategory category, Delegate callback, object? state, MemoryAccessType? accessType = null)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _owner = owner;
            Type = type;
            Category = category;
            _callback = callback;
            _state = state;
            AccessType = accessType;
            _gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        public HookHandle Handle { get; private set; }

        public HookCategory Category { get; }

        public HookType Type { get; }

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

        private void HandleCallbackException(Exception exception, string hookType)
        {
            var logger = _owner.Logger;
            var handling = _owner.Options.CallbackExceptionHandling;

            // If no logger configured but user wants logging, fall back to throwing
            if (handling != CallbackExceptionHandling.Throw && logger == null)
            {
                handling = CallbackExceptionHandling.Throw;
            }

            switch (handling)
            {
                case CallbackExceptionHandling.Throw:
                    throw exception;

                case CallbackExceptionHandling.LogAndThrow:
                    logger!.LogError($"Exception in {hookType} hook callback", exception);
                    throw exception;

                case CallbackExceptionHandling.LogAndContinue:
                    logger!.LogError($"Exception in {hookType} hook callback (continuing execution)", exception);
                    break;
            }
        }

        private void LogDisposedHook(string hookType)
        {
            if (_owner.Options.EnableVerboseDiagnostics)
            {
                _owner.Logger?.LogWarning($"{hookType} hook invoked but registration is disposed");
            }
        }

        private void LogInvalidCallback(string hookType, string expectedType)
        {
            if (_owner.Options.EnableVerboseDiagnostics)
            {
                _owner.Logger?.LogWarning($"{hookType} hook invoked but callback is not of expected type {expectedType}");
            }
        }

        public void InvokeCode(ulong address, int size)
        {
            if (_disposed)
            {
                LogDisposedHook("Code/Block");
                return;
            }

            try
            {
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
            catch (Exception ex)
            {
                HandleCallbackException(ex, Category == HookCategory.Code ? "Code" : "Block");
            }
        }

        public void InvokeMemory(MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (_disposed)
            {
                LogDisposedHook("Memory");
                return;
            }

            if (_callback is not MemoryHook memHook)
            {
                LogInvalidCallback("Memory", nameof(MemoryHook));
                return;
            }

            try
            {
                memHook(_owner, accessType, address, size, value, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Memory");
            }
        }

        public bool InvokeEventMemory(MemoryAccessType accessType, ulong address, int size, long value)
        {
            if (_disposed)
            {
                LogDisposedHook("EventMemory");
                return true;
            }

            if (_callback is not MemoryEventHook eventHook)
            {
                LogInvalidCallback("EventMemory", nameof(MemoryEventHook));
                return true;
            }

            try
            {
                return eventHook(_owner, accessType, address, size, value, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "EventMemory");
                return true; // Default return if LogAndContinue
            }
        }

        public void InvokeInterrupt(uint interruptNumber)
        {
            if (_disposed)
            {
                LogDisposedHook("Interrupt");
                return;
            }

            if (_callback is not InterruptHook interruptHook)
            {
                LogInvalidCallback("Interrupt", nameof(InterruptHook));
                return;
            }

            try
            {
                interruptHook(_owner, interruptNumber, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Interrupt");
            }
        }

        public uint InvokeIn(uint port, int size)
        {
            if (_disposed)
            {
                LogDisposedHook("In");
                return 0;
            }

            if (_callback is not InHook inHook)
            {
                LogInvalidCallback("In", nameof(InHook));
                return 0;
            }

            try
            {
                return inHook(_owner, port, size, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "In");
                return 0; // Default return if LogAndContinue
            }
        }

        public void InvokeOut(uint port, int size, uint value)
        {
            if (_disposed)
            {
                LogDisposedHook("Out");
                return;
            }

            if (_callback is not OutHook outHook)
            {
                LogInvalidCallback("Out", nameof(OutHook));
                return;
            }

            try
            {
                outHook(_owner, port, size, value, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Out");
            }
        }

        public void InvokeSyscall()
        {
            if (_disposed)
            {
                LogDisposedHook("Syscall");
                return;
            }

            if (_callback is not SyscallHook syscallHook)
            {
                LogInvalidCallback("Syscall", nameof(SyscallHook));
                return;
            }

            try
            {
                syscallHook(_owner, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "Syscall");
            }
        }

        public bool InvokeInvalidInstruction()
        {
            if (_disposed)
            {
                LogDisposedHook("InvalidInstruction");
                return false;
            }

            if (_callback is not InvalidInstructionHook invalidInsnHook)
            {
                LogInvalidCallback("InvalidInstruction", nameof(InvalidInstructionHook));
                return false;
            }

            try
            {
                return invalidInsnHook(_owner, _state);
            }
            catch (Exception ex)
            {
                HandleCallbackException(ex, "InvalidInstruction");
                return false; // Default return if LogAndContinue
            }
        }
    }
}