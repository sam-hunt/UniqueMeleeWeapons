using HarmonyLib;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended hook 6 of 6: MeleeToolMod's per-tool damageFactor on CE's damage readouts, the
// display half of Verb_MeleeAttack_ToolMods_Damage_Patch under CE. Combat needs no CE hook: CE's
// swing calls VerbProperties.AdjustedMeleeDamageAmount(Verb, Pawn), which delegates to the
// overload that postfix sits on. The card does: in vanilla, StatWorker_MeleeAverageDPS reads the
// same overload, but CE's readouts take the damage from Tool.AdjustedBaseMeleeDamageAmount, which
// applies MeleeWeapon_DamageMultiplier and the stuff factor and never reaches our postfix. So
// under CE a Studded mace's card under-read its head by 15% and Serrated's edge/point split showed
// as untraited (found 2026-11-06 auditing the touch-points after hook 5). Lifecycle (gate,
// timing) is owned by CombatExtendedCompat.
//
// TARGET: the public static StatWorker_MeleeDamageBase.GetAdjustedDamage(ToolCE, Thing), the one
// funnel both CE damage workers use: StatWorker_MeleeDamage (CE's per-tool MeleeDamage stat,
// label, breakdown and final value) and StatWorker_MeleeDamageAverage (the workerClass CE swaps
// onto MeleeWeapon_AverageDPS, including its GetValueUnfinalized, so the stat VALUE follows too;
// vanilla reads it only as JobGiver_PickUpOpportunisticWeapon's minimum-DPS gate). Nothing in
// CE's combat path calls it (decompile-verified, CE 16.7.4.0), so the postfix is display-only and
// cannot double-apply the factor a swing already got. Its IL is 67 bytes, past Mono's 20-byte
// inlining limit, so callers cannot bypass the patch. A number-in, number-out postfix: no text is
// rebuilt and no string of ours is shown, unlike hook 5. The factor comes from
// MeleeToolModResolver.DamageFactor, the function the swing uses.
//
// The CE parameter is declared as Verse.Tool here (ToolCE derives from it), so this file holds no
// CE type; positional __0/__1 keep a CE parameter rename harmless.
//
// LOAD ORDER AND DEGRADED MODE: as hook 5. Applied from CombatExtendedCompat.Run after every
// mod's static ctor, resolved by name, a miss logs once and patches nothing (CE's card stays as it
// was, combat still applies the factor), so it vetoes nothing and is not counted by AnyMissing.
internal static class StatWorker_MeleeDamageBaseCE_ToolDamage_Patch
{
    internal const string TypeName = "CombatExtended.StatWorker_MeleeDamageBase";

    // Set once at apply time when the target was not found; never cleared.
    internal static bool Missing;

    internal static int TryApply(Harmony harmony)
    {
        System.Type worker = AccessTools.TypeByName(TypeName);
        System.Type toolCE = AccessTools.TypeByName("CombatExtended.ToolCE");
        var target = worker == null || toolCE == null
            ? null
            : AccessTools.Method(worker, "GetAdjustedDamage", new[] { toolCE, typeof(Thing) });
        if (target == null || !target.IsStatic || target.ReturnType != typeof(float))
        {
            Log.Warning(CombatExtendedCompat.LogPrefix + "its melee damage readout (" + TypeName + ".GetAdjustedDamage) has changed; weapon info cards will not show per-tool trait damage, which still applies in combat.");
            Missing = true;
            return 0;
        }
        harmony.Patch(target, postfix: new HarmonyMethod(typeof(StatWorker_MeleeDamageBaseCE_ToolDamage_Patch), nameof(Postfix)));
        return 1;
    }

    public static void Postfix(ref float __result, Tool __0, Thing __1)
    {
        __result *= MeleeToolModResolver.DamageFactor(__0, __1);
    }
}
