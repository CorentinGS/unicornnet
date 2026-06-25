using System;
using System.Linq;
using Xunit;

namespace UnicornNet.Tests;

public sealed class HookManagerTests
{
    [Fact]
    public void FakeHookManager_RegistersAndRemovesHooks()
    {
        var manager = new FakeHookManager();
        var invocationCount = 0;

        var handle = manager.AddHook(Unicorn.HookType.Code, new Unicorn.CodeHook((_, _, _, _) => invocationCount++));
        var invokedBeforeRemoval = manager.TrySimulateHook(handle, 0x1000, 4);

        manager.RemoveHook(handle);
        var invokedAfterRemoval = manager.TrySimulateHook(handle, 0x1000, 4);

        Assert.True(invokedBeforeRemoval);
        Assert.False(invokedAfterRemoval);
        Assert.Equal(1, invocationCount);
        Assert.Contains(handle, manager.RemovedHooks);
    }

    [Fact]
    public void PublicHookRegistrationMethods_AreNotGeneric()
    {
        var genericHookMethods = typeof(Unicorn).GetMethods()
            .Where(method => method.Name.StartsWith("Add", StringComparison.Ordinal)
                             && method.Name.EndsWith("Hook", StringComparison.Ordinal)
                             && method.IsGenericMethod)
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(genericHookMethods);
    }
}
