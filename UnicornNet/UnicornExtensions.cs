namespace UnicornNet;

/// <summary>
/// Extension methods for common Unicorn operations
/// </summary>
public static class UnicornExtensions
{
    extension(Unicorn engine)
    {
        /// <summary>
        /// Map a stack region with read/write permissions
        /// </summary>
        public MemoryRegion MapStack(ulong address, ulong size)
        {
            ArgumentNullException.ThrowIfNull(engine);
            return engine.MapRegion(address, size, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);
        }
        /// <summary>
        /// Map a heap region with read/write permissions
        /// </summary>
        public MemoryRegion MapHeap(ulong address, ulong size)
        {
            ArgumentNullException.ThrowIfNull(engine);
            return engine.MapRegion(address, size, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);
        }
        /// <summary>
        /// Map a code region, write the code, and set execute permissions
        /// </summary>
        public MemoryRegion MapCode(ulong address, ReadOnlySpan<byte> code)
        {
            ArgumentNullException.ThrowIfNull(engine);

            // Align size to page boundary (4KB)
            var alignedSize = ((ulong)code.Length + 0xFFF) & ~0xFFFul;

            var region = engine.MapRegion(address, alignedSize, Unicorn.MemoryPermissions.All);
            engine.MemWrite(address, code);

            // Set to read + execute only for better security
            region.Protect(Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Execute);

            return region;
        }
        /// <summary>
        /// Map a data region with read/write permissions and write initial data
        /// </summary>
        public MemoryRegion MapData(ulong address, ReadOnlySpan<byte> data)
        {
            ArgumentNullException.ThrowIfNull(engine);

            // Align size to page boundary (4KB)
            var alignedSize = ((ulong)data.Length + 0xFFF) & ~0xFFFul;

            var region = engine.MapRegion(address, alignedSize, Unicorn.MemoryPermissions.Read | Unicorn.MemoryPermissions.Write);
            engine.MemWrite(address, data);

            return region;
        }
        /// <summary>
        /// Map a read-only data region
        /// </summary>
        public MemoryRegion MapReadOnlyData(ulong address, ReadOnlySpan<byte> data)
        {
            ArgumentNullException.ThrowIfNull(engine);

            // Align size to page boundary (4KB)
            var alignedSize = ((ulong)data.Length + 0xFFF) & ~0xFFFul;

            var region = engine.MapRegion(address, alignedSize, Unicorn.MemoryPermissions.All);
            engine.MemWrite(address, data);

            // Set to read-only
            region.Protect(Unicorn.MemoryPermissions.Read);

            return region;
        }
        /// <summary>
        /// Write a value to memory at the specified address
        /// </summary>
        public void WriteBytes(ulong address, params byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(bytes);
            engine.MemWrite(address, bytes);
        }
        /// <summary>
        /// Read bytes from memory
        /// </summary>
        public byte[] ReadBytes(ulong address, int count)
        {
            ArgumentNullException.ThrowIfNull(engine);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
            }

            var buffer = new byte[count];
            engine.MemRead(address, buffer);
            return buffer;
        }
    }
}
