using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoTableNeeded
{
    public class NoTableNeededMod : Mod
    {
        public const string HarmonyId = "nelim.notableneeded";

        public static NoTableNeededSettings Settings { get; private set; }

        private const float RowHeight = 28f;
        private const float ToolbarHeight = 24f;
        private const float FamilyColumnWidth = 150f;
        private const float ButtonWidth = 120f;

        private Vector2 scrollPosition;
        private string searchText = "";

        public NoTableNeededMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NoTableNeededSettings>();

            // One patch, for the hunger half. The food half needs none.
            new Harmony(HarmonyId).PatchAll();
        }

        public override string SettingsCategory() => "No Table Needed";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            if (!TableEtiquette.HasRun)
            {
                Widgets.Label(inRect, "NoTableNeeded.Settings.NotScanned".Translate());
                return;
            }

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("NoTableNeeded.Settings.Summary".Translate(
                TableEtiquette.ExemptCount(), TableEtiquette.Candidates.Count));
            listing.GapLine();

            var wasNotAMeal = Settings.exemptNotAMeal;
            var wasRations = Settings.exemptPreservedRations;

            listing.CheckboxLabeled(
                "NoTableNeeded.Settings.NotAMeal".Translate(),
                ref Settings.exemptNotAMeal,
                "NoTableNeeded.Settings.NotAMealTip".Translate());

            listing.CheckboxLabeled(
                "NoTableNeeded.Settings.PreservedRations".Translate(),
                ref Settings.exemptPreservedRations,
                "NoTableNeeded.Settings.PreservedRationsTip".Translate(
                    TableEtiquette.RationDaysToRotStart.ToString("0")));

            if (wasNotAMeal != Settings.exemptNotAMeal || wasRations != Settings.exemptPreservedRations)
            {
                TableEtiquette.Apply();
            }

            // Hunger does not go through tableDesired: the patch reads these settings at every
            // meal, so there is nothing to re-apply here.
            listing.GapLine();
            listing.CheckboxLabeled(
                "NoTableNeeded.Settings.WhenHungry".Translate(),
                ref Settings.exemptWhenHungry,
                "NoTableNeeded.Settings.WhenHungryTip".Translate());

            if (Settings.exemptWhenHungry)
            {
                var current = Settings.hungerThreshold == HungerCategory.Starving
                    ? "NoTableNeeded.Hunger.Starving".Translate()
                    : "NoTableNeeded.Hunger.UrgentlyHungry".Translate();

                if (listing.ButtonTextLabeled("NoTableNeeded.Settings.HungerThreshold".Translate(), current,
                        TextAnchor.UpperLeft, null, "NoTableNeeded.Settings.HungerThresholdTip".Translate()))
                {
                    Settings.hungerThreshold = Settings.hungerThreshold == HungerCategory.Starving
                        ? HungerCategory.UrgentlyHungry
                        : HungerCategory.Starving;
                }
            }

            listing.Gap();
            listing.End();

            // Listing_Standard draws inside a GUI group anchored on inRect, so its internal
            // coordinates are relative to that group's origin. Anything drawn after End() is
            // outside the group and must start from inRect again, otherwise the toolbar would
            // ride up by inRect.y (40px, see Dialog_ModSettings) over the checkboxes.
            var y = inRect.y + listing.CurHeight;

            DrawToolbar(new Rect(inRect.x, y, inRect.width, ToolbarHeight));
            y += ToolbarHeight + 10f;

            DrawFoodList(new Rect(inRect.x, y, inRect.width, Mathf.Max(0f, inRect.yMax - y)));
        }

        private void DrawToolbar(Rect rect)
        {
            var searchRect = new Rect(rect.x, rect.y, rect.width - ButtonWidth - 10f, rect.height);
            searchText = Widgets.TextField(searchRect, searchText);
            if (searchText.NullOrEmpty())
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                Widgets.Label(
                    new Rect(searchRect.x + 6f, searchRect.y, searchRect.width - 6f, searchRect.height),
                    "NoTableNeeded.Settings.Search".Translate());
                GUI.color = Color.white;
            }

            var buttonRect = new Rect(rect.xMax - ButtonWidth, rect.y, ButtonWidth, rect.height);
            if (Widgets.ButtonText(buttonRect, "NoTableNeeded.Settings.Reset".Translate()))
            {
                Settings.Reset();
                searchText = "";
                TableEtiquette.Apply();
            }
        }

        private void DrawFoodList(Rect rect)
        {
            var rows = FilteredRows();
            if (rows.Count == 0)
            {
                Widgets.Label(rect, "NoTableNeeded.Settings.NoMatch".Translate());
                return;
            }

            var viewRect = new Rect(0f, 0f, rect.width - 20f, rows.Count * RowHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            var y = 0f;
            foreach (var def in rows)
            {
                // Outside the viewport: skip the draw. The list runs past a hundred rows on the
                // base game alone, generated raw meats included.
                if (y + RowHeight >= scrollPosition.y && y <= scrollPosition.y + rect.height)
                {
                    DrawRow(new Rect(0f, y, viewRect.width, RowHeight), def);
                }
                y += RowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawRow(Rect rect, ThingDef def)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            var checkboxRect = new Rect(rect.x, rect.y, rect.width - FamilyColumnWidth, rect.height);
            var exempt = Settings.IsExempt(def);
            var before = exempt;
            Widgets.CheckboxLabeled(checkboxRect, def.LabelCap, ref exempt);
            if (exempt != before)
            {
                Settings.SetExempt(def, exempt);
                TableEtiquette.Apply();
            }

            var familyRect = new Rect(rect.xMax - FamilyColumnWidth, rect.y, FamilyColumnWidth, rect.height);
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(familyRect, FamilyLabel(def));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, () => RowTip(def), def.shortHash);
        }

        private static string FamilyLabel(ThingDef def)
            => TableEtiquette.FamilyOf(def) == FoodFamily.PreservedRation
                ? "NoTableNeeded.Family.PreservedRation".Translate()
                : "NoTableNeeded.Family.NotAMeal".Translate();

        private static string RowTip(ThingDef def)
            => "NoTableNeeded.Settings.RowTip".Translate(
                def.defName,
                def.modContentPack?.Name ?? "RimWorld",
                def.ingestible.preferability.ToString());

        private List<ThingDef> FilteredRows()
        {
            var all = TableEtiquette.Candidates;
            if (searchText.NullOrEmpty()) return all;

            var needle = searchText.Trim();
            return all.Where(def => Matches(def, needle)).ToList();
        }

        private static bool Matches(ThingDef def, string needle)
        {
            const StringComparison ignoreCase = StringComparison.CurrentCultureIgnoreCase;
            if (!def.label.NullOrEmpty() && def.label.IndexOf(needle, ignoreCase) >= 0) return true;
            return def.defName.IndexOf(needle, ignoreCase) >= 0;
        }
    }
}
