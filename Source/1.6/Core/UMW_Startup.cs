using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Startup work that must run against the CURRENT DefDatabase: the def cache and the
// settings-driven def-field writes. Runs once per play-data LOAD, not once per process —
// deliberately NOT [StaticConstructorOnStartup], whose once-per-process contract is too weak
// for def-mutating work: an in-process reload (a mid-session language change) replaces every
// def instance and a type initializer never re-runs. Invoked instead by
// Patches/StaticConstructorOnStartupUtility_CallAll_Patch.cs at exactly the moment static
// ctors run — after defs, DefOf rebinding and full language injection — on every load; that
// file carries the verified load ordering, the DoPlayLoad trap, and the hot-reload caveat.
//
// Everything called here must stay idempotent (reloads and re-patching make it fire more than
// once per process).
public static class UMW_Startup
{
    public static void Run()
    {
        // Rebuild the set of weapons we own (per-weapon settings rows, pool filtering, the
        // warband quest gate) from the fresh DefDatabase, so the cached instances — and the
        // rows' label sort order — follow the active language.
        UniqueWeaponDefs.Rebuild();
        UniqueMeleeWeaponsMod.Settings.ApplyWarbandQuestWeight();
        UniqueMeleeWeaponsMod.Settings.ApplyAbilityTuning();
        // After Rebuild: the trader def-writes iterate UniqueWeaponDefs.All.
        UniqueMeleeWeaponsMod.Settings.ApplyTraderStock();
        TraitEffectSummary.AttachToTraits();
        ResolveFloorGraphics();
        // The one deferred Harmony patch: applied only if some ThingDef carries
        // CarriedWeaponOffsetExtension, which is knowable only now. Idempotent, never unpatched.
        Patches.PawnRenderUtility_DrawCarriedWeapon_Patch.ApplyIfConsumersExist();
    }

    // Builds each OnFloorGraphicExtension graphic now rather than the first time one of the weapons is
    // dropped, so a texture the extension borrows from another mod (VFEP's _OnFloor crates) that has
    // gone missing logs at load, where the smoke test sees it, instead of as a pink box mid-game; and
    // checks the icon contract from that extension's header (a UI-twinned shader whose mask resolved),
    // which otherwise fails silently as a wrongly tinted or untinted icon.
    // Idempotent: GraphicData caches its graphic, and a reload brings fresh GraphicData instances.
    private static void ResolveFloorGraphics()
    {
        foreach (ThingDef def in UniqueWeaponDefs.All)
        {
            GraphicData data = def.GetModExtension<OnFloorGraphicExtension>()?.graphicData;
            Material mat = data?.Graphic?.MatSingle;
            if (mat == null)
            {
                continue;
            }
            if (!ShaderDatabase.TryGetUIShader(mat.shader, out _))
            {
                Log.Error($"[Unique Melee Weapons] {def.defName}: floor graphic shader {mat.shader.name} has no UI " +
                          "twin, so its info-card and float-menu icons will take the weapon's accent colour instead " +
                          "of the graphic's own tint. Use CutoutComplex (see OnFloorGraphicExtension).");
            }
            else if (mat.shader.SupportsMaskTex() && mat.GetTexture(ShaderPropertyIDs.MaskTex) == null)
            {
                Log.Error($"[Unique Melee Weapons] {def.defName}: floor graphic mask '{data.maskPath}' was not found, " +
                          "so the crate tints through the shader's default mask instead.");
            }
        }
    }
}
