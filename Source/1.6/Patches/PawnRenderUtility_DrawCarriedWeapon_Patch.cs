using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Applies CarriedWeaponOffsetExtension (see its header for why vanilla offers no def hook here).
//
// Prefix rather than postfix because DrawCarriedWeapon computes the final position and issues
// the draw itself, so the only seam is the incoming drawPos; vanilla's own per-facing static is
// added on top inside the method, leaving ours a pure additive nudge. Scaled by the same
// life-stage factor vanilla uses for its static so children keep proportion.
//
// DELIBERATELY NOT [HarmonyPatch]: the one patch in this assembly that PatchAll() must skip.
// Its consumer is the extension, and whether any def carries one is unknowable in the Mod ctor
// (before defs load) but is pure def data afterwards. So UMW_Startup.Run calls
// ApplyIfConsumersExist once the DefDatabase is built: if no ThingDef carries the extension
// (every install without the Vanilla Textures Expanded compat root today), UMW places no patch
// on PawnRenderUtility at all - no per-frame call, no entry in patched-method listings when a
// player is debugging a rendering conflict. A third-party def carrying the extension turns the
// patch on with no code here knowing about them, like the UMW_UniqueMelee tag and the
// stuff_adjective symbol. Gating on VTE's presence would have been the wrong signal for the same
// reason; a setting would be a UI row for something no player can perceive.
//
// Never unpatched: the only in-process reload (a language change) rebuilds the defs identically,
// so the answer can't change without a restart, and Harmony's Unpatch re-JITs the target for
// nothing. Run is idempotent, so a static flag suffices. Deferral here is about the decision,
// not safety - the target is vanilla, so the foreign-cctor timing hazard in CLAUDE.md does not
// apply; do not "tidy" this back into PatchAll.
//
// Cost when applied: one modExtensions scan per drafted-idle, on-screen, openly-armed pawn per
// frame - a list of zero to two entries for every weapon def in practice.
public static class PawnRenderUtility_DrawCarriedWeapon_Patch
{
    private static bool applied;

    public static void ApplyIfConsumersExist()
    {
        if (applied) return;
        if (!DefDatabase<ThingDef>.AllDefsListForReading
                .Any(d => d.HasModExtension<CarriedWeaponOffsetExtension>()))
            return;

        var original = AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawCarriedWeapon));
        var prefix = new HarmonyMethod(typeof(PawnRenderUtility_DrawCarriedWeapon_Patch), nameof(Prefix));
        UniqueMeleeWeaponsMod.Harmony.Patch(original, prefix: prefix);
        applied = true;
        Log.Message("[Unique Melee Weapons] Carried-weapon offset extension in use; patched PawnRenderUtility.DrawCarriedWeapon.");
    }

    public static void Prefix(ThingWithComps weapon, ref Vector3 drawPos, Rot4 facing,
        float equipmentDrawDistanceFactor)
    {
        var ext = weapon?.def?.GetModExtension<CarriedWeaponOffsetExtension>();
        if (ext == null) return;
        drawPos += ext.For(facing) * equipmentDrawDistanceFactor;
    }
}
