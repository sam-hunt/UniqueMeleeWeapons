using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Forces a unique-weapon trait to override colour two (the green-masked body) of a stuffable
// unique melee weapon — the melee analogue of vanilla WeaponTraitDef.forcedColor, which only
// reaches colour one (the red-masked accent, via CompUniqueWeapon.ForceColor()).
//
// Colour two normally carries the material tint (see UniqueMeleeWeapon.DrawColorTwo).
// There is no third mask channel, so a forced colour two replaces the stuff tint outright: the body
// renders this colour regardless of material. That is the deliberate trade — use it only for traits whose
// identity is a body colour (e.g. blood-caked), and gate the family with its own exclusion token
// (BodyColor) so two body-colour traits can't co-roll. A body-colour trait sits on a different
// channel from the Color-tagged inlays (colour one), so the two can legitimately co-occur.
//
// No persistence or graphic-cache work is needed: colour two is re-derived from the (already-scribed) trait
// list at draw time, and traits are assigned in PostPostMake before the graphic is first built — the
// same timing that already makes colour one and the stuff tint work.
public class ForcedColorTwoExtension : DefModExtension
{
    // The colour the body (colour two) renders, replacing the material tint. Reuses the same
    // weapon ColorDefs as colour one (e.g. UMW_Blood).
    public ColorDef color;
}
