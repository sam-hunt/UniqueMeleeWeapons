using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Compatibility" settings section: opt-outs for the gated compat roots in LoadFolders.xml. The
// whole section is specific to third-party mods, so it early-returns when none of them is active
// (the wholly-DLC-specific pattern from the frame file's header); stored values are never touched,
// so a toggle survives a session without its mod.
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

    // Must equal the IfModActive value on the VanillaTexturesExpanded entry in LoadFolders.xml.
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
        ModMetaData vte = ModLister.GetActiveModWithIdentifier(VtePackageId);
        ThingDef spear = DefDatabase<ThingDef>.GetNamedSilentFail(UniqueSpearDefName);
        if (vte == null || spear == null)
        {
            return;
        }

        SectionHeader(listing, "UMW_SettingsCompat".Translate());

        // Both names are injected, not restated: the spear's from its def label so it follows the
        // active language, the mod's from its About.xml so a rename upstream shows through.
        listing.CheckboxLabeled(
            "UMW_MatchVteSpear".Translate(spear.label, vte.Name),
            ref matchVteSpear,
            "UMW_MatchVteSpearDesc".Translate(spear.label, vte.Name));

        listing.Gap(SectionGap);
    }
}
