using System;

namespace UnicornNet;

public partial class Unicorn
{
    public void Control(ControlCommand command)
    {
        _control.Control(command);
    }

    internal void Control(ControlCommand command, nint arg1)
    {
        Control(command.WithArguments([arg1]));
    }

    internal void Control(ControlCommand command, nint arg1, nint arg2)
    {
        Control(command.WithArguments([arg1, arg2]));
    }

    internal void Control(ControlCommand command, nint arg1, nint arg2, nint arg3)
    {
        Control(command.WithArguments([arg1, arg2, arg3]));
    }

    internal void Control(ControlCommand command, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(command.WithArguments([arg1, arg2, arg3, arg4]));
    }

    internal void Control(ControlCommand command, ReadOnlySpan<nint> arguments)
    {
        Control(command.WithArguments(arguments));
    }

    internal void Control(ControlType type, ControlIo access)
    {
        Control(ControlCommand.Create(type, 0, access));
    }

    internal void Control(ControlType type, ControlIo access, nint arg1)
    {
        Control(ControlCommand.Create(type, [arg1], access));
    }

    internal void Control(ControlType type, ControlIo access, nint arg1, nint arg2)
    {
        Control(ControlCommand.Create(type, [arg1, arg2], access));
    }

    internal void Control(ControlType type, ControlIo access, nint arg1, nint arg2, nint arg3)
    {
        Control(ControlCommand.Create(type, [arg1, arg2, arg3], access));
    }

    internal void Control(ControlType type, ControlIo access, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(ControlCommand.Create(type, [arg1, arg2, arg3, arg4], access));
    }

    internal void Control(ControlType type, ControlIo access, ReadOnlySpan<nint> arguments)
    {
        Control(ControlCommand.Create(type, arguments, access));
    }

    internal void ControlRead(ControlType type)
    {
        Control(type, ControlIo.Read);
    }

    internal void ControlRead(ControlType type, nint arg1)
    {
        Control(type, ControlIo.Read, arg1);
    }

    internal void ControlRead(ControlType type, nint arg1, nint arg2)
    {
        Control(type, ControlIo.Read, arg1, arg2);
    }

    internal void ControlRead(ControlType type, nint arg1, nint arg2, nint arg3)
    {
        Control(type, ControlIo.Read, arg1, arg2, arg3);
    }

    internal void ControlRead(ControlType type, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(type, ControlIo.Read, arg1, arg2, arg3, arg4);
    }

    internal void ControlRead(ControlType type, ReadOnlySpan<nint> arguments)
    {
        Control(type, ControlIo.Read, arguments);
    }

    internal void ControlWrite(ControlType type)
    {
        Control(type, ControlIo.Write);
    }

    internal void ControlWrite(ControlType type, nint arg1)
    {
        Control(type, ControlIo.Write, arg1);
    }

    internal void ControlWrite(ControlType type, nint arg1, nint arg2)
    {
        Control(type, ControlIo.Write, arg1, arg2);
    }

    internal void ControlWrite(ControlType type, nint arg1, nint arg2, nint arg3)
    {
        Control(type, ControlIo.Write, arg1, arg2, arg3);
    }

    internal void ControlWrite(ControlType type, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(type, ControlIo.Write, arg1, arg2, arg3, arg4);
    }

    internal void ControlWrite(ControlType type, ReadOnlySpan<nint> arguments)
    {
        Control(type, ControlIo.Write, arguments);
    }

    internal void ControlReadWrite(ControlType type)
    {
        Control(type, ControlIo.ReadWrite);
    }

    internal void ControlReadWrite(ControlType type, nint arg1)
    {
        Control(type, ControlIo.ReadWrite, arg1);
    }

    internal void ControlReadWrite(ControlType type, nint arg1, nint arg2)
    {
        Control(type, ControlIo.ReadWrite, arg1, arg2);
    }

    internal void ControlReadWrite(ControlType type, nint arg1, nint arg2, nint arg3)
    {
        Control(type, ControlIo.ReadWrite, arg1, arg2, arg3);
    }

    internal void ControlReadWrite(ControlType type, nint arg1, nint arg2, nint arg3, nint arg4)
    {
        Control(type, ControlIo.ReadWrite, arg1, arg2, arg3, arg4);
    }

    internal void ControlReadWrite(ControlType type, ReadOnlySpan<nint> arguments)
    {
        Control(type, ControlIo.ReadWrite, arguments);
    }

    internal void ControlNone(ControlType type)
    {
        Control(type, ControlIo.None);
    }
}
