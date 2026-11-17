namespace Cthangover.Mods.Exploration
{
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Mods;

    /// <summary>
    /// Registers only persistent-domain reset. Native scenes, observers and visibility algorithms remain adapter-owned.
    /// </summary>
    public sealed class ExplorationInitializer : IModInitializer, IModUnloadAware
    {
        /// <summary>
        /// Does not access services before the ordinary resource-ready phase.
        /// </summary>
        public void OnModLoaded(string modId)
        {
        }

        /// <summary>
        /// Registers the deduplicated reset owner once application services are available.
        /// </summary>
        public void OnModResourcesReady()
        {
            AppServices.Current.RegisterResettable(new ExplorationReset());
        }

        /// <summary>
        /// Invalidates borrowed observer handles before the mod's retained assembly can join another game.
        /// </summary>
        public void OnModUnloading()
        {
            ExplorationMemory.Reset();
        }

        /// <summary>
        /// No worker or native-resource cleanup remains after the unload preparation phase.
        /// </summary>
        public void OnModUnloaded(string modId)
        {
        }
    }
}
