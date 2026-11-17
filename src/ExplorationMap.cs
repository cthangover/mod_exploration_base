namespace Cthangover.Mods.Exploration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Generation-bound main-thread view of a persistent grid. Adapters supply visible cells; this class owns
    /// explored bits and transient visibility, without choosing a field-of-view or world-coordinate algorithm.
    /// </summary>
    public sealed class ExplorationMap
    {
        private readonly ExplorationRecord record;

        private readonly ExplorationState generation;

        private HashSet<int> visible = new();

        internal ExplorationMap(ExplorationRecord record, ExplorationState generation)
        {
            this.record = record;
            this.generation = generation;
        }

        /// <summary>
        /// Fixed grid column count; coordinate adapters must use this width for row-major indices.
        /// </summary>
        public int Width
        {
            get
            {
                return record.Width;
            }
        }

        /// <summary>
        /// Fixed row count; cells outside this rectangle are never implicitly clamped or revealed.
        /// </summary>
        public int Height
        {
            get
            {
                return record.Height;
            }
        }

        /// <summary>
        /// Presentation revision advances once per operation that changes explored bits or current visibility.
        /// </summary>
        public long Revision { get; private set; }

        /// <summary>
        /// False after a load or new game; adapters must reopen the map before reading or changing its cells.
        /// </summary>
        public bool IsCurrent
        {
            get
            {
                return ExplorationMemory.IsCurrent(generation);
            }
        }

        /// <summary>
        /// Reads persistent knowledge at a validated coordinate, refusing access through obsolete handles.
        /// </summary>
        public bool IsExplored(int x, int y)
        {
            var index = Index(x, y);
            return (record.Explored[index / 8] & (1 << (index % 8))) != 0;
        }

        /// <summary>
        /// Reads current adapter-supplied visibility; this state is not restored from a saved game.
        /// </summary>
        public bool IsVisible(int x, int y)
        {
            return visible.Contains(Index(x, y));
        }

        /// <summary>
        /// Permanently reveals validated indices without changing current visibility. Duplicate cells are harmless;
        /// a bad index leaves all bits and the revision unchanged.
        /// </summary>
        public void Reveal(IReadOnlyList<int> cells)
        {
            EnsureCurrent();
            ValidateCells(cells, Width * Height);
            if (RevealValidated(cells))
            {
                Revision++;
            }
        }

        /// <summary>
        /// Atomically replaces transient visibility and reveals its cells. Null means no current visible cells;
        /// previously explored cells remain known.
        /// </summary>
        public void SetVisible(IReadOnlyList<int> cells)
        {
            EnsureCurrent();
            ValidateCells(cells, Width * Height);
            var next = cells == null ? new HashSet<int>() : new HashSet<int>(cells);
            var changed = !visible.SetEquals(next);
            changed |= RevealValidated(cells);
            visible = next;
            if (changed)
            {
                Revision++;
            }
        }

        /// <summary>
        /// Drops current visibility when an observer leaves, preserving the explored layer for later visits.
        /// </summary>
        public void ClearVisibility()
        {
            SetVisible(null);
        }

        internal static void ValidateCells(IReadOnlyList<int> cells, int count)
        {
            if (cells == null)
            {
                return;
            }

            if (cells.Count > 1048576)
            {
                throw new ArgumentException("Exploration update exceeds cell budget");
            }

            foreach (var cell in cells)
            {
                if (cell < 0 || cell >= count)
                {
                    throw new ArgumentOutOfRangeException(nameof(cells), "Exploration index is outside the grid");
                }
            }
        }

        private bool RevealValidated(IReadOnlyList<int> cells)
        {
            var changed = false;
            if (cells == null)
            {
                return false;
            }

            foreach (var cell in cells)
            {
                var before = record.Explored[cell / 8];
                var after = (byte)(before | (1 << (cell % 8)));
                changed |= before != after;
                record.Explored[cell / 8] = after;
            }

            return changed;
        }

        private int Index(int x, int y)
        {
            EnsureCurrent();
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Exploration coordinate is outside the grid");
            }

            return y * Width + x;
        }

        private void EnsureCurrent()
        {
            if (!IsCurrent)
            {
                throw new InvalidOperationException("Exploration handle belongs to a previous save generation");
            }
        }
    }
}
