using RimWorld;
using Verse;

namespace NoTableNeeded
{
    /// <summary>
    /// The hunger half of the mod. Unlike <see cref="TableEtiquette"/>, which writes one bool per
    /// food once and for all, hunger is a per-pawn, per-meal state. No vanilla field expresses it,
    /// which is why the mod carries a single Harmony patch.
    /// </summary>
    public static class HungerExcuse
    {
        /// <summary>
        /// True if this pawn has an excuse not to be fussy about the furniture.
        ///
        /// Read from Toils_Ingest.FinalizeIngest, which gains the memory BEFORE calling
        /// thing.Ingested() and crediting needs.food.CurLevel. What is observed here is therefore
        /// the hunger the pawn walked in with, not that of a pawn already fed.
        /// </summary>
        public static bool TooHungryToCare(Pawn pawn)
        {
            var settings = NoTableNeededMod.Settings;
            if (!settings.exemptWhenHungry) return false;

            // No food need: mechanoids, entities. They have no mood either, so FinalizeIngest
            // would never have reached this point, but the patch guards every source of the
            // memory rather than only that one.
            var food = pawn?.needs?.food;
            if (food == null) return false;

            return food.CurCategory >= settings.hungerThreshold;
        }
    }
}
