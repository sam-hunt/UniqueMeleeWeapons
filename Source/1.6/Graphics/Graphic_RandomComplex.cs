using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// A <see cref="Graphic_Random"/> that preserves colour two.
/// <para>
/// Vanilla <c>Graphic_Random.GetColoredVersion</c> hard-clamps colour two to white (and logs
/// <c>"Cannot use Graphic_Random.GetColoredVersion with a non-white colorTwo"</c>), so a
/// random-variant graphic can only ever be tinted by colour one. Our stuffable unique weapons need
/// <em>both</em> — colour one for the unique accent (red mask) and colour two for the material tint
/// (green mask, supplied by <see cref="UniqueMeleeWeapon.DrawColorTwo"/>). We forward colour two
/// unchanged; that is the whole difference from the base class.
/// </para>
/// <para>
/// Nothing else needs overriding: the underlying <c>Graphic_Single</c> subgraphics are already
/// built with their <c>colorTwo</c> + per-variant <c>_m</c> mask in <c>Graphic_Collection.Init</c>,
/// so propagating the colour through to a fresh instance is sufficient.
/// </para>
/// </summary>
public class Graphic_RandomComplex : Graphic_Random
{
    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        return GraphicDatabase.Get<Graphic_RandomComplex>(path, newShader, drawSize, newColor, newColorTwo, data);
    }
}
