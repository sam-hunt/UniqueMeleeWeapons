using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Generation" settings section: what the material roll is allowed to hand a unique weapon when one
// is made. Per-weapon and per-trait opt-outs have sections of their own.
public partial class UniqueMeleeWeaponsSettings
{
    // Drop Woody stuffs from the random material roll when one of our unique
    // weapons is generated (see Patches/GenStuff_ExcludeWoodStuff_Patch.cs).
    public bool excludeWoodStuff = true;

    private void ExposeGenerationSettings()
    {
        Scribe_Values.Look(ref excludeWoodStuff, "excludeWoodStuff", true);
    }

    private void ResetGenerationSettings()
    {
        excludeWoodStuff = true;
    }

    private void DrawGenerationSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsGeneration".Translate());

        listing.CheckboxLabeled(
            "UMW_ExcludeWoodStuff".Translate(ThingDefOf.WoodLog.label),
            ref excludeWoodStuff,
            "UMW_ExcludeWoodStuffDesc".Translate(UMW_DefOf.MeleeWeapon_LongSword.label));

        // The ultratech-traits toggle that lived here through 1.x is now the Royalty group header of
        // the Traits section (Settings_Traits.cs migrates a saved "off").

        EndSection(listing);
    }
}
