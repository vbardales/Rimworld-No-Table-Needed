# Attributions

## Mods studied

**No code was copied.** No file, no line, no identifier of the mod below is present here.

[**Tables Are For Meals**](https://steamcommunity.com/sharedfiles/filedetails/?id=2198471772)
by **Cozarkian**, for RimWorld 1.2, is where the idea comes from: that non-meal food should not
trigger the "ate without table" thought. It ships its sources but carries no licence file, so it
is all rights reserved and could not have been reused in any case.

The implementation here is a different one, because 1.2 and 1.6 do not offer the same tools:

- `IngestibleProperties.tableDesired` did not exist in 1.2. The original had to lower
  `chairSearchRadius` to 10 instead, which suppresses the thought through the outer condition of
  `Toils_Ingest.FinalizeIngest` but also shortens how far a colonist walks to find a chair. It
  then undid that side effect with a Harmony patch on `GenClosest.RegionwiseBFSWorker`, forcing
  the radius back to 32 for `BuildingArtificial` searches. No Table Needed writes `tableDesired`
  and leaves seat-finding alone entirely.
- The original names the foods it affects one by one, with `PatchOperationFindMod` blocks for
  RimCuisine 2 and Vanilla Plants Expanded. No Table Needed classifies on `preferability` and
  rot time, so it covers modded food it has never heard of.
- The original also rebalances: pemmican nutrition lowered to 0.04, and the "ate without table"
  thought made to stack at -3/-5/-8 over 1.5 days. No Table Needed changes no stat and no thought
  def.
- Hunger as an excuse has no counterpart in the original.

## Thanks

- **Cozarkian**, for the idea and for shipping the sources that made the 1.2 approach legible.
- **Andreas Pardeike**, for [Harmony](https://github.com/pardeike/Harmony).
- **Krafs**, for [Krafs.Rimworld.Ref](https://github.com/krafs/Rimworld.Ref), which lets the mod
  build without a RimWorld installation.
- **Ludeon Studios**, for a game whose code repays reading.
