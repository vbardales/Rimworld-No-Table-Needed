using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NoTableNeeded
{
    /// <summary>Families of food this mod excuses from wanting a table.</summary>
    public enum FoodFamily
    {
        /// <summary>Untouched: it is a meal, and a meal is eaten sitting down.</summary>
        None,

        /// <summary>
        /// Preferability below MealTerrible: raw produce, eggs, fish, milk, chocolate, kibble,
        /// raw meat. The game already declines to treat these as meals everywhere else.
        /// </summary>
        NotAMeal,

        /// <summary>
        /// A meal that keeps: pemmican, packaged survival meal, insect jelly. Built to be
        /// carried, therefore built to be eaten wherever the carrier happens to be.
        /// </summary>
        PreservedRation,
    }

    /// <summary>
    /// The food half of the mod: classify ingestibles, then write
    /// <c>ingestible.tableDesired</c>.
    ///
    /// That bool (default true, on IngestibleProperties) has exactly ONE reader in the whole of
    /// Assembly-CSharp, Toils_Ingest.FinalizeIngest:
    ///
    ///     if (... chairSearchRadius > 10f)
    ///       if (!facingCell.HasEatSurface(...) &amp;&amp; posture == Standing
    ///           &amp;&amp; !IsWildMan() &amp;&amp; thing.def.ingestible.tableDesired)
    ///         TryGainMemory(ThoughtDefOf.AteWithoutTable);
    ///
    /// Which fixes the blast radius precisely: the "ate without table" memory, and nothing else.
    /// The base game only ever sets it on beer.
    ///
    /// The other apparent lever, dropping <c>chairSearchRadius</c> to 10, would suppress the same
    /// memory through the outer condition. But that field is really the chair search radius
    /// (Toils_Ingest.TryFindChairOrSpot hands it to GenClosest.ClosestThingReachable as
    /// maxDistance), so lowering it also shortens how far a colonist will walk to sit down. This
    /// mod leaves seat-finding exactly as the base game has it.
    /// </summary>
    public static class TableEtiquette
    {
        /// <summary>
        /// The keeping time, in days before rot, that separates a meal from a ration.
        ///
        /// Base game: cooked meals rot in 4 days, nutrient paste in 0.75, baby food in 14;
        /// pemmican lasts 70; packaged survival meals and insect jelly carry no CompRottable at
        /// all. Any threshold between 14 and 70 makes the cut; 20 leaves room on both sides for
        /// rations added by other mods.
        /// </summary>
        public const float RationDaysToRotStart = 20f;

        /// <summary>
        /// The value each def carried before this mod first wrote to it. Without it, unticking a
        /// food in the settings would write a hard true and clobber another mod's decision, or
        /// the game's own in the case of beer.
        /// </summary>
        private static readonly Dictionary<ThingDef, bool> originalTableDesired =
            new Dictionary<ThingDef, bool>();

        private static List<ThingDef> candidates;

        /// <summary>False until the game has finished loading defs.</summary>
        public static bool HasRun => candidates != null;

        /// <summary>Every food matching one of the two families, ordered for display.</summary>
        public static List<ThingDef> Candidates => candidates ?? new List<ThingDef>();

        /// <summary>
        /// Classify one food. Deterministic, and built on vanilla fields only, so food from any
        /// other mod is sorted by the same measure: no hardcoded defName list, no
        /// PatchOperationFindMod to keep up to date.
        /// </summary>
        public static FoodFamily FamilyOf(ThingDef def)
        {
            var ingestible = def?.ingestible;
            if (ingestible == null) return FoodFamily.None;

            // Drops drugs and serums (no nutrition) along with growing plants and paste
            // dispensers: you do not carry those, and a plant grazed by an animal has no mood
            // to hurt in the first place.
            if (!def.IsNutritionGivingIngestible || !def.EverHaulable) return FoodFamily.None;

            // Corpses are left to the base game. The rule would take them happily enough, but
            // eating a body is punished quite enough elsewhere, and corpse defs are generated one
            // per race: including them would bury the settings list under a hundred and fifty
            // rows nobody is ever going to untick.
            if ((ingestible.foodType & FoodTypeFlags.Corpse) != FoodTypeFlags.None) return FoodFamily.None;

            if (ingestible.preferability < FoodPreferability.MealTerrible) return FoodFamily.NotAMeal;

            return KeepsLikeARation(def) ? FoodFamily.PreservedRation : FoodFamily.None;
        }

        private static bool KeepsLikeARation(ThingDef def)
        {
            var rot = def.GetCompProperties<CompProperties_Rottable>();
            return rot == null || rot.daysToRotStart >= RationDaysToRotStart;
        }

        /// <summary>
        /// Collect the affected foods. Called from a <see cref="StaticConstructorOnStartupAttribute"/>,
        /// hence after GenerateImpliedDefs_PostResolve: defs built at load time (raw meats) are
        /// already in the database and sort like any other. That is the whole reason this half of
        /// the mod needs an assembly rather than a PatchOperation, which never sees them.
        /// </summary>
        private static void Scan()
        {
            if (candidates != null) return;

            candidates = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => FamilyOf(def) != FoodFamily.None)
                .OrderBy(def => (int)FamilyOf(def))
                .ThenBy(def => def.label ?? def.defName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Write <c>tableDesired</c> from the settings. Replayable: called at startup and again
        /// whenever a setting changes, which is what makes the checkboxes take effect without a
        /// restart, the field being read afresh at every meal.
        /// </summary>
        public static void Apply()
        {
            Scan();

            var settings = NoTableNeededMod.Settings;
            foreach (var def in candidates)
            {
                if (!originalTableDesired.TryGetValue(def, out var original))
                {
                    original = def.ingestible.tableDesired;
                    originalTableDesired[def] = original;
                }

                def.ingestible.tableDesired = settings.IsExempt(def) ? false : original;
            }
        }

        /// <summary>How many foods currently do not ask for a table.</summary>
        public static int ExemptCount()
            => Candidates.Count(def => NoTableNeededMod.Settings.IsExempt(def));
    }

    [StaticConstructorOnStartup]
    internal static class Startup
    {
        static Startup()
        {
            try
            {
                TableEtiquette.Apply();
            }
            catch (Exception ex)
            {
                // Throwing here would leave the game half-loaded. Log and hand back control:
                // without the mod, "ate without table" simply behaves as the base game does.
                Log.Error("[No Table Needed] startup scan failed, table etiquette left untouched:\n" + ex);
            }
        }
    }
}
