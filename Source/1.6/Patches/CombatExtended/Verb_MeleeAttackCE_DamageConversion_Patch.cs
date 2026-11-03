using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended hook 2 of 4: damage conversion on CE's melee verb, the mirror of
// Verb_MeleeAttackDamage_DamageConversion_Patch. Lifecycle (gate, timing, degraded mode) is owned
// by CombatExtendedCompat, which resolves the target by name after every mod's static ctor.
//
// TARGET: a passthrough postfix on Verb_MeleeAttackCE.DamageInfosToApply(LocalTargetInfo, bool),
// the vanilla body via MeleeDamageConversionResolver. CE's armorPenetration rides
// ArmorPenetrationInt unchanged, so a Sharp-to-Sharp swap keeps the right CE number.
//
// KNOWN LIMIT: CE's own internal-hit roll runs before the rebuild, on the original def, and
// presets HitPart only when it fires; on the common other path UMW_Stab_Deep's higher chance
// still applies through vanilla DamageWorker_Stab.ChooseHitPart (CE does not patch that worker).
//
// DEGRADED MODE: a miss vetoes every trait carrying MeleeDamageConversionExtension from the rolls.
internal static class Verb_MeleeAttackCE_DamageConversion_Patch
{
    // Set once at apply time when the target was not found; never cleared.
    internal static bool Missing;

    internal static int TryApply(Type verbType, Harmony harmony)
    {
        var target = AccessTools.Method(verbType, "DamageInfosToApply", new[] { typeof(LocalTargetInfo), typeof(bool) });
        if (target == null)
        {
            Log.Warning(CombatExtendedCompat.LogPrefix + CombatExtendedCompat.VerbTypeName + ".DamageInfosToApply was not found; damage-conversion traits will not roll.");
            Missing = true;
            return 0;
        }
        harmony.Patch(target, postfix: new HarmonyMethod(typeof(Verb_MeleeAttackCE_DamageConversion_Patch), nameof(Postfix)));
        return 1;
    }

    public static IEnumerable<DamageInfo> Postfix(IEnumerable<DamageInfo> values, Verb __instance)
    {
        return MeleeDamageConversionResolver.Apply(values, __instance.EquipmentSource);
    }

    internal static bool Vetoes(WeaponTraitDef trait)
    {
        return Missing && trait.HasModExtension<MeleeDamageConversionExtension>();
    }
}
