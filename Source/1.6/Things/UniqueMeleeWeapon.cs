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
    /// We only reroute colour two to the material so it lands on the green-masked body.
    /// </summary>
    public override Color DrawColorTwo
    {
        get
        {
            if (Stuff != null)
            {
                return def.GetColorForStuff(Stuff);
            }
            return base.DrawColorTwo;
        }
    }
}
