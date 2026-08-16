using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Generation" settings section: what the roll is allowed to hand a unique weapon when one is made.
public partial class UniqueMeleeWeaponsSettings
{
    // Drop Woody stuffs from the random material roll when one of our unique
    // weapons is generated (see Patches/GenStuff_ExcludeWoodStuff_Patch.cs).
    public bool excludeWoodStuff = true;

    // Let the three Royalty-tech traits roll (see Patches/CompUniqueWeapon_UltratechTraits_Patch.cs).
    // Only meaningful — and only shown — with Royalty active, since without it the defs don't exist.
    public bool allowUltratechTraits = true;

    private void ExposeGenerationSettings()
    {
        Scribe_Values.Look(ref excludeWoodStuff, "excludeWoodStuff", true);
        Scribe_Values.Look(ref allowUltratechTraits, "allowUltratechTraits", true);
    }

    private void ResetGenerationSettings()
    {
        excludeWoodStuff = true;
        allowUltratechTraits = true;
    }

    private void DrawGenerationSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsGeneration".Translate());

        listing.CheckboxLabeled(
            "UMW_ExcludeWoodStuff".Translate(ThingDefOf.WoodLog.label),
            ref excludeWoodStuff,
            "UMW_ExcludeWoodStuffDesc".Translate(UMW_DefOf.MeleeWeapon_LongSword.label));

        // Royalty-only row: the three traits it governs are MayRequire-gated on Royalty, so without
        // the DLC there is nothing to toggle and the row would only confuse. Hidden rather than
        // disabled, and the stored value is left alone so it survives a run without Royalty loaded.
        if (ModsConfig.RoyaltyActive)
        {
            // The three MayRequireRoyalty DefOf handles are non-null exactly when this row shows.
            // The label order must match the description's parentheticals (see UMW_UI.xml).
            listing.CheckboxLabeled(
                "UMW_AllowUltratechTraits".Translate(),
                ref allowUltratechTraits,
                "UMW_AllowUltratechTraitsDesc".Translate(
                    UMW_DefOf.UMW_Monomolecular.label,
                    UMW_DefOf.UMW_PlasmaCored.label,
                    UMW_DefOf.UMW_ZeusHeaded.label));
        }

        listing.Gap(SectionGap);
    }
}
