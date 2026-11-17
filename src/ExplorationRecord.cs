namespace Cthangover.Mods.Exploration
{
    using System;

    /// <summary>
    /// Persistent row-major explored bits. Visibility and presentation are deliberately absent from save data;
    /// replacing authored grid dimensions requires a new identity or a new playthrough.
    /// </summary>
    public sealed class ExplorationRecord
    {
        /// <summary>
        /// Column count used to interpret every persisted bit, validated together with Height before restore.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Row count; adapters map their own coordinates into this projection-independent rectangle.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Owned little-bit-order bytes: index y * Width + x uses bit index % 8 of byte index / 8.
        /// </summary>
        public byte[] Explored { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Rejects invalid dimensions, oversized allocations, mismatched byte counts and nonzero padding bits.
        /// </summary>
        public void Validate()
        {
            ValidateDimensions(Width, Height);
            var cells = Width * Height;
            if (Explored == null || Explored.Length != (cells + 7) / 8)
            {
                throw new ArgumentException("Exploration bit count does not match grid dimensions");
            }

            var remainder = cells % 8;
            if (remainder != 0 && (Explored[^1] >> remainder) != 0)
            {
                throw new ArgumentException("Exploration padding bits must be zero");
            }
        }

        /// <summary>
        /// Bounds each dimension and the combined grid before callers allocate persistent or transient arrays.
        /// </summary>
        public static void ValidateDimensions(int width, int height)
        {
            if (width < 1 || height < 1 || width > 2048 || height > 2048 || (long)width * height > 1048576)
            {
                throw new ArgumentException("Exploration grid exceeds supported bounds");
            }
        }
    }
}
