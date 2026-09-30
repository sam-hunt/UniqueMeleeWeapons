using Verse;

namespace UniqueMeleeWeapons;

// A graphic a weapon draws INSTEAD of its own while lying on a map: our analogue of VEF's
// ThingWithFloorGraphic + FloorGraphicExtension, which gives VFE Pirates' warcasket weapons their
// boxed-crate look on the floor. Our uniques must use thingClass UniqueMeleeWeapon (colour-two
// routing, HP clamp, inspect line, bladelink-graft guard), which replaces VEF's class, so the
// override lives in UniqueMeleeWeapon.Graphic and reads this extension. The inherited VEF
// FloorGraphicExtension is left on those defs, inert: only VEF's replaced class reads it.
//
// Ours rather than reflecting VEF's extension, because reflection would save only restating one
// texPath per def:
//  1. The tint still needs a home on our side. VEF's graphicData is untinted, and VEF resolves it
//     with GraphicColoredFor(this), which would tint the crate with the unique's ACCENT colour. So
//     reflection would mean copying VEF's GraphicData and re-tinting it, behind a reflected
//     type/field name that must track an unpublished VEF internal.
//  2. This is plain loader-validated data: a wrong Class= fails at load, not silently.
//  3. It stays generic: any future unique can opt in, VFEP or not.
// Drift trade: if VFEP renames an _OnFloor texture, ours goes missing. UMW_Startup.Run resolves
// every consumer's graphic once at load, so that surfaces as a load-time missing-texture error
// (which the smoke test sees) rather than a pink box the first time one is dropped.
//
// The swap happens in UniqueMeleeWeapon.DrawAt, not in a Graphic override as in VEF. Graphic also
// feeds Widgets.GetIconFor (decompile-verified 1.6: it takes the icon material from thing.Graphic),
// so overriding it shows the crate in the info card, float menu and inventory rows of a grounded
// weapon; with DrawAt the map alone sees the crate and every icon stays the weapon art with both
// mask tints through the mix UI shader, as when held. (A first attempt kept the Graphic override and
// made the crate icon at least keep its tint by drawing it with CutoutComplex, the one shader with
// a UI twin, over a solid-red mask; superseded, and it fell into the atlas trap below.)
//
// The consumer def must be drawerType RealtimeOnly, for two reasons. DrawAt is the realtime path
// (Thing.DynamicDrawPhaseAt -> DrawAt) and Print is not overridden, so a printed consumer would
// print the weapon art. And printing the crate is unusable anyway: Graphic.Print runs the material
// through TryGetTextureAtlasReplacementInfo, and if the texture has a tile in any static atlas the
// print uses that atlas material, colour one moved into the vertex colour and maskTex replaced by
// the atlas's mask atlas (null when the texture was inserted mask-less). VFEP's boxed-weapon
// buildings (VFEP_Box_*, minifiable, so atlased into the Item group too) use these very _OnFloor
// textures as their main graphic, so any mask-bearing shader on the crate sampled a black mask and
// rendered untinted in 2026-09 testing. Realtime drawing goes Graphic.Draw -> MatAt, never touches
// the atlas, and costs one DrawMesh per visible grounded weapon. Nothing else about the weapon
// depends on drawerType: held weapons are drawn by PawnRenderUtility from the graphic directly.
// UMW_Startup.ResolveFloorGraphics errors at load on a consumer that is not RealtimeOnly.
//
// Deliberately not named FloorGraphicExtension: VEF's class of that simple name is also present on
// the same defs, and a shared simple name invites confusion in logs and in duck-typing tools.
//
// Currently attached by the two VFEP warcasket uniques
// (1.6/Mods/VanillaFactionsExpandedPirates/Defs/ThingDefs/). Ordinary ThingDef extension, not a
// trait extension: it has no player-facing effect to describe, so TraitEffectSummary is not
// involved (as CarriedWeaponOffsetExtension).
public class OnFloorGraphicExtension : DefModExtension
{
    // Resolved via GraphicData.Graphic, so the tint is this data's own <color>, fixed in XML.
    public GraphicData graphicData;
}
