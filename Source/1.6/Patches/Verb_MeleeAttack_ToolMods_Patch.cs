using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Applies unique-weapon traits' MeleeToolModExtension to a swing's per-tool damage and armor
// penetration. VerbProperties.AdjustedMeleeDamageAmount and AdjustedArmorPenetration are the two
// per-tool getters the melee verb calls once per swing (Verb_MeleeAttackDamage.DamageInfosToApply);
// both expose a (Tool, Pawn, Thing equipment, HediffComp_VerbGiver) overload that the public
// (Verb, Pawn) entry delegates to, and the pawn stat cards (StatWorker_MeleeArmorPenetration etc.)
// route through the SAME overload — so one postfix on each reaches both live combat and the
// wielded-weapon stat display.
//
// Cost mirrors the other melee postfixes: non-unique weapons fail the CompUniqueWeapon check and
// bail before allocating. No caching/flush — recomputed each swing from the live trait list.
// Discovered automatically by PatchAll().
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
}

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedMeleeDamageAmount),
    new[] { typeof(Tool), typeof(Pawn), typeof(Thing), typeof(HediffComp_VerbGiver) })]
public static class Verb_MeleeAttack_ToolMods_Damage_Patch
{
    public static void Postfix(ref float __result, Tool tool, Thing equipment)
    {
        List<MeleeToolMod> mods = MeleeToolModResolver.CollectMods(tool, equipment);
        if (mods == null)
        {
            return;
        }
        for (int i = 0; i < mods.Count; i++)
        {
            __result *= mods[i].damageFactor;
        }
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
        for (int i = 0; i < mods.Count; i++)
        {
            __result *= mods[i].armorPenetrationFactor;
            if (mods[i].armorPenetrationFloor > floor)
            {
                floor = mods[i].armorPenetrationFloor;
            }
        }
        if (__result < floor)
        {
            __result = floor;
        }
    }
}
