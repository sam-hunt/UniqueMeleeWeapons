using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended hook 4 of 4: the quality-scaled armor-penetration floors of MeleeToolMod on
// CE's melee verb, the CE half of Verb_MeleeAttack_ToolMods_Patch (its damage half needs no CE
// hook: MeleeWeapon_DamageMultiplier rides AdjustedMeleeDamageAmount, which CE still calls).
// Lifecycle (gate, timing, degraded mode) is owned by CombatExtendedCompat, which resolves the
// targets by name after every mod's static ctor.
//
// TARGETS: postfixes on the Verb_MeleeAttackCE.ArmorPenetrationSharp and ArmorPenetrationBlunt
// getters. Each floor (MeleeToolMod.ceArmorPenetration*QualityFloor) is compared against CE's
// fully-scaled value (tool field x part/life-stage/damage-factor^0.75 x skill x
// MeleePenetrationFactor) after being multiplied by the quality factor of MeleePenetrationFactor
// read through an abstract StatRequest (def + quality; no stuff, no thing), exactly as the vanilla
// floor uses MeleeWeapon_DamageMultiplier's. The getters feed the hit, the Sharp crit doubling
// (applied after the getter) and the Sharp-deflected-into-Blunt path (LastAttackVerb), so two
// patches cover every read. CE's info card reads ToolCE fields directly and never shows the
// floor; our trait block does (TraitEffectSummary's CE-unit line).
//
// THE STAT. MeleePenetrationFactor is re-resolved on every play-data load (ResolveStat, from
// CombatExtendedCompat.Run): a mid-session language change replaces every def instance, and a
// reference cached at first apply would go stale. Null when CE lacks the stat, which counts as a
// miss.
//
// DEGRADED MODE: a miss vetoes every trait whose MeleeToolModExtension carries a CE floor.
internal static class Verb_MeleeAttackCE_PenetrationFloor_Patch
{
    // Set once at apply time when either getter or the stat was not found; never cleared.
    internal static bool Missing;

    private static StatDef penetrationFactorStat;

    internal static void ResolveStat()
    {
        penetrationFactorStat = DefDatabase<StatDef>.GetNamedSilentFail("MeleePenetrationFactor");
    }

    internal static int TryApply(Type verbType, Harmony harmony)
    {
        var sharpGetter = AccessTools.PropertyGetter(verbType, "ArmorPenetrationSharp");
        var bluntGetter = AccessTools.PropertyGetter(verbType, "ArmorPenetrationBlunt");
        if (sharpGetter == null || bluntGetter == null || penetrationFactorStat == null)
        {
            Log.Warning(CombatExtendedCompat.LogPrefix + CombatExtendedCompat.VerbTypeName + ".ArmorPenetrationSharp/Blunt or the MeleePenetrationFactor stat was not found; penetration-floor traits will not roll.");
            Missing = true;
            return 0;
        }
        harmony.Patch(sharpGetter, postfix: new HarmonyMethod(typeof(Verb_MeleeAttackCE_PenetrationFloor_Patch), nameof(SharpPostfix)));
        harmony.Patch(bluntGetter, postfix: new HarmonyMethod(typeof(Verb_MeleeAttackCE_PenetrationFloor_Patch), nameof(BluntPostfix)));
        return 2;
    }

    public static void SharpPostfix(ref float __result, Verb __instance)
    {
        ApplyFloor(ref __result, __instance, sharp: true);
    }

    public static void BluntPostfix(ref float __result, Verb __instance)
    {
        ApplyFloor(ref __result, __instance, sharp: false);
    }

    private static void ApplyFloor(ref float result, Verb verb, bool sharp)
    {
        ThingWithComps weapon = verb.EquipmentSource;
        if (weapon == null)
        {
            return;
        }
        List<MeleeToolMod> mods = MeleeToolModResolver.CollectMods(verb.tool, weapon);
        if (mods == null)
        {
            return;
        }
        float floor = -1f;
        for (int i = 0; i < mods.Count; i++)
        {
            float candidate = sharp ? mods[i].ceArmorPenetrationSharpQualityFloor : mods[i].ceArmorPenetrationBluntQualityFloor;
            if (candidate > floor)
            {
                floor = candidate;
            }
        }
        if (floor < 0f)
        {
            return;
        }
        floor *= QualityFactor(weapon);
        if (result < floor)
        {
            result = floor;
        }
    }

    // MeleePenetrationFactor's quality factor for this weapon's quality, and only that: def and
    // quality, no stuff and no thing, so StatPart_Quality is the one transform on the default base
    // of 1 (its StatPart_Health is a no-op without a thing, and its worker only overrides
    // IsDisabledFor). A weapon without CompQuality (the VFEP warcasket uniques) counts as Normal.
    private static float QualityFactor(Thing weapon)
    {
        QualityCategory quality = weapon.TryGetQuality(out QualityCategory qc) ? qc : QualityCategory.Normal;
        return penetrationFactorStat.Worker.GetValue(StatRequest.For(weapon.def, null, quality));
    }

    internal static bool Vetoes(WeaponTraitDef trait)
    {
        if (!Missing)
        {
            return false;
        }
        List<MeleeToolMod> mods = trait.GetModExtension<MeleeToolModExtension>()?.mods;
        if (mods == null)
        {
            return false;
        }
        for (int i = 0; i < mods.Count; i++)
        {
            if (mods[i].ceArmorPenetrationSharpQualityFloor >= 0f || mods[i].ceArmorPenetrationBluntQualityFloor >= 0f)
            {
                return true;
            }
        }
        return false;
    }
}
