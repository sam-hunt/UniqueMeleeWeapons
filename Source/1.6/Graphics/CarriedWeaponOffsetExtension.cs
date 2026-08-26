using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Per-facing nudge for where a drafted-but-idle pawn holds the weapon (the "carried" pose, not
// aiming). Vanilla has no def hook for this: PawnRenderUtility.DrawCarriedWeapon adds one of four
// hard-coded statics (EqLocNorth (0,0,-0.11), EqLocEast (0.22,0,-0.22), EqLocSouth (0,0,-0.22),
// EqLocWest (-0.22,0,-0.22)) to the pawn's draw position and hands off to DrawEquipmentAiming.
// The one def field in this area, equippedDistanceOffset, is read only by the AIMING branch of
// DrawEquipmentAndApparelExtras and never touches the carried pose. So a sprite whose visual
// centre of mass differs from its texture centre (a long spearhead) reads as gripped in the wrong
// place with no XML remedy - hence this extension plus PawnRenderUtility_DrawCarriedWeapon_Patch,
// which is applied after defs load and only if some ThingDef actually carries this extension
// (so installs with no consumer place no patch on the render path at all).
//
// Vectors are world-space (x = screen right, z = screen up, y unused), in cells, applied AFTER
// vanilla's per-facing static and scaled by the same equipmentDrawDistanceFactor (life-stage
// scaling for children) so the nudge shrinks with the pawn. Unset facings default to zero.
//
// Currently attached only by the Vanilla Textures Expanded compat patch
// (1.6/Mods/VanillaTexturesExpanded/Patches/Spear_Unique_VTE.xml), where the unique spear is
// drawn 1.3x and its longer head makes the default grip sit too close to the tip when facing
// north or south. Ordinary ThingDef extension, not a trait extension: it has no player-facing
// effect to describe, so TraitEffectSummary is not involved.
public class CarriedWeaponOffsetExtension : DefModExtension
{
    public Vector3 north;
    public Vector3 east;
    public Vector3 south;
    public Vector3 west;

    public Vector3 For(Rot4 facing)
    {
        switch (facing.AsInt)
        {
            case Rot4.NorthInt: return north;
            case Rot4.EastInt: return east;
            case Rot4.SouthInt: return south;
            case Rot4.WestInt: return west;
            default: return Vector3.zero;
        }
    }
}
