namespace Cthangover.Mods.Exploration
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    /// <summary>
    /// Main-thread exploration owner shared by projection adapters. Reset and load replace the generation;
    /// detached old sessions cannot reveal cells in a different game or resurrect pre-load visibility.
    /// </summary>
    public static class ExplorationMemory
    {
        private static ExplorationState state = new();

        private static readonly Dictionary<string, ExplorationMap> maps = new(StringComparer.Ordinal);

        /// <summary>
        /// Opens a map, applying initial cells only on first creation. Existing grids must retain dimensions.
        /// Invalid initial cells are rejected before creating a record, including when reopening an existing map.
        /// </summary>
        public static ExplorationMap Open(string id, int width, int height, IReadOnlyList<int> initial = null)
        {
            ExplorationState.ValidateIdentity(id);
            ExplorationRecord.ValidateDimensions(width, height);
            ExplorationMap.ValidateCells(initial, width * height);
            if (state.Maps.TryGetValue(id, out var record))
            {
                if (record.Width != width || record.Height != height)
                {
                    throw new ArgumentException("Exploration grid changed; use a new identity or start a new game");
                }
            }
            else
            {
                if (state.Maps.Count >= 256)
                {
                    throw new ArgumentException("Exploration map capacity reached");
                }

                record = new ExplorationRecord
                {
                    Width = width,
                    Height = height,
                    Explored = new byte[(width * height + 7) / 8]
                };
                state.Maps.Add(id, record);
                var created = new ExplorationMap(record, state);
                maps.Add(id, created);
                created.Reveal(initial);
                return created;
            }

            if (!maps.TryGetValue(id, out var map))
            {
                map = new ExplorationMap(record, state);
                maps.Add(id, map);
            }

            return map;
        }

        /// <summary>
        /// Returns a detached persistent snapshot. Mutating its dictionaries or bytes never edits live memory.
        /// </summary>
        public static ExplorationState Snapshot()
        {
            return JsonSerializer.Deserialize<ExplorationState>(JsonSerializer.Serialize(state));
        }

        /// <summary>
        /// Validates and copies all data before replacing the generation. Current visibility is always cleared.
        /// </summary>
        public static void Restore(ExplorationState restored)
        {
            ArgumentNullException.ThrowIfNull(restored);
            restored.Validate();
            var copy = JsonSerializer.Deserialize<ExplorationState>(JsonSerializer.Serialize(restored));
            state = copy;
            maps.Clear();
        }

        /// <summary>
        /// Starts empty explored memory and invalidates all previously issued map handles.
        /// </summary>
        public static void Reset()
        {
            state = new();
            maps.Clear();
        }

        internal static bool IsCurrent(ExplorationState generation)
        {
            return ReferenceEquals(state, generation);
        }
    }
}
