using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Converts a unique weapon's base melee damage in place, per its traits' MeleeDamageConversionExtension.
//
// Verb_MeleeAttackDamage.DamageInfosToApply builds the DamageInfos a swing will deal
// (the weapon's normal hit comes first, from verbProps.meleeDamageDef). We passthrough-postfix it
// and, for a weapon carrying a conversion trait, rebuild any matching hit with its DamageDef swapped
// — same amount/AP/everything else, just a different wound (e.g. UMW_Serrated reroutes Cut →
// UMW_Cut_Ragged). This modifies the base damage rather than stacking an extra hit (cf. the
// ApplyMeleeDamageToTarget on-hit-effects postfix). A weapon's meleeDamageDef is fixed per
// def, so a trait-gated reroute has to happen here at runtime.
//
// Cost: gated exactly like the on-hit-effects postfix — EquipmentSource is null for every natural
// attack (animals/mechs/fists bail first), and non-unique weapons fail the CompUniqueWeapon check.
// When nothing matches we return the original enumerable unchanged (no wrapper, no allocation); only an
// actual conversion-traited weapon pays for the wrapping iterator. Discovered by PatchAll().
[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply")]
public static class Verb_MeleeAttackDamage_DamageConversion_Patch
{
    public static IEnumerable<DamageInfo> Postfix(IEnumerable<DamageInfo> values, Verb_MeleeAttackDamage __instance)
    {
        ThingWithComps weapon = __instance.EquipmentSource;
        if (weapon == null)
        {
            return values;
        }
        CompUniqueWeapon comp = weapon.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return values;
        }
        List<DamageConversion> conversions = CollectConversions(comp);
        if (conversions == null)
        {
            return values;
        }
        return ApplyConversions(values, conversions);
    }

    // Gathers every conversion off the weapon's traits, or null if none carry one.
    private static List<DamageConversion> CollectConversions(CompUniqueWeapon comp)
    {
        List<DamageConversion> result = null;
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            var ext = traits[i].GetModExtension<MeleeDamageConversionExtension>();
            if (ext?.conversions == null)
            {
                continue;
            }
            (result ??= new List<DamageConversion>()).AddRange(ext.conversions);
        }
        return result;
    }

    private static IEnumerable<DamageInfo> ApplyConversions(IEnumerable<DamageInfo> values, List<DamageConversion> conversions)
    {
        foreach (DamageInfo di in values)
        {
            DamageDef to = MatchTo(di.Def, conversions);
            yield return to == null ? di : Reroute(di, to);
        }
    }

    private static DamageDef MatchTo(DamageDef from, List<DamageConversion> conversions)
    {
        for (int i = 0; i < conversions.Count; i++)
        {
            if (conversions[i].from == from)
            {
                return conversions[i].to;
            }
        }
        return null;
    }

    // Rebuilds src with a new DamageDef. DamageInfo has no
    // SetDef, so we reconstruct, mirroring exactly the fields the melee verb populated.
    private static DamageInfo Reroute(DamageInfo src, DamageDef to)
    {
        var di = new DamageInfo(to, src.Amount, src.ArmorPenetrationInt, src.Angle, src.Instigator,
            src.HitPart, src.Weapon, src.Category, src.IntendedTarget, src.InstigatorGuilty,
            src.SpawnFilth, src.WeaponQuality, src.CheckForJobOverride);
        di.SetBodyRegion(src.Height, src.Depth);
        di.SetWeaponBodyPartGroup(src.WeaponBodyPartGroup);
        di.SetWeaponHediff(src.WeaponLinkedHediff);
        di.SetTool(src.Tool);
        return di;
    }
}
