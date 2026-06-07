using System;

namespace UnicornNet;

public interface IHookManager : IDisposable
{
    Unicorn.HookHandle AddHook(Unicorn.HookType type, Delegate callback, Unicorn.HookRange? range = null, object? state = null);

    void RemoveHook(Unicorn.HookHandle handle);
}
