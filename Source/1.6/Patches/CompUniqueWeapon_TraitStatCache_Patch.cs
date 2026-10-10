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
// statOffsets/statFactors, so any stat that only weapon traits modify is immutable. Which stats
// those are depends on the whole mod list, not on this mod alone: with no other mods it is
// MaxHitPoints, Flammability and MeleeWeapon_CooldownMultiplier (the other stats our traits name
// carry StatParts and so are never immutable), but Biotech's FireResistant gene already names
// Flammability and so makes it mutable, and any mod's hediff or gene can do the same to any stat.
// That is why the patch clears every stat the trait names rather than a fixed list: a clear on a
// mutable stat is a null-safe no-op. MaxHitPoints is the one that bites at generation:
// Thing.PostMake reads it to set HitPoints BEFORE ThingWithComps.PostMake creates the comps and
// rolls traits, so the trait-less maximum is what every later Thing.MaxHitPoints read returned, for
// the life of the weapon, while the info card (which computes from the StatRequest, not the
// per-thing cache) showed the lowered value. Through 1.4.0-rc.1 Carbonized therefore never cut a
// weapon's hit points at all.
//
// Clear-then-reread is vanilla's own idiom for this: when a thing's stuff changes,
// CompAbilityEffect_Transmute and the dev "set stuff" tool both call
// StatDefOf.MaxHitPoints.Worker.ClearCacheForThing(thing) and then re-read the maximum. VEF does the
// same from its own AddTrait postfix behind its refreshMaxHitPointsStat flag. The alternative,
// marking trait-named stats mutable game-wide (what Unique Weapons Unbound does, because it must
// also handle removal and its preview thing), removes the per-thing cache for EVERY thing in the
// game, not just uniques; a few dictionary removals when a trait lands is the right trade for a mod
// whose trait lists are otherwise fixed at generation.
//
// Why AddTrait: the trait list grows through exactly one vanilla method, and its callers split into
// two timings, generation (InitializeTraits inside CompUniqueWeapon.PostPostMake) and in-world
// (vanilla's "Add trait to unique weapon" dev action, UWU's customization bench, any mod). Clearing
// here, for every stat the trait names, covers both: StatWorker.ClearCacheForThing drops both the
// immutable entry and the 10-tick one, so the next read recomputes with the trait present and the
// inspect pane updates at once even while paused (TicksGame does not advance, so a stale 10-tick
// entry would otherwise never expire). It also reaches a third-party unique that joined via the
// UMW_UniqueMelee tag, which a UniqueMeleeWeapon override would not. Trait REMOVAL has no vanilla
// hook (the dev action edits the list directly), so a removed trait's immutable stats stay stale on
// that weapon; vanilla has the same gap, in-world trait edits are UWU's flow to get right (it makes
// those stats mutable), and it only ever leaves a weapon weaker or tougher than its card says
// until reload. Likewise a forced body colour added in-world does not repaint the weapon, exactly
// as vanilla's own forcedColor does not: AddTrait never calls Notify_ColorChanged. Deliberately not
// handled here.
//
// The clamp is a GENERATION requirement, not an in-world feature: Thing.PostMake has already set
// HitPoints from the trait-less maximum when Carbonized rolls, so without it a fresh weapon reads
// "100 / 80". It runs after the clear so it sees the new maximum, and only clamps downward: a fresh
// weapon's "full" is whatever Thing.PostMake rolled, no trait raises MaxHitPoints, and a repaired
// item clamps the same way (JobDriver_Repair). That it also catches an in-world addition (UWU's
// bench does no hit-point handling of its own) is incidental; vanilla's stuff-change precedents
// rescale proportionally instead, which only differs for a damaged weapon gaining the trait
// in-world, and was judged not worth modelling for a non-vanilla flow. Weapons saved before this
// fix sit above their maximum once the cache is correct; UniqueMeleeWeapon.ExposeData queues a
// clamp for after the load completes (its header has why the read must not happen in ExposeData).
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
