using System.Runtime.CompilerServices;

namespace UnicornNet;

/// <summary>
///     Extension methods for common Unicorn operations
/// </summary>
public static class UnicornExtensions
{
    /// <summary>
    ///     Page size used for memory alignment (4KB)
    /// </summary>
    private const ulong PageSize = 0x1000;

    /// <summary>
    ///     Page mask for alignment calculations
    /// </summary>
    private const ulong PageMask = PageSize - 1;

    /// <summary>
    ///     Aligns a size value to the next page boundary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong AlignToPageSize(ulong size)
    {
        return size + PageMask & ~PageMask;
    }

    extension(Unicorn engine)
    {
        /// <summary>
        ///     Map a stack region with read/write permissions
        /// </summary>
        public MemoryRegion MapStack(ulong address, ulong size)
        {
            ArgumentNullException.ThrowIfNull(engine);
            return engine.MapRegion(address, size, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);
        }
        /// <summary>
        ///     Map a heap region with read/write permissions
        /// </summary>
        public MemoryRegion MapHeap(ulong address, ulong size)
        {
            ArgumentNullException.ThrowIfNull(engine);
            return engine.MapRegion(address, size, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);
        }
        /// <summary>
        ///     Map a code region, write the code, and set execute permissions
        /// </summary>
        public MemoryRegion MapCode(ulong address, ReadOnlySpan<byte> code)
        {
            ArgumentNullException.ThrowIfNull(engine);

            var alignedSize = AlignToPageSize((ulong)code.Length);

            var region = engine.MapRegion(address, alignedSize, Unicorn.MemoryPermissions.All);
            engine.MemWrite(address, code);

            // Set to read + execute only for better security
            region.Protect(Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Execute);

            return region;
        }
        /// <summary>
        ///     Map a data region with read/write permissions and write initial data
        /// </summary>
        public MemoryRegion MapData(ulong address, ReadOnlySpan<byte> data)
        {
            ArgumentNullException.ThrowIfNull(engine);

            var alignedSize = AlignToPageSize((ulong)data.Length);

            var region = engine.MapRegion(address, alignedSize, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);
            engine.MemWrite(address, data);

            return region;
        }
        /// <summary>
        ///     Map a read-only data region
        /// </summary>
        public MemoryRegion MapReadOnlyData(ulong address, ReadOnlySpan<byte> data)
        {
            ArgumentNullException.ThrowIfNull(engine);

            var alignedSize = AlignToPageSize((ulong)data.Length);

            var region = engine.MapRegion(address, alignedSize, Unicorn.MemoryPermissions.All);
            engine.MemWrite(address, data);

            // Set to read-only
            region.Protect(Unicorn.MemoryPermissions.Read);

            return region;
        }
        /// <summary>
        ///     Write bytes to memory at the specified address
        /// </summary>
        public void WriteBytes(ulong address, params byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            engine.MemWrite(address, bytes);
        }

        /// <summary>
        ///     Write bytes to memory at the specified address without allocation (Span-based)
        /// </summary>
        public void WriteBytes(ulong address, ReadOnlySpan<byte> bytes)
        {
            engine.MemWrite(address, bytes);
        }

        /// <summary>
        ///     Read bytes from memory into the provided destination span (zero-allocation)
        /// </summary>
        public void ReadBytes(ulong address, Span<byte> destination)
        {
            engine.MemRead(address, destination);
        }

        /// <summary>
        ///     Read bytes from memory and return as a new array
        /// </summary>
        public byte[] ReadBytes(ulong address, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            var buffer = new byte[count];
            engine.MemRead(address, buffer);
            return buffer;
        }
    }
}