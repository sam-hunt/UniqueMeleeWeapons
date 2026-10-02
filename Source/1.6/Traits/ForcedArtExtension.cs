using Verse;

namespace UniqueMeleeWeapons;

// Guarantees the weapon an active art inscription (CompArt) regardless of quality — for provenance
// traits like UMW_Storied, whose fiction IS a recorded history, on a weapon whose quality would
// normally deny it one (vanilla's bar is CompProperties_Art.minQualityForArtistic, Excellent on
// every weapon def).
//
// Three cases, three behaviours (mechanism split across the two ForcedArt patches and
// ForcedArtUtility):
//   • Rolled at generation: vanilla already delivers — CompUniqueWeapon.PostPostMake rolls quality
//     with QualityGenerator.Super (a Gaussian clamped to [Masterwork, Legendary], decompile-
//     verified), so InitializeArt fires at any legitimate generation, with the Outsider context's
//     tale-less description. Deliberately so: an outsider-made reward must not depict the player
//     colony's deeds. The CanShowArt patch is the backstop should another mod widen that quality
//     roll below Excellent. A quality-less unique (the VFEP warcasket pair) has no SetQuality to
//     ride and no quality to earn an inscription with, so it bears one only when this trait rolls:
//     UniqueMeleeWeapon.PostMake then calls InitializeArt with the same Outsider context itself.
//     CanShowArt is unconditionally true without a CompQuality, so the bar never applies there;
//     the gate is the trait scan in PostMake. (1.4.0-rc.1 inscribed every warcasket unique, trait or
//     no trait, which gave a quality-less weapon what only Excellent-plus quality grants; reverted.)
//   • Trait added to an in-world weapon (UWU's customization bench, dev tools, any mod calling
//     vanilla CompUniqueWeapon.AddTrait): ForcedArtUtility.EnsureArt attaches a colony tale — the
//     colony is literally inscribing its history onto the weapon — preferring a real tale over
//     vanilla's 25% tale-less roll.
//   • Weapon already has art (e.g. UWU def-converts an Excellent+ base weapon whose art transfers
//     with it): untouched. EnsureArt keys off CompArt.Active and never overwrites, so recorded
//     history survives trait addition, trait removal, and def conversion alike.
//
// Marker only, no fields: the quality override is binary and "prefer a tale" has no meaningful
// per-trait tuning. TraitEffectSummary publishes one fixed line for it (UMW_TraitStat_ForcedArt).
public class ForcedArtExtension : DefModExtension
{
}
