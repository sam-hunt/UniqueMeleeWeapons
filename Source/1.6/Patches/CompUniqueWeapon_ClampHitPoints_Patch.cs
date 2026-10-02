using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Keeps a weapon's hit points at or below its maximum after a trait lowers that maximum
// (UMW_Carbonized's MaxHitPoints x0.8), so a fresh Carbonized weapon starts "80 / 80" rather than
// "100 / 80", and one carbonized in the world drops to its new ceiling the way JobDriver_Repair
// clamps a repaired item.
//
// Why AddTrait and not UniqueMeleeWeapon.PostMake: the trait list grows through exactly one vanilla
// method, and its callers split into two timings. Generation calls it from InitializeTraits inside
// CompUniqueWeapon.PostPostMake, which ThingWithComps.PostMake runs AFTER Thing.PostMake has already
// set HitPoints from the trait-less maximum (decompile-verified 1.6). Anything in-world calls it
// too: vanilla's own "Add trait to unique weapon" dev action, UWU's customization bench, any mod.
// Through 1.4.0-rc.1 the clamp sat in PostMake, so it caught the first timing and missed the second
// entirely, and the second is how a tester most easily reaches a Carbonized weapon (commonality
// 0.5, never alone). Only traits ever lower the maximum, so a clamp here is complete; a clamp in
// PostMake would be a redundant copy of this one.
//
// The stat is read uncached on purpose. Thing.MaxHitPoints reads it with cacheStaleAfterTicks 10,
// and the stat was last computed before this trait existed, so the property would hand back the old
// maximum; worse, a paused game never advances TicksGame, so that stale entry would never expire.
// A -1 read recomputes and overwrites the cache entry, so the inspect pane shows the new maximum at
// once as well. Clamp only downward: a fresh weapon's "full" is whatever Thing.PostMake rolled, and
// no trait raises MaxHitPoints.
[HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.AddTrait))]
public static class CompUniqueWeapon_ClampHitPoints_Patch
{
    public static void Postfix(CompUniqueWeapon __instance)
    {
        ThingWithComps weapon = __instance.parent;
        if (!weapon.def.useHitPoints)
        {
            return;
        }
        int max = Mathf.RoundToInt(weapon.GetStatValue(StatDefOf.MaxHitPoints));
        if (weapon.HitPoints > max)
        {
            weapon.HitPoints = max;
        }
    }
}
