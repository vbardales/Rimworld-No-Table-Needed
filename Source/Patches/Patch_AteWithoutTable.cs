using HarmonyLib;
using RimWorld;
using Verse;

namespace NoTableNeeded
{
    /// <summary>
    /// The mod's only patch.
    ///
    /// It targets <c>TryGainMemory</c> rather than <c>Toils_Ingest.FinalizeIngest</c>, where the
    /// memory is actually granted: that happens inside an anonymous delegate assigned to
    /// <c>toil.initAction</c>, so a compiler-generated method whose name shifts with every Ludeon
    /// recompile. The overload aimed at here is public, named, and exactly the one FinalizeIngest
    /// calls.
    ///
    /// The filter is on the def, not on the caller, so the memory is suppressed wherever it comes
    /// from. In the base game FinalizeIngest is the only source, and were another mod to grant it,
    /// a starving pawn would deserve the same indulgence.
    /// </summary>
    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory),
        new[] { typeof(ThoughtDef), typeof(Pawn), typeof(Precept) })]
    public static class Patch_TryGainMemory
    {
        private static bool Prefix(MemoryThoughtHandler __instance, ThoughtDef def)
        {
            if (def != ThoughtDefOf.AteWithoutTable) return true;

            return !HungerExcuse.TooHungryToCare(__instance.pawn);
        }
    }
}
