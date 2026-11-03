using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended hook 1 of 4: on-hit trait effects on CE's melee verb, the mirror of
// Verb_MeleeAttackDamage_OnHitTraits_Patch. Lifecycle (gate, timing, degraded mode) is owned by
// CombatExtendedCompat, which resolves the target by name after every mod's static ctor.
//
// TARGET: a postfix on Verb_MeleeAttackCE.ApplyMeleeDamageToTarget(LocalTargetInfo), running the
// vanilla body verbatim via MeleeOnHitTraitResolver.
//
// WHY NOT `wounded` ALONE. CE's swing is a LIST of DamageInfos and the method returns the LAST
// entry's DamageResult with only totalDamageDealt summed across entries; on a blunt crit against
// flesh (and every blunt surprise attack, which CE forces to crit) the last entry is CE's appended
// Stun, whose worker deals no hit points, so that result's `wounded` is false even though the blow
// wounded. Reading `wounded` alone left every on-hit trait inert on roughly 40 to 60% of mace and
// warhammer head hits (found 2026-11-03 review). The hook therefore treats the swing as wounding
// when `wounded` OR the summed damage is positive; DamageWorker_AddInjury sets the two together,
// and Stun adds 0, so the sum is the same answer vanilla's flag gives. Consequence worth knowing:
// BellCast now also procs on CE's crits, where its StunFor and CE's crit stun take the longer
// duration, never the sum.
//
// DEGRADED MODE: a miss vetoes every trait carrying MeleeTraitEffectExtension from the rolls.
internal static class Verb_MeleeAttackCE_OnHitTraits_Patch
{
    // Set once at apply time when the target was not found; never cleared.
    internal static bool Missing;

    internal static int TryApply(Type verbType, Harmony harmony)
    {
        var target = AccessTools.Method(verbType, "ApplyMeleeDamageToTarget", new[] { typeof(LocalTargetInfo) });
        if (target == null)
        {
            Log.Warning(CombatExtendedCompat.LogPrefix + CombatExtendedCompat.VerbTypeName + ".ApplyMeleeDamageToTarget was not found; on-hit traits will not roll.");
            Missing = true;
            return 0;
        }
        harmony.Patch(target, postfix: new HarmonyMethod(typeof(Verb_MeleeAttackCE_OnHitTraits_Patch), nameof(Postfix)));
        return 1;
    }

    public static void Postfix(Verb __instance, LocalTargetInfo target, DamageWorker.DamageResult __result)
    {
        // Summed damage, not the last entry's flag: see the header.
        bool wounded = __result != null && (__result.wounded || __result.totalDamageDealt > 0f);
        MeleeOnHitTraitResolver.Fire(__instance, target, wounded);
    }

    internal static bool Vetoes(WeaponTraitDef trait)
    {
        return Missing && trait.HasModExtension<MeleeTraitEffectExtension>();
    }
}
