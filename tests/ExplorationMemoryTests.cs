namespace Cthangover.Mods.Exploration.Tests
{
    using System;
    using System.Text.Json;
    using Cthangover.Core.Mods.Testing;

    /// <summary>
    /// Exercises projection-independent public memory contracts, including atomic invalid input and stale handles.
    /// Tests restore the previous persistent envelope and do not call native engine APIs.
    /// </summary>
    public sealed class ExplorationMemoryTests
    {
        [ModTest("exploration/visible_explored_initial_and_revision")]
        private static void Visibility(ModTestContext context)
        {
            var previous = ExplorationMemory.Snapshot();
            try
            {
                ExplorationMemory.Reset();
                var map = ExplorationMemory.Open("fixture/map", 5, 3, new[] { 0, 14 });
                ModAssert.Equal(5, map.Width, "grid width");
                ModAssert.Equal(3, map.Height, "grid height");
                ModAssert.True(map.IsCurrent, "fresh handle");
                ModAssert.True(map.IsExplored(0, 0), "initial first cell");
                ModAssert.True(map.IsExplored(4, 2), "initial last cell");
                ModAssert.False(map.IsVisible(0, 0), "initial knowledge is not visibility");
                ModAssert.False(map.IsExplored(1, 1), "unvisited cell hidden");
                map.SetVisible(new[] { 6, 7, 7 });
                ModAssert.True(map.IsExplored(1, 1), "visible cells become persistent");
                ModAssert.True(map.IsVisible(2, 1), "current visible cell");
                var revision = map.Revision;
                map.SetVisible(new[] { 7, 6 });
                ModAssert.Equal(revision, map.Revision, "same visible set does not dirty presentation");
                map.SetVisible(new[] { 8 });
                ModAssert.False(map.IsVisible(1, 1), "old visibility replaced");
                ModAssert.True(map.IsExplored(1, 1), "old exploration retained");
                map.Reveal(new[] { 9 });
                ModAssert.True(map.IsExplored(4, 1), "explicit reveal");
                ModAssert.False(map.IsVisible(4, 1), "explicit reveal does not change visibility");
                map.ClearVisibility();
                ModAssert.False(map.IsVisible(3, 1), "observer leaves");
                ModAssert.True(map.IsExplored(3, 1), "knowledge after observer leaves");
                var reopened = ExplorationMemory.Open("fixture/map", 5, 3, new[] { 10 });
                ModAssert.True(ReferenceEquals(map, reopened), "one handle per current map");
                ModAssert.False(reopened.IsExplored(0, 2), "initial reveals do not reapply on reopening");
                ModAssert.False(ExplorationMemory.Open("fixture/other", 5, 3).IsExplored(0, 0), "namespaces isolated");
                var snapshot = ExplorationMemory.Snapshot();
                snapshot.Maps["fixture/map"].Explored[0] = 0;
                ModAssert.True(map.IsExplored(0, 0), "snapshot bytes are detached");
                var wire = JsonSerializer.Serialize(ExplorationMemory.Snapshot());
                ExplorationMemory.Restore(JsonSerializer.Deserialize<ExplorationState>(wire));
                ModAssert.Equal(wire, JsonSerializer.Serialize(ExplorationMemory.Snapshot()), "all persistent fields roundtrip");
                ModAssert.False(map.IsCurrent, "load invalidates old handle");
                var restored = ExplorationMemory.Open("fixture/map", 5, 3);
                ModAssert.True(restored.IsExplored(2, 1), "visited cell restored");
                ModAssert.False(restored.IsVisible(2, 1), "current visibility is not saved");
            }
            finally
            {
                ExplorationMemory.Restore(previous);
            }
        }

        [ModTest("exploration/invalid_updates_and_restore_are_atomic")]
        private static void Invalid(ModTestContext context)
        {
            var previous = ExplorationMemory.Snapshot();
            try
            {
                ExplorationMemory.Reset();
                var map = ExplorationMemory.Open("fixture/atomic", 3, 3, new[] { 0 });
                map.SetVisible(new[] { 1 });
                var revision = map.Revision;
                var before = JsonSerializer.Serialize(ExplorationMemory.Snapshot());
                var rejected = false;
                try
                {
                    map.Reveal(new[] { 2, 9 });
                }
                catch (ArgumentOutOfRangeException)
                {
                    rejected = true;
                }

                ModAssert.True(rejected, "bad reveal index rejected");
                ModAssert.False(map.IsExplored(2, 0), "valid prefix did not mutate");
                rejected = false;
                try
                {
                    map.SetVisible(new[] { 2, -1 });
                }
                catch (ArgumentOutOfRangeException)
                {
                    rejected = true;
                }

                ModAssert.True(rejected, "bad visibility index rejected");
                ModAssert.True(map.IsVisible(1, 0), "previous visibility retained");
                ModAssert.Equal(revision, map.Revision, "invalid operations preserve revision");
                rejected = false;
                try
                {
                    ExplorationMemory.Open("fixture/atomic", 9, 1);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                ModAssert.True(rejected, "same cell count with changed shape cannot reinterpret save");
                var invalid = ExplorationMemory.Snapshot();
                invalid.Maps["fixture/atomic"].Explored[1] |= 128;
                rejected = false;
                try
                {
                    ExplorationMemory.Restore(invalid);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                ModAssert.True(rejected, "nonzero padding rejected");
                ModAssert.True(map.IsCurrent, "invalid restore did not replace generation");
                ModAssert.Equal(before, JsonSerializer.Serialize(ExplorationMemory.Snapshot()), "save unchanged after refusals");
                ExplorationMemory.Reset();
                ModAssert.False(map.IsCurrent, "new game invalidates old handle");
                rejected = false;
                try
                {
                    map.Reveal(new[] { 3 });
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }

                ModAssert.True(rejected, "old observer cannot reveal new game");
                ModAssert.Equal(0, ExplorationMemory.Snapshot().Maps.Count, "new game is still empty");
            }
            finally
            {
                ExplorationMemory.Restore(previous);
            }
        }

        [ModTest("exploration/bounded_dimensions_identity_and_initial_cells")]
        private static void Bounds(ModTestContext context)
        {
            var previous = ExplorationMemory.Snapshot();
            try
            {
                ExplorationMemory.Reset();
                var valid = ExplorationMemory.Open("fixture/large", 2048, 512, new[] { 1048575 });
                ModAssert.True(valid.IsExplored(2047, 511), "largest grid last bit");
                var invalidDimensions = new[]
                {
                    0,
                    -1,
                    2049,
                    int.MaxValue
                };
                foreach (var width in invalidDimensions)
                {
                    var rejected = false;
                    try
                    {
                        ExplorationMemory.Open("fixture/invalid", width, 512);
                    }
                    catch (ArgumentException)
                    {
                        rejected = true;
                    }

                    ModAssert.True(rejected, "invalid dimensions rejected before allocation: " + width);
                }

                var rejectedBudget = false;
                try
                {
                    ExplorationMemory.Open("fixture/oversized", 2048, 2048);
                }
                catch (ArgumentException)
                {
                    rejectedBudget = true;
                }

                ModAssert.True(rejectedBudget, "combined cell budget enforced");
                foreach (var id in new[] { "", "../map", "fixture//map", "fixture\\map", "fixture:map" })
                {
                    var rejected = false;
                    try
                    {
                        ExplorationMemory.Open(id, 1, 1);
                    }
                    catch (ArgumentException)
                    {
                        rejected = true;
                    }

                    ModAssert.True(rejected, "invalid identity rejected");
                }

                var rejectedInitial = false;
                try
                {
                    ExplorationMemory.Open("fixture/new", 1, 1, new[] { 0, 1 });
                }
                catch (ArgumentOutOfRangeException)
                {
                    rejectedInitial = true;
                }

                ModAssert.True(rejectedInitial, "bad initial cells rejected");
                ModAssert.Equal(1, ExplorationMemory.Snapshot().Maps.Count, "invalid opens did not create records");
            }
            finally
            {
                ExplorationMemory.Restore(previous);
            }
        }
    }
}
