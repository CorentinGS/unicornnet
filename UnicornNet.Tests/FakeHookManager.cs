using System;
using System.Collections.Generic;

namespace UnicornNet.Tests;

internal sealed class FakeHookManager : IHookManager
{
    private readonly Dictionary<Unicorn.HookHandle, (Unicorn.HookType Type, Delegate Callback, object? State)> _hooks = new();
    private nuint _nextHandle;

    public List<Unicorn.HookHandle> RemovedHooks { get; } = [];

    public Unicorn.HookHandle AddHook(Unicorn.HookType type, Delegate callback, Unicorn.HookRange? range = null, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var handle = new Unicorn.HookHandle(++_nextHandle);
        _hooks.Add(handle, (type, callback, state));
        return handle;
    }

    public void RemoveHook(Unicorn.HookHandle handle)
    {
        if (_hooks.Remove(handle))
        {
            RemovedHooks.Add(handle);
        }
    }

    public void Dispose()
    {
        _hooks.Clear();
    }

    public bool TrySimulateHook(Unicorn.HookHandle handle, ulong address, int size)
    {
        if (!_hooks.TryGetValue(handle, out var registration))
        {
            return false;
        }

        switch (registration.Type)
        {
            case Unicorn.HookType.Code when registration.Callback is Unicorn.CodeHook codeHook:
                codeHook(null!, address, size, registration.State);
                return true;
            case Unicorn.HookType.Block when registration.Callback is Unicorn.BlockHook blockHook:
                blockHook(null!, address, size, registration.State);
                return true;
            default:
                return false;
        }
    }
}
