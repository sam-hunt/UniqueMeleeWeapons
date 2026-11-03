using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Compatibility" settings section: opt-outs for the gated compat roots in LoadFolders.xml.
//
// The whole section HIDES when none of its mods is active, which is the sanctioned exception to the
// family rule that a gated row stays visible (RimworldModTemplate, Source/<version>/Settings/CLAUDE.md,
// rule 1): that rule exists so a player can discover content an integration would add, and a row
// here only tunes a correction to the other mod's own content. A player without VTE learns nothing
// they could want from a greyed "match the spear to VTE" row. Stored values are never touched, so a
// toggle survives a session without its mod. Content-offering gated rows (the Royalty and VFE
// Pirates groups in the Weapons and Traits sections) do stay visible.
//
// One row today: matchVteSpear, default ON, governing the Vanilla Textures Expanded spear compat
// (1.6/Mods/VanillaTexturesExpanded/Patches/Spear_Unique_VTE.xml, header there). It is read at XML
// patch time through PatchOperation_UMWSetting, so a change takes effect on the next load and the
// row says so; there is no Apply* def-write here on purpose (re-resolving a def's graphic for a live
// drawSize change is not worth the machinery for a cosmetic toggle).
//
// Why a toggle at all, given the compat is on only when VTE is: texture packs stack, and whichever
// loads LAST wins Core's Spear.png. In a large pack, a retexture after VTE can put the vanilla
// spear back to horizontal, at which point a compat keyed on VTE's presence is actively wrong and
// the player has no other way to correct it. Off also means the def carries no
// CarriedWeaponOffsetExtension, so PawnRenderUtility_DrawCarriedWeapon_Patch never applies. It is
// deliberately NOT framed as a performance option: the difference is unmeasurable, and the row
// must not suggest otherwise.
public partial class UniqueMeleeWeaponsSettings
{
    public bool matchVteSpear = MatchVteSpearDefault;
    private const bool MatchVteSpearDefault = true;

    // Must equal the IfModActive value on the VanillaTexturesExpanded entry in LoadFolders.xml, and
    // the row below must gate on the same suffix-ignoring test IfModActive uses (ignorePostfix: true,
    // via ModIntegrations.VanillaTexturesExpanded), or a Workshop copy installed beside a local one
    // hides the row while the root still loads (1.6/Mods/CLAUDE.md).
    public const string VtePackageId = "VanillaExpanded.VTEXE";
    private const string UniqueSpearDefName = "UMW_Spear_Unique";

    private void ExposeCompatSettings()
    {
        Scribe_Values.Look(ref matchVteSpear, "matchVteSpear", MatchVteSpearDefault);
    }

    private void ResetCompatSettings()
    {
        matchVteSpear = MatchVteSpearDefault;
    }

    private void DrawCompatSection(Listing_Standard listing)
    {
        ThingDef spear = DefDatabase<ThingDef>.GetNamedSilentFail(UniqueSpearDefName);
        if (!ModIntegrations.VanillaTexturesExpanded.IsActive || spear == null)
        {
            return;
        }

        SectionHeader(listing, "UMW_SettingsCompat".Translate());

        // Both names are injected, not restated: the spear's from its def label so it follows the
        // active language, the mod's from its About.xml (via ModIntegrations, which also colours it) so
        // a rename upstream shows through. Resolve() keeps the colour tag; the implicit conversion to
        // string would strip it (ModIntegration.ColoredName).
        string vteName = ModIntegrations.VanillaTexturesExpanded.ColoredName;
        ToggleRow(listing,
            "UMW_MatchVteSpear".Translate(spear.label, vteName).Resolve(),
            "UMW_MatchVteSpearDesc".Translate(spear.label, vteName).Resolve(),
            ref matchVteSpear);

        EndSection(listing);
    }
}
