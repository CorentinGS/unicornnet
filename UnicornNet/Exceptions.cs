namespace UnicornNet;

/// <summary>
/// Base exception for all Unicorn engine errors
/// </summary>
public class UnicornException : Exception
{
    public Unicorn.ErrorCode ErrorCode { get; }
    public string Operation { get; }

    public UnicornException(Unicorn.ErrorCode errorCode, string operation)
        : base($"{operation} failed with error code {errorCode} ({(int)errorCode})")
    {
        ErrorCode = errorCode;
        Operation = operation;
    }

    public UnicornException(Unicorn.ErrorCode errorCode, string operation, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        Operation = operation;
    }

    public UnicornException(Unicorn.ErrorCode errorCode, string operation, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when engine initialization or control operations fail
/// </summary>
public class UnicornEngineException : UnicornException
{
    public UnicornEngineException(Unicorn.ErrorCode errorCode, string operation)
        : base(errorCode, operation)
    {
    }

    public UnicornEngineException(Unicorn.ErrorCode errorCode, string operation, string message)
        : base(errorCode, operation, message)
    {
    }

    public UnicornEngineException(Unicorn.ErrorCode errorCode, string operation, string message, Exception innerException)
        : base(errorCode, operation, message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when memory operations fail
/// </summary>
public class UnicornMemoryException : UnicornException
{
    public ulong? Address { get; }
    public ulong? Size { get; }

    public UnicornMemoryException(Unicorn.ErrorCode errorCode, string operation, ulong? address = null, ulong? size = null)
        : base(errorCode, operation, BuildMessage(errorCode, operation, address, size))
    {
        Address = address;
        Size = size;
    }

    public UnicornMemoryException(Unicorn.ErrorCode errorCode, string operation, string message, ulong? address = null, ulong? size = null)
        : base(errorCode, operation, message)
    {
        Address = address;
        Size = size;
    }

    private static string BuildMessage(Unicorn.ErrorCode errorCode, string operation, ulong? address, ulong? size)
    {
        var msg = $"{operation} failed with error code {errorCode} ({(int)errorCode})";
        if (address.HasValue)
        {
            msg += $" at address 0x{address.Value:X}";
        }
        if (size.HasValue)
        {
            msg += $" (size: 0x{size.Value:X})";
        }
        return msg;
    }
}

/// <summary>
/// Exception thrown when hook operations fail
/// </summary>
public class UnicornHookException : UnicornException
{
    public Unicorn.HookType? HookType { get; }

    public UnicornHookException(Unicorn.ErrorCode errorCode, string operation, Unicorn.HookType? hookType = null)
        : base(errorCode, operation, BuildMessage(errorCode, operation, hookType))
    {
        HookType = hookType;
    }

    public UnicornHookException(Unicorn.ErrorCode errorCode, string operation, string message, Unicorn.HookType? hookType = null)
        : base(errorCode, operation, message)
    {
        HookType = hookType;
    }

    private static string BuildMessage(Unicorn.ErrorCode errorCode, string operation, Unicorn.HookType? hookType)
    {
        var msg = $"{operation} failed with error code {errorCode} ({(int)errorCode})";
        if (hookType.HasValue)
        {
            msg += $" (hook type: {hookType.Value})";
        }
        return msg;
    }
}
