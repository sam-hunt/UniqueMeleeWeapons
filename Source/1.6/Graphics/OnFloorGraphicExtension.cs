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
// The graphicData must name a shader with a UI twin, in practice CutoutComplex (the Cutout default
// has none), and therefore a maskPath, since such shaders tint through a mask: Textures/Masks/
// UMW_SolidRed is red everywhere, so colour one (= <color>) covers the whole texture exactly as
// Cutout would and the map render is unchanged. Decompile-verified 1.6: Widgets.GetIconFor(Thing)
// takes its material from thing.Graphic, so while the weapon is on the map it sees this graphic,
// but keeps the material only if ShaderDatabase.TryGetUIShader finds a twin (uiLookup pairs
// CutoutComplex with CutoutComplexUI and nothing else in vanilla). Otherwise it draws the bare
// texture under GUI.color = thing.DrawColor, the unique's accent, and the info-card and float-menu
// icons (FloatMenuMakerMap sets iconThing to the clicked thing) of a grounded crate come out in
// that colour instead of the fixed tint. A Harmony postfix on GetIconFor could force the colour
// instead; the shader swap was chosen as pure data. UMW_Startup.ResolveFloorGraphics logs an error
// at load for a twinless shader or a mask that did not resolve (ContentFinder is silent on a
// mistyped maskPath and the shader then samples its default mask), so the smoke test sees both.
//
// The consumer def must also be drawerType RealtimeOnly. Weapons inherit MapMeshOnly from BaseWeapon
// and are printed into the static map mesh, and Graphic.Print (decompile-verified 1.6) runs the
// material through TryGetTextureAtlasReplacementInfo: if the crate texture has a tile in any static
// atlas, the print uses the atlas material instead, with colour one moved into the vertex colour and
// maskTex replaced by that atlas's mask atlas, which is null when the texture was inserted mask-less.
// VFEP's boxed-weapon buildings (VFEP_Box_*) use the very same _OnFloor textures as their main
// graphic, so vanilla atlases them without a mask, and a printed crate would sample CutoutComplex's
// default black mask and render untinted (the failure seen in 2026-09 testing). Realtime drawing
// goes Graphic.Draw -> MatAt, never touches the atlas, and costs one DrawMesh per visible grounded
// weapon. Nothing else about the weapon depends on drawerType: held weapons are drawn by
// PawnRenderUtility from the graphic directly. ResolveFloorGraphics errors on a non-RealtimeOnly
// consumer as well.
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
