namespace Cthangover.Mods.Exploration.Tests
{
    using System.Text.Json;
    using Cthangover.Core.Mods.Testing;
    using Cthangover.Core.Settings;

    /// <summary>
    /// Tests the real mod-slot JSON boundary, unsupported versions, missing shelves and new-game reset ownership.
    /// No native renderer or projected map is needed to establish save correctness.
    /// </summary>
    public sealed class ExplorationSaveableTests
    {
        [ModTest("exploration/slot_roundtrip_visibility_reset_and_invalid_version")]
        private static void RoundTrip(ModTestContext context)
        {
            var previous = ExplorationMemory.Snapshot();
            try
            {
                ExplorationMemory.Reset();
                var map = ExplorationMemory.Open("fixture/saved", 9, 2, new[] { 17 });
                map.SetVisible(new[] { 1, 8 });
                var expected = JsonSerializer.Serialize(ExplorationMemory.Snapshot());
                var saveable = new ExplorationSaveable();
                var slot = new ModSlot();
                saveable.OnSave(slot);
                var wire = JsonSerializer.Serialize(slot);
                var restoredSlot = JsonSerializer.Deserialize<ModSlot>(wire);
                new ExplorationReset().ResetForNewGame();
                ModAssert.Equal(0, ExplorationMemory.Snapshot().Maps.Count, "ordinary new-game reset clears maps");
                ModAssert.True(saveable.OnLoad(restoredSlot, out var error), "real wire data loads: " + error);
                ModAssert.Equal(expected, JsonSerializer.Serialize(ExplorationMemory.Snapshot()), "all saved fields restored");
                var restoredMap = ExplorationMemory.Open("fixture/saved", 9, 2);
                ModAssert.True(restoredMap.IsExplored(8, 1), "initial final cell restored");
                ModAssert.True(restoredMap.IsExplored(8, 0), "revealed boundary cell restored");
                ModAssert.False(restoredMap.IsVisible(8, 0), "visibility not persisted");
                restoredSlot.Data["Exploration"] = new DataBlob(99, new ExplorationState());
                ModAssert.False(saveable.OnLoad(restoredSlot, out error), "unsupported version rejected");
                ModAssert.True(!string.IsNullOrEmpty(error), "version diagnostic supplied");
                ModAssert.True(restoredMap.IsCurrent, "rejected slot preserves current generation");
                ModAssert.Equal(expected, JsonSerializer.Serialize(ExplorationMemory.Snapshot()), "rejected slot is atomic");
                ModAssert.True(saveable.OnLoad(new ModSlot(), out error), "absent shelf is a fresh empty generation");
                ModAssert.False(restoredMap.IsCurrent, "absent shelf cannot retain previous game");
                ModAssert.Equal(0, ExplorationMemory.Snapshot().Maps.Count, "absent shelf clears stale data");
            }
            finally
            {
                ExplorationMemory.Restore(previous);
            }
        }
    }
}
