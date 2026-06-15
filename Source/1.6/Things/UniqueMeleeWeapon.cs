using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Thing class for <em>stuffable</em> unique melee weapons. It combines two independent
/// recolours in a single texture via the <c>CutoutComplex</c> shader:
/// <list type="bullet">
///   <item><b>Colour one</b> (<see cref="DrawColor"/>, mask <b>red</b>) — the unique accent
///   colour. Supplied unchanged by vanilla <c>CompUniqueWeapon.ForceColor()</c> (a randomly
///   picked weapon <c>ColorDef</c>, or a trait's <c>forcedColor</c> such as gold/jade).</item>
///   <item><b>Colour two</b> (<see cref="DrawColorTwo"/>, mask <b>green</b>) — the stuff/material
///   tint. Vanilla leaves this at the def's <c>colorTwo</c> (white); we redirect it to the stuff
///   colour so the blade tints like any ordinary smithed weapon.</item>
/// </list>
/// The mask drives the two colours per pixel, so one diffuse renders both the material tint and
/// the unique accent at once — the trick the whole mod relies on. The mask must contain <b>no
/// black</b> over the weapon silhouette: black means "not painted" and would show the raw,
/// un-tinted diffuse (a black blade would ignore its material entirely).
/// </summary>
public class UniqueMeleeWeapon : ThingWithComps
{
    /// <summary>
    /// Colour one (the red-masked accent) is left to the base implementation, which returns
    /// the first comp's <c>ForceColor()</c> — i.e. <c>CompUniqueWeapon</c>'s unique colour.
    /// Colour two (the green-masked body) defaults to the stuff/material tint, but a trait may
    /// override it via <see cref="ForcedColorTwoExtension"/> — the colour-two analogue of vanilla's
    /// colour-one <c>forcedColor</c>. A forced body colour <b>replaces</b> the material tint
    /// (there is no third mask channel); first such trait wins.
    /// </summary>
    public override Color DrawColorTwo
    {
        get
        {
            ColorDef forced = ForcedBodyColor();
            if (forced != null)
            {
                return forced.color;
            }
            if (Stuff != null)
            {
                return def.GetColorForStuff(Stuff);
            }
            return base.DrawColorTwo;
        }
    }

    /// <summary>The body (colour two) override from the first equipped trait carrying a
    /// <see cref="ForcedColorTwoExtension"/>, or <c>null</c> for the default material tint.</summary>
    private ColorDef ForcedBodyColor()
    {
        var comp = this.TryGetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return null;
        }
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            ColorDef color = traits[i].GetModExtension<ForcedColorTwoExtension>()?.color;
            if (color != null)
            {
                return color;
            }
        }
        return null;
    }
}
