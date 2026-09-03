using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NoTableNeeded
{
    public class NoTableNeededSettings : ModSettings
    {
        /// <summary>Berries, eggs, fish, milk, chocolate, kibble, raw meat.</summary>
        public bool exemptNotAMeal = true;

        /// <summary>Pemmican, packaged survival meals, insect jelly, and their modded cousins.</summary>
        public bool exemptPreservedRations = true;

        /// <summary>Excuse a pawn who is eating in an advanced state of hunger.</summary>
        public bool exemptWhenHungry = true;

        /// <summary>
        /// The state from which hunger excuses the meal. UrgentlyHungry by default: pawns go
        /// looking for food as soon as they are Hungry, so excusing from Hungry would drop the
        /// memory on very nearly every meal, which is not the point. UrgentlyHungry is the
        /// exceptional state, caravan, siege, famine, a neglected prisoner, where the pawn could
        /// not eat in time.
        /// </summary>
        public HungerCategory hungerThreshold = HungerCategory.UrgentlyHungry;

        /// <summary>
        /// The user's explicit decisions, by defName. An entry is kept only while it contradicts
        /// its family, so flipping a family checkbox takes back control of everything that was
        /// merely following the default.
        /// </summary>
        public Dictionary<string, bool> overrides = new Dictionary<string, bool>();

        public bool DefaultExempt(ThingDef def)
        {
            switch (TableEtiquette.FamilyOf(def))
            {
                case FoodFamily.NotAMeal: return exemptNotAMeal;
                case FoodFamily.PreservedRation: return exemptPreservedRations;
                default: return false;
            }
        }

        public bool IsExempt(ThingDef def)
            => overrides.TryGetValue(def.defName, out var value) ? value : DefaultExempt(def);

        public void SetExempt(ThingDef def, bool value)
        {
            if (value == DefaultExempt(def)) overrides.Remove(def.defName);
            else overrides[def.defName] = value;
        }

        public void Reset()
        {
            exemptNotAMeal = true;
            exemptPreservedRations = true;
            exemptWhenHungry = true;
            hungerThreshold = HungerCategory.UrgentlyHungry;
            overrides.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref exemptNotAMeal, "exemptNotAMeal", true);
            Scribe_Values.Look(ref exemptPreservedRations, "exemptPreservedRations", true);
            Scribe_Values.Look(ref exemptWhenHungry, "exemptWhenHungry", true);
            Scribe_Values.Look(ref hungerThreshold, "hungerThreshold", HungerCategory.UrgentlyHungry);
            Scribe_Collections.Look(ref overrides, "overrides", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                overrides = overrides ?? new Dictionary<string, bool>();
            }
        }
    }
}
