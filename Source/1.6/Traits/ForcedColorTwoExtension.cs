using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Forces a unique-weapon trait to override <b>colour two</b> (the green-masked body) of a stuffable
/// unique melee weapon — the melee analogue of vanilla <c>WeaponTraitDef.forcedColor</c>, which only
/// reaches <b>colour one</b> (the red-masked accent, via <c>CompUniqueWeapon.ForceColor()</c>).
/// <para>
/// Colour two normally carries the <em>material tint</em> (see <see cref="UniqueMeleeWeapon.DrawColorTwo"/>).
/// There is no third mask channel, so a forced colour two <b>replaces</b> the stuff tint outright: the body
/// renders this colour regardless of material. That is the deliberate trade — use it only for traits whose
/// identity is a body colour (e.g. blood-caked), and gate the family with its own exclusion token
/// (<c>BodyColor</c>) so two body-colour traits can't co-roll. A body-colour trait sits on a different
/// channel from the <c>Color</c>-tagged inlays (colour one), so the two <em>can</em> legitimately co-occur.
/// </para>
/// <para>
/// No persistence or graphic-cache work is needed: colour two is re-derived from the (already-scribed) trait
/// list at draw time, and traits are assigned in <c>PostPostMake</c> before the graphic is first built — the
/// same timing that already makes colour one and the stuff tint work.
/// </para>
/// </summary>
public class ForcedColorTwoExtension : DefModExtension
{
    /// <summary>The colour the body (colour two) renders, replacing the material tint. Reuses the same
    /// weapon <c>ColorDef</c>s as colour one (e.g. <c>UMW_Blood</c>).</summary>
    public ColorDef color;
}
