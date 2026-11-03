using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended hook 3 of 4: the MeleeParryExtension parry chance on CE's melee verb, the
// mirror of Verb_MeleeAttackDamage_Parry_Patch. Lifecycle (gate, timing, degraded mode) is owned
// by CombatExtendedCompat, which resolves the target by name after every mod's static ctor.
//
// TARGET: a postfix on the protected static
// Verb_MeleeAttackCE.GetComparativeChanceAgainst(Pawn, Pawn, StatDef, float, float), the helper
// every parry and riposte computation and the melee tooltip go through, returning
// Clamp01(base + first.stat - second.stat * mult). For the parry calls the FIRST pawn is the one
// being attacked (CE's parameter names are reversed, hence the positional __0). When the stat is
// CE's MeleeParryChance and that pawn's primary carries a MeleeParryExtension trait, the trait's
// chance is added post-curve to the final roll: CE's parry stat is skill-driven and compressed by
// a postProcessCurve, so adding before the curve would shrink a weapon property to a few points.
//
// WHY NOT PORT THE VANILLA PREFIX. Under CE a prefix returning false would leave TryCastShot
// logging a hit with no damage after CE's own parry resolution has already run; CE's parry
// (block, weapon damage, riposte, log line) is the better outcome anyway.
//
// DECIDED: the bonus also applies when a worn shield is CE's parry item, because the chance is
// one number from this helper and the item only decides what absorbs the blow; Quilloned rolls
// on the gladius, a one-hander, and skipping behind a shield would punish the one weapon the
// trait exists for.
//
// THE STAT. MeleeParryChance is re-resolved on every play-data load (ResolveStat, from
// CombatExtendedCompat.Run): a mid-session language change replaces every def instance, and a
// reference cached at first apply would then compare unequal forever (Quilloned silently inert
// after a language switch). Null when CE lacks the stat, which counts as a miss.
//
// DEGRADED MODE: a miss vetoes every trait carrying MeleeParryExtension from the rolls.
internal static class Verb_MeleeAttackCE_Parry_Patch
{
    // Set once at apply time when the target or the stat was not found; never cleared.
    internal static bool Missing;

    private static StatDef parryStat;

    internal static void ResolveStat()
    {
        parryStat = DefDatabase<StatDef>.GetNamedSilentFail("MeleeParryChance");
    }

    internal static int TryApply(Type verbType, Harmony harmony)
    {
        var target = AccessTools.Method(verbType, "GetComparativeChanceAgainst",
            new[] { typeof(Pawn), typeof(Pawn), typeof(StatDef), typeof(float), typeof(float) });
        if (target == null || parryStat == null)
        {
            Log.Warning(CombatExtendedCompat.LogPrefix + CombatExtendedCompat.VerbTypeName + ".GetComparativeChanceAgainst or the MeleeParryChance stat was not found; parry traits will not roll.");
            Missing = true;
            return 0;
        }
        harmony.Patch(target, postfix: new HarmonyMethod(typeof(Verb_MeleeAttackCE_Parry_Patch), nameof(Postfix)));
        return 1;
    }

    // __0 is CE's first parameter: for the parry and tooltip calls, the pawn being attacked.
    public static void Postfix(ref float __result, Pawn __0, StatDef stat)
    {
        if (stat != parryStat || __0 == null)
        {
            return;
        }
        CompUniqueWeapon comp = __0.equipment?.Primary?.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return;
        }
        // Highest chance wins rather than stacking, as in the vanilla parry patch.
        float parryChance = 0f;
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            var ext = traits[i].GetModExtension<MeleeParryExtension>();
            if (ext != null)
            {
                parryChance = Mathf.Max(parryChance, ext.parryChance);
            }
        }
        if (parryChance > 0f)
        {
            __result = Mathf.Clamp01(__result + parryChance);
        }
    }

    internal static bool Vetoes(WeaponTraitDef trait)
    {
        return Missing && trait.HasModExtension<MeleeParryExtension>();
    }
}
