using System;
using Xunit;

namespace UnicornNet.Tests;

public sealed class ControlEngineTests
{
    [Fact]
    public void ControlCommand_CarriesArgumentsWithPackedCommandValue()
    {
        ReadOnlySpan<nint> arguments = [(nint)0x1000, (nint)0x2000];

        var command = Unicorn.ControlCommand.Create(
            Unicorn.ControlType.TranslationBlockRemove,
            arguments,
            Unicorn.ControlIo.Write);

        Assert.Equal(Unicorn.ControlType.TranslationBlockRemove, command.Type);
        Assert.Equal(Unicorn.ControlIo.Write, command.Access);
        Assert.Equal(2, command.ArgumentCount);
        Assert.Equal(arguments.ToArray(), command.Arguments.ToArray());
    }

    [Fact]
    public void ControlEngine_ForwardsCommandAndArgumentsToNativeProxy()
    {
        var native = new FakeNativeProxy();
        var engine = new ControlEngine(native, () => new IntPtr(0x1234), () => { });
        var command = Unicorn.ControlCommand.Read(Unicorn.ControlType.PageSize, [(nint)0x4000]);

        engine.Control(command);

        Assert.True(native.LastControl.HasValue);
        Assert.Equal(command.Value, native.LastControl.Value.Control);
        Assert.Equal(command.Arguments.ToArray(), native.LastControl.Value.Arguments);
    }
}
