using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Makes a trait's statOffsets/statFactors take effect on the weapon's own cached stats the moment
// the trait lands, and keeps hit points at or below a maximum the trait has just lowered
// (UMW_Carbonized's MaxHitPoints x0.8), so a fresh Carbonized weapon reads "80 / 80".
//
// The cache problem (decompile-verified 1.6): StatDef.SetImmutability marks a stat immutable when
// nothing mutable at runtime can touch it, and StatWorker.GetValue(Thing) then caches that stat PER
// THING FOREVER on first read; no cacheStaleAfterTicks value bypasses that branch. The scan behind
// it, StatDef.PopulateMutableStats, reads WeaponTraitDef.equippedStatOffsets ONLY, never a trait's
// statOffsets/statFactors, so any stat that only weapon traits modify is immutable. For this mod
// that is MaxHitPoints, Flammability and MeleeWeapon_CooldownMultiplier (the others carry StatParts
// and so are never immutable). MaxHitPoints is the one that bites at generation: Thing.PostMake
// reads it to set HitPoints BEFORE ThingWithComps.PostMake creates the comps and rolls traits, so
// the trait-less maximum is what every later Thing.MaxHitPoints read returned, for the life of the
// weapon, while the info card (which computes from the StatRequest, not the per-thing cache) showed
// the lowered value. Through 1.4.0-rc.1 Carbonized therefore never cut a weapon's hit points at all.
// VEF hit the same wall and clears the same cache behind its refreshMaxHitPointsStat flag.
//
// Why AddTrait: the trait list grows through exactly one vanilla method, and its callers split into
// two timings, generation (InitializeTraits inside CompUniqueWeapon.PostPostMake) and in-world
// (vanilla's "Add trait to unique weapon" dev action, UWU's customization bench, any mod). Clearing
// here, for every stat the trait names, covers both: StatWorker.ClearCacheForThing drops both the
// immutable entry and the 10-tick one, so the next read recomputes with the trait present and the
// inspect pane updates at once even while paused (TicksGame does not advance, so a stale 10-tick
// entry would otherwise never expire). Trait REMOVAL has no vanilla hook (the dev action edits the
// list directly), so a removed trait's immutable stats stay stale on that weapon; vanilla has the
// same gap and it only ever leaves a weapon weaker or tougher than its card says until reload.
//
// The clamp runs after the clear so it sees the new maximum, and only clamps downward: a fresh
// weapon's "full" is whatever Thing.PostMake rolled, no trait raises MaxHitPoints, and a repaired
// item clamps the same way (JobDriver_Repair). Weapons saved before this fix sit above their
// maximum once the cache is correct; UniqueMeleeWeapon.ExposeData clamps those on load.
[HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.AddTrait))]
public static class CompUniqueWeapon_TraitStatCache_Patch
{
    public static void Postfix(CompUniqueWeapon __instance, WeaponTraitDef traitDef)
    {
        ThingWithComps weapon = __instance.parent;
        ClearCaches(weapon, traitDef.statOffsets);
        ClearCaches(weapon, traitDef.statFactors);
        if (weapon.def.useHitPoints)
        {
            int max = Mathf.RoundToInt(weapon.GetStatValue(StatDefOf.MaxHitPoints));
            if (weapon.HitPoints > max)
            {
                weapon.HitPoints = max;
            }
        }
    }

    private static void ClearCaches(Thing weapon, List<StatModifier> modifiers)
    {
        if (modifiers == null)
        {
            return;
        }
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i].stat?.Worker.ClearCacheForThing(weapon);
        }
    }
}
