using System;

namespace UnicornNet;

public partial class Unicorn : IDisposable
{
    private readonly ControlEngine _control;
    private readonly SafeEngineHandle _engineHandle;
    private readonly MemoryManager _memory;
    private readonly IUnicornNativeProxy _native;
    private readonly RegisterBank _registers;
    private bool _disposed;

    public Unicorn(Architecture architecture, Mode mode)
        : this(architecture, mode, UnicornOptions.Default, NativeUnicornProxy.Instance)
    {
    }

    public Unicorn(Architecture architecture, Mode mode, UnicornOptions options)
        : this(architecture, mode, options, NativeUnicornProxy.Instance)
    {
    }

    internal Unicorn(Architecture architecture, Mode mode, IUnicornNativeProxy native)
        : this(architecture, mode, UnicornOptions.Default, native)
    {
    }

    internal Unicorn(Architecture architecture, Mode mode, UnicornOptions options, IUnicornNativeProxy native)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(native);

        EngineArchitecture = architecture;
        EngineMode = mode;
        Options = options;
        _native = native;

        var err = _native.Open((int)architecture, (int)mode, out var handle);
        if (err != 0 || handle == IntPtr.Zero)
        {
            throw new UnicornEngineException((ErrorCode)err, "uc_open");
        }

        _engineHandle = new SafeEngineHandle(handle, _native);
        _control = new ControlEngine(_native, () => EngineHandle, EnsureNotDisposed);
        _memory = new MemoryManager(_native, () => EngineHandle, EnsureNotDisposed);
        _registers = new RegisterBank(_native, () => EngineHandle, EnsureNotDisposed);
    }

    /// <summary>
    ///     Gets the configuration options for this engine instance.
    /// </summary>
    public UnicornOptions Options { get; }

    /// <summary>
    ///     Gets the architecture this engine was created with.
    /// </summary>
    public Architecture EngineArchitecture { get; }

    /// <summary>
    ///     Gets the mode this engine was created with.
    /// </summary>
    public Mode EngineMode { get; }

    /// <summary>
    ///     Gets the logger configured for this engine instance, if any.
    /// </summary>
    internal IUnicornLogger? Logger
    {
        get => Options.Logger;
    }

    private IntPtr EngineHandle
    {
        get => _engineHandle?.DangerousGetHandle() ?? IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearHooks();
        _engineHandle?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~Unicorn()
    {
        Dispose();
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Unicorn));
    }

    /// <summary>
    ///     Removes cached translation blocks for the specified address range.
    ///     This is useful when code has been modified at runtime and the translation cache needs to be invalidated.
    /// </summary>
    /// <param name="address">The start address of the range to remove from cache</param>
    /// <param name="end">The end address of the range to remove from cache</param>
    public void RemoveCache(ulong address, ulong end)
    {
        ControlWrite(ControlType.TranslationBlockRemove, (nint)address, (nint)end);
    }

    /// <summary>
    ///     Flushes the entire translation block cache.
    ///     This invalidates all cached translation blocks and forces recompilation on next execution.
    /// </summary>
    public void FlushCache()
    {
        ControlWrite(ControlType.TranslationBlockFlush);
    }

    public ErrorCode GetLastError()
    {
        EnsureNotDisposed();
        return (ErrorCode)_native.Errno(EngineHandle);
    }

    /// <summary>
    ///     Gets the current error state of the engine.
    ///     This can be used to check if emulation stopped due to an error.
    /// </summary>
    /// <returns>The current error code, or <see cref="ErrorCode.Ok" /> if no error.</returns>
    public ErrorCode GetErrorState()
    {
        return GetLastError();
    }

    /// <summary>
    ///     Validates that all registered hooks are in a healthy state.
    /// </summary>
    /// <returns>
    ///     A tuple containing:
    ///     - isHealthy: true if all hooks are valid, false if any hooks are disposed or invalid
    ///     - disposedCount: number of hooks that have been disposed
    ///     - totalCount: total number of registered hooks
    /// </returns>
    /// <remarks>
    ///     This method is useful for diagnosing issues where hooks may have been
    ///     inadvertently disposed or are in an invalid state. If hooks are disposed
    ///     while emulation is running, they will silently stop working.
    /// </remarks>
    public (bool isHealthy, int disposedCount, int totalCount) ValidateHooks()
    {
        EnsureNotDisposed();

        var totalCount = _hookRegistry.Count;
        var disposedCount = 0;

        foreach (var registration in _hookRegistry.Values)
        {
            // Check if the registration is disposed by trying to access its handle
            // We can't directly check _disposed as it's private, so we check if handle is empty
            // and if the GCHandle is still allocated
            try
            {
                if (registration.Handle.IsEmpty)
                {
                    disposedCount++;
                }
            }
            catch
            {
                disposedCount++;
            }
        }

        var isHealthy = disposedCount == 0;

        if (!isHealthy && Logger != null)
        {
            Logger.LogWarning($"Hook validation failed: {disposedCount} of {totalCount} hooks are disposed or invalid");
        }

        return (isHealthy, disposedCount, totalCount);
    }
}
