namespace Cthangover.Mods.Exploration
{
    using Cthangover.Core.Bootstrap;

    /// <summary>
    /// Integrates explored memory with new-game reset; changing map presentations does not reset the playthrough.
    /// </summary>
    public sealed class ExplorationReset : IResettable
    {
        /// <summary>
        /// Stable owner key lets the lifecycle registry replace retained-assembly registrations without duplicates.
        /// </summary>
        public string Id
        {
            get
            {
                return "exploration_base";
            }
        }

        /// <summary>
        /// Starts a fresh save generation and invalidates every previous observer's map handle.
        /// </summary>
        public void ResetForNewGame()
        {
            ExplorationMemory.Reset();
        }
    }
}
