# Shared exploration memory

`exploration_base` persists explored map cells with the playthrough. It does not draw a map, calculate visibility through walls or create a camera. 2D and 3D adapters share this owner while retaining their own coordinate and visibility rules.

## Game authors

Enable the editor for your chosen map presentation; it supplies this dependency. Configure maps, initial knowledge and markers through that editor's forms. This shared base needs no manual JSON and has no separate authoring window. Currently visible cells are temporary; explored cells persist. New game clears knowledge.

## Adapter contract

`ExplorationMemory.Open(id, width, height, initial)` returns the current `ExplorationMap`. Use a namespace such as `my_game/town_map`. Row-major index is `y * width + x`. The optional initial-index list applies only when the record is first created; reopening does not reveal those cells again.

`Reveal(indices)` adds persistent knowledge. `SetVisible(indices)` replaces current visibility and explores its cells. `ClearVisibility()` removes current visibility. Read through `IsExplored(x,y)` and `IsVisible(x,y)`. `Revision` advances only when data actually changes; resubmitting the same visibility set does not dirty presentation.

After load or new game, an old handle's `IsCurrent` becomes false. Open it again; old reads/writes refuse. An existing record cannot change width/height. Use a new identity when cell interpretation changes; an adapter can include a coordinate-transform signature in its key.

## Persistence and limits

The ordinary `IModSaveable` writes version 1 to the `Exploration` shelf. Dimensions and explored bit arrays persist; current visibility and Revision do not. `Snapshot()` returns detached data. `Restore()` validates and copies everything before replacing the generation; invalid data leaves current memory unchanged. A missing shelf restores an empty generation.

Up to 256 maps, each dimension 1–2048 and at most 1,048,576 cells per map. Every update is validated completely before bits change. Calls belong on the main thread. Remembered objects, doors, images and streaming state belong to their presentation owners.

The focused `exploration_units` suite covers bounds, independent maps/snapshots, initial knowledge, visibility replacement, atomic refusals, stale handles and a complete ordinary mod-slot JSON roundtrip.
