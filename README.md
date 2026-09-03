# No Table Needed

Removes the "ate without table" thought for food that is not a meal, for rations built to
travel, and for anyone hungry enough not to care.

## The lever

RimWorld 1.6, `Toils_Ingest.FinalizeIngest`:

```csharp
if (ingester.needs.mood != null && thing.def.IsNutritionGivingIngestible
    && thing.def.ingestible.chairSearchRadius > 10f)
{
    if (!(ingester.Position + ingester.Rotation.FacingCell).HasEatSurface(actor.Map)
        && ingester.GetPosture() == PawnPosture.Standing
        && !ingester.IsWildMan()
        && thing.def.ingestible.tableDesired)
    {
        ingester.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.AteWithoutTable);
    }
```

`IngestibleProperties.tableDesired` (bool, default `true`) has **no other reader** in
`Assembly-CSharp` — checked against a full decompilation of 1.6.4871. The base game only ever
sets it on beer. It is an exact lever: the thought, and nothing else.

The other apparent lever, `chairSearchRadius`, produces the same effect **by accident**, through
the `> 10f` in the outer `if`. But that field is really the chair search radius
(`Toils_Ingest.TryFindChairOrSpot` hands it to `GenClosest.ClosestThingReachable` as
`maxDistance`). Dropping it to 10 also changes how far a colonist will walk to reach a chair.
This mod does not touch it.

## The food rule

Two families, both decided on vanilla fields alone:

| Family | Test | Base game |
|---|---|---|
| Not a meal | `ingestible.preferability < MealTerrible` | raw produce, eggs, fish, milk, chocolate, ambrosia, kibble, raw meat |
| Preserved ration | a meal with `CompProperties_Rottable.daysToRotStart >= 20`, or no `CompRottable` at all | pemmican (70 d), packaged survival meal (never), insect jelly (never) |

The 20-day threshold is not arbitrary. In the base game cooked meals rot in **4** days, nutrient
paste in 0.75 and baby food in 14; pemmican lasts **70** and survival meals never rot. Anything
between 14 and 70 separates the two groups; 20 leaves room on both sides for rations added by
other mods.

No hardcoded `defName` list, no `PatchOperationFindMod`: a mod adding hardtack or dried meat is
sorted by the same measure, with no compatibility patch to write.

### Deliberate exclusions

- **Corpses** (`foodType` containing `Corpse`). The rule would take them happily enough, but
  eating a body is punished quite enough elsewhere, and those defs are generated one per race:
  including them would bury the settings list under a hundred and fifty rows.
- **Growing plants and paste dispensers**: dropped by `EverHaulable`.
- **Drugs and serums**: dropped by `IsNutritionGivingIngestible` (no nutrition).

## The hunger rule

Past **urgently hungry** — or, if you prefer, only once **starving** — the thought is skipped
whatever the food was, lavish meal included.

The threshold matters. Colonists start looking for food while merely *hungry*, so forgiving from
there would drop the thought on nearly every meal. `UrgentlyHungry` is the exceptional state, the
one where the pawn could not eat in time: caravans, sieges, famine, a neglected prisoner.

Hunger is read where the memory is granted, which is **before** `thing.Ingested()` and before
`needs.food.CurLevel` is credited. What the mod sees is therefore the hunger the pawn walked in
with, not the hunger they leave with.

## Why an assembly rather than a `PatchOperation`

Raw meats (`Meat_*`) are not in the XML at all: `ThingDefGenerator_Meat` builds them at load time,
one per race, with `preferability = RawBad`. `PatchOperation`s apply to the XML before that
generation and **never** see them.

So the mod hooks a `[StaticConstructorOnStartup]`. In `PlayDataLoader.DoPlayLoad`,
`StaticConstructorOnStartupUtility.CallAll()` runs after `GenerateImpliedDefs_PostResolve`: every
def, implied ones included, is in the database. Which is also why **load order makes no
difference** — a mod loaded after this one has already poured its food in.

## Why a Harmony patch

Only for hunger. It is a per-pawn, per-meal state, and no vanilla field expresses it:
`ThoughtUtility.CanGetThought` offers no per-def worker for memories, and `ThoughtMaker` calls
`Init()` before `pawn` is assigned, so a custom `thoughtClass` could not read the eater either.

The patch is a prefix on `MemoryThoughtHandler.TryGainMemory(ThoughtDef, Pawn, Precept)` — public,
named, and exactly the overload `FinalizeIngest` calls. Patching `FinalizeIngest` itself would
mean targeting an anonymous delegate whose compiler-generated name shifts with every Ludeon
recompile.

## Settings

One checkbox per food family, above the full list of affected foods with search and a per-food
override. One checkbox for hunger, with its threshold.

Unticking a food restores the value it held **before** the mod loaded (recorded on first write),
never a hard `true`: another mod's decision is never overwritten.

Everything takes effect immediately. `tableDesired` is read afresh at every meal, and the hunger
patch reads the settings live.

## Building

Requires the .NET SDK. Reference assemblies come from NuGet, so a RimWorld installation is not
needed to compile.

```bash
dotnet build Source/NoTableNeeded.csproj -c Release
```

The assembly is written straight into `Mod/Assemblies/`.

For a quick iteration loop, make `RimWorld/Mods/NoTableNeeded` an NTFS junction pointing at
**`Mod/`** — not at the repository root: a rebuild then lands in the game with nothing to copy.

## Layout

Only `Mod/` is published. RimWorld's uploader hands Steam the mod directory as-is
(`SteamUGC.SetItemContent` on `ModMetaData.RootDir`, with no filtering whatsoever), so anything
sitting in that folder is downloaded by every subscriber. Sources and build intermediates
therefore live outside it.

```
Mod/            <- the published mod, and the junction target
  About/          metadata, Workshop preview and mod icon
  Assemblies/     the built assembly
  Languages/      English and French
  LICENSE         MIT requires the notice to travel with the distribution
Source/         C# sources, never published
.build/         build intermediates, git-ignored, never published
```

## Licence

MIT — see [LICENSE](LICENSE).

This mod reimplements an idea taken from another mod without copying any of its code; the detail
is in [ATTRIBUTION.md](ATTRIBUTION.md).

Its code was written with Claude Code (Anthropic), under human direction, review and testing.

If I do not answer within a reasonable time after being contacted, anyone may freely update this
or any other of my mods, including publishing a continuation of it. All credit must be preserved.
