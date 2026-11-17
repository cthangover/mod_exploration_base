namespace Cthangover.Mods.Exploration
{
    using System;
    using System.Text.Json;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Settings;

    /// <summary>
    /// Ordinary mod-slot persistence for explored cells only. Projection adapters never become a second owner
    /// of exploration save data, and unsupported versions fail before replacing the live generation.
    /// </summary>
    public sealed class ExplorationSaveable : IModSaveable
    {
        /// <summary>
        /// Captures a detached persistent snapshot, excluding observer visibility and runtime revisions.
        /// </summary>
        public void OnSave(ModSlot slot)
        {
            slot.Data["Exploration"] = new DataBlob(1, ExplorationMemory.Snapshot());
        }

        /// <summary>
        /// Validates a complete version-one record before publication; an absent shelf restores empty memory.
        /// </summary>
        public bool OnLoad(ModSlot slot, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var restored = new ExplorationState();
                if (slot.Data.TryGetValue("Exploration", out var blob))
                {
                    if (blob.Version != 1)
                    {
                        throw new ArgumentException("Unsupported exploration save version");
                    }

                    restored = JsonSerializer.Deserialize<ExplorationState>(blob.GetBytes())
                        ?? throw new ArgumentException("Missing exploration save data");
                }

                ExplorationMemory.Restore(restored);
                return true;
            }
            catch (Exception error)
            {
                errorMessage = error.Message;
                return false;
            }
        }
    }
}
