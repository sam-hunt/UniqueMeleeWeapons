using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Applies unique-weapon traits' MeleeToolModExtension to a swing's per-tool damage and armor
// penetration. VerbProperties.AdjustedMeleeDamageAmount and AdjustedArmorPenetration are the two
// per-tool getters the melee verb calls once per swing (Verb_MeleeAttackDamage.DamageInfosToApply);
// both expose a (Tool, Pawn, Thing equipment, HediffComp_VerbGiver) overload that the public
// (Verb, Pawn) entry delegates to, and the pawn stat cards (StatWorker_MeleeArmorPenetration etc.)
// route through the SAME overload — so one postfix on each reaches both live combat and the
// wielded-weapon stat display. Not under Combat Extended: CE's swing still reaches the damage
// overload, but its cards read neither getter, so hooks 5 and 6 under Patches/CombatExtended/
// lift CE's penetration and damage readouts through the same shared functions.
//
// Cost mirrors the other melee postfixes: non-unique weapons fail the CompUniqueWeapon check and
// bail before allocating. No caching/flush — recomputed each swing from the live trait list.
// Discovered automatically by PatchAll().
//
// ARMOR PENETRATION ORDER: factors first, then the floors, highest floor wins. Two floor kinds
// (see MeleeToolMod): the flat floor is compared as-is against the scaled result; the quality floor
// is first multiplied by the quality factor of MeleeWeapon_DamageMultiplier for the weapon's
// quality, read through an abstract StatRequest (def + quality, no stuff, no thing) so it carries
// the StatPart_Quality transform and nothing else. That is the same multiplier vanilla applies to
// a tool's explicit <armorPenetration>, so a quality floor of 0.9 tracks a monosword's edge
// through every quality tier (0.72 Awful to 1.49 Legendary). One abstract stat evaluation per
// swing on a quality-floored tool, beside the live one vanilla already made for the same stat.
internal static class MeleeToolModResolver
{
    // Gathers every MeleeToolMod off the weapon's traits that targets this tool, or null if the
    // equipment is not a unique weapon or no entry matches.
    internal static List<MeleeToolMod> CollectMods(Tool tool, Thing equipment)
    {
        if (!(equipment is ThingWithComps weapon))
        {
            return null;
        }
        CompUniqueWeapon comp = weapon.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return null;
        }
        List<MeleeToolMod> result = null;
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            var ext = traits[i].GetModExtension<MeleeToolModExtension>();
            if (ext?.mods == null)
            {
                continue;
            }
            for (int j = 0; j < ext.mods.Count; j++)
            {
                if (ext.mods[j].Matches(tool))
                {
                    (result ??= new List<MeleeToolMod>()).Add(ext.mods[j]);
                }
            }
        }
        return result;
    }

    // The product of every matching damageFactor, or 1. Shared by the swing (below) and Combat
    // Extended's damage readout
    // (Patches/CombatExtended/StatWorker_MeleeDamageBaseCE_ToolDamage_Patch), so the card and the
    // hit can never disagree.
    internal static float DamageFactor(Tool tool, Thing equipment)
    {
        List<MeleeToolMod> mods = CollectMods(tool, equipment);
        if (mods == null)
        {
            return 1f;
        }
        float factor = 1f;
        for (int i = 0; i < mods.Count; i++)
        {
            factor *= mods[i].damageFactor;
        }
        return factor;
    }
}

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedMeleeDamageAmount),
    new[] { typeof(Tool), typeof(Pawn), typeof(Thing), typeof(HediffComp_VerbGiver) })]
public static class Verb_MeleeAttack_ToolMods_Damage_Patch
{
    public static void Postfix(ref float __result, Tool tool, Thing equipment)
    {
        __result *= MeleeToolModResolver.DamageFactor(tool, equipment);
    }
}

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedArmorPenetration),
    new[] { typeof(Tool), typeof(Pawn), typeof(Thing), typeof(HediffComp_VerbGiver) })]
public static class Verb_MeleeAttack_ToolMods_ArmorPenetration_Patch
{
    public static void Postfix(ref float __result, Tool tool, Thing equipment)
    {
        List<MeleeToolMod> mods = MeleeToolModResolver.CollectMods(tool, equipment);
        if (mods == null)
        {
            return;
        }
        // Apply all factors first, then raise to the highest floor last (floors win over factors).
        float floor = -1f;
        float qualityFloor = -1f;
        for (int i = 0; i < mods.Count; i++)
        {
            __result *= mods[i].armorPenetrationFactor;
            if (mods[i].armorPenetrationFloor > floor)
            {
                floor = mods[i].armorPenetrationFloor;
            }
            if (mods[i].armorPenetrationQualityFloor > qualityFloor)
            {
                qualityFloor = mods[i].armorPenetrationQualityFloor;
            }
        }
        if (qualityFloor >= 0f)
        {
            floor = Mathf.Max(floor, qualityFloor * QualityFactor(equipment));
        }
        if (__result < floor)
        {
            __result = floor;
        }
    }

    // MeleeWeapon_DamageMultiplier's quality factor for this weapon's quality, and only that: the
    // request names the def and quality but no stuff and no thing, so the stat's StatPart_Quality
    // is the one transform applied to its default base of 1 (no melee weapon def sets a base for
    // it). The VFEP warcasket uniques carry no CompQuality and fall back to Normal (x1).
    private static float QualityFactor(Thing equipment)
    {
        QualityCategory quality = equipment.TryGetQuality(out QualityCategory qc) ? qc : QualityCategory.Normal;
        StatRequest req = StatRequest.For(equipment.def, null, quality);
        return StatDefOf.MeleeWeapon_DamageMultiplier.Worker.GetValue(req);
    }
}
