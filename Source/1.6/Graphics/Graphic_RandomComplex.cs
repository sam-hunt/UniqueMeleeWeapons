using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// A Graphic_Random that preserves colour two.
//
// Vanilla Graphic_Random.GetColoredVersion hard-clamps colour two to white (and logs
// "Cannot use Graphic_Random.GetColoredVersion with a non-white colorTwo"), so a
// random-variant graphic can only ever be tinted by colour one. Our stuffable unique weapons need
// both — colour one for the unique accent (red mask) and colour two for the material tint
// (green mask, supplied by UniqueMeleeWeapon.DrawColorTwo). We forward colour two
// unchanged; that is the whole difference from the base class.
//
// Nothing else needs overriding: the underlying Graphic_Single subgraphics are already
// built with their colorTwo + per-variant _m mask in Graphic_Collection.Init,
// so propagating the colour through to a fresh instance is sufficient.
public class Graphic_RandomComplex : Graphic_Random
{
    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        return GraphicDatabase.Get<Graphic_RandomComplex>(path, newShader, drawSize, newColor, newColorTwo, data);
    }
}
