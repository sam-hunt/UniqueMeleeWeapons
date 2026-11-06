using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended hook 5 of 5: the quality-scaled armor-penetration floors on CE's info card, the
// display half of hook 4 (Verb_MeleeAttackCE_PenetrationFloor_Patch). In vanilla the card needs
// nothing, because the stat card and the swing share the AdjustedArmorPenetration overload
// Verb_MeleeAttack_ToolMods_Patch postfixes; CE's card shares nothing with its swing. Lifecycle
// (gate, timing) is owned by CombatExtendedCompat. Added 2026-11-06 after a Workshop report: a CE
// player saw a Monomolecular longsword's card keep the plain longsword's penetration.
//
// TARGET: CombatExtended.StatWorker_MeleeArmorPenetration, the workerClass CE's
// Patches/Core/Stats/Stats.xml swaps onto MeleeWeapon_AverageArmorPenetration (and the worker of
// its own pawn stat MeleeArmorPenetration, which reads the race's tools and never a weapon's, so
// it falls through the unique-weapon check below). Decompile-verified against CE 16.7.4.0. It
// computes everything from the raw ToolCE fields and never calls the verb's getters hook 4
// postfixes. Two postfixes:
//   - private GetFinalDisplayValue(StatRequest): the stat-row label AND the "final value" line
//     (both public callers, GetStatDrawEntryLabel and GetExplanationFinalizePart, go through
//     it). It returns one formatted "X mm RHA, Y MPa" string of the chanceFactor-weighted
//     average over tools, with no seam to adjust a number before formatting, so the postfix
//     recomputes that average with each tool lifted to its floor and re-formats it with CE's own
//     unit keys (CE_mmRHA, CE_MPa) and number style. Reusing a foreign Keyed key is deliberate
//     here, unlike TraitEffectSummary's own-key rule: this is CE's readout, it must read exactly
//     like CE's, and it only exists while CE is loaded. Patched rather than its two callers
//     because it is the single funnel; it is a few hundred bytes of IL with loops and closures,
//     far past what Mono's JIT inlines, so the patch cannot be bypassed by inlining.
//   - public GetExplanationUnfinalized(StatRequest, ToStringNumberSense): the per-tool breakdown
//     ("Sharp penetration: 0.72 x 1.35 = 0.97 mm RHA"). CE's lines are one StringBuilder of
//     translated text, so instead of rewriting them (parsing localized output) the postfix APPENDS
//     one line per floored tool and capacity (UMW_CETooltip_FloorSharp/Blunt), leaving CE's text
//     byte-identical. Above it the card's final value already carries the lifted average.
// Both rewrite only when some tool's floor actually beats CE's own number; otherwise the result
// is left exactly as CE built it, which is every weapon that is not one of ours with a floor.
//
// THE NUMBERS. CE's per-tool value is raw x MeleePenetrationFactor x skill x "other factors";
// those three come from CE's own private helpers (GetPenetrationFactor, GetSkillFactor,
// GetOtherFactors) invoked by reflection, never re-derived here, so a CE retune flows through.
// The floor comes from Verb_MeleeAttackCE_PenetrationFloor_Patch.FloorFor, the function the swing
// uses. The card's "other factors" (life stage and MeleeDamageFactor, ^0.75) are CE's display
// approximation of the swing's GetDamageFactorFor; that gap is CE's, and it already exists in
// every CE card. Reflection per call is fine: the card computes once on open.
//
// LOAD ORDER AND OTHER MODS. Applied from CombatExtendedCompat.Run, after every mod's static
// ctor (the BetterTradersGuild timing rule, Source/1.6/CLAUDE.md); the worker has no cctor of its
// own, but its helpers touch CE_StatDefOf. Every member is resolved by name; any miss logs once
// and applies nothing (CE's card stays as it was). A postfix composes with any other mod's patch
// on the same methods: it sees their result, and replaces it only when a floor wins. A throw inside
// a postfix is caught, logged once, and the incoming result is returned untouched.
//
// DEGRADED MODE: none. A miss here costs only the readout, combat still floors, so unlike hooks
// 1 to 4 this one vetoes nothing and is not counted by CombatExtendedCompat.AnyMissing. The
// reverse dependency is enforced: while hook 4 is Missing the floor is not applied in combat, so
// this hook shows nothing either rather than advertise a number the swing will not reach.
internal static class StatWorker_MeleeArmorPenetrationCE_Floor_Patch
{
    internal const string WorkerTypeName = "CombatExtended.StatWorker_MeleeArmorPenetration";
    private const string ToolTypeName = "CombatExtended.ToolCE";

    // Set once at apply time when any member was not found; never cleared.
    internal static bool Missing;

    private static MethodInfo penetrationFactorMethod;
    private static MethodInfo skillFactorMethod;
    private static MethodInfo otherFactorsMethod;
    private static Type toolCEType;
    private static FieldInfo sharpField;
    private static FieldInfo bluntField;
    private static bool loggedFailure;

    internal static int TryApply(Harmony harmony)
    {
        Type worker = AccessTools.TypeByName(WorkerTypeName);
        toolCEType = AccessTools.TypeByName(ToolTypeName);
        if (worker == null || toolCEType == null)
        {
            return Miss();
        }
        MethodInfo finalValue = AccessTools.Method(worker, "GetFinalDisplayValue", new[] { typeof(StatRequest) });
        MethodInfo explanation = AccessTools.Method(worker, "GetExplanationUnfinalized", new[] { typeof(StatRequest), typeof(ToStringNumberSense) });
        penetrationFactorMethod = AccessTools.Method(worker, "GetPenetrationFactor", new[] { typeof(StatRequest) });
        skillFactorMethod = AccessTools.Method(worker, "GetSkillFactor", new[] { typeof(StatRequest) });
        otherFactorsMethod = AccessTools.Method(worker, "GetOtherFactors", new[] { typeof(StatRequest) });
        sharpField = AccessTools.Field(toolCEType, "armorPenetrationSharp");
        bluntField = AccessTools.Field(toolCEType, "armorPenetrationBlunt");
        if (finalValue?.ReturnType != typeof(string)
            || explanation?.ReturnType != typeof(string)
            || penetrationFactorMethod?.ReturnType != typeof(float)
            || skillFactorMethod?.ReturnType != typeof(float)
            || otherFactorsMethod == null || !typeof(IEnumerable<float>).IsAssignableFrom(otherFactorsMethod.ReturnType)
            || sharpField?.FieldType != typeof(float)
            || bluntField?.FieldType != typeof(float))
        {
            return Miss();
        }
        harmony.Patch(finalValue, postfix: new HarmonyMethod(typeof(StatWorker_MeleeArmorPenetrationCE_Floor_Patch), nameof(FinalValuePostfix)));
        harmony.Patch(explanation, postfix: new HarmonyMethod(typeof(StatWorker_MeleeArmorPenetrationCE_Floor_Patch), nameof(ExplanationPostfix)));
        return 2;
    }

    private static int Miss()
    {
        Log.Warning(CombatExtendedCompat.LogPrefix + "its melee armor penetration readout (" + WorkerTypeName + ") has changed; weapon info cards will not show trait penetration minimums, which still apply in combat.");
        Missing = true;
        return 0;
    }

    // __0 is CE's StatRequest parameter (positional, so a parameter rename in CE is harmless).
    public static void FinalValuePostfix(ref string __result, object __instance, StatRequest __0)
    {
        try
        {
            List<ToolReading> readings = Read(__instance, __0);
            if (readings == null)
            {
                return;
            }
            float totalChance = 0f;
            for (int i = 0; i < readings.Count; i++)
            {
                totalChance += readings[i].tool.chanceFactor;
            }
            if (totalChance <= 0f)
            {
                return;
            }
            float sharp = 0f;
            float blunt = 0f;
            for (int i = 0; i < readings.Count; i++)
            {
                float weight = readings[i].tool.chanceFactor / totalChance;
                sharp += weight * readings[i].SharpShown;
                blunt += weight * readings[i].BluntShown;
            }
            __result = sharp.ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_mmRHA".Translate() + ", "
                + blunt.ToStringByStyle(ToStringStyle.FloatMaxTwo) + " " + "CE_MPa".Translate();
        }
        catch (Exception e)
        {
            LogFailureOnce(e);
        }
    }

    public static void ExplanationPostfix(ref string __result, object __instance, StatRequest __0)
    {
        try
        {
            List<ToolReading> readings = Read(__instance, __0);
            if (readings == null)
            {
                return;
            }
            var sb = new StringBuilder(__result);
            if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
            {
                sb.AppendLine();
            }
            for (int i = 0; i < readings.Count; i++)
            {
                ToolReading r = readings[i];
                if (r.sharpFloor > r.sharp)
                {
                    sb.AppendLine("UMW_CETooltip_FloorSharp".Translate(r.tool.LabelCap, r.sharpFloor.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                }
                if (r.bluntFloor > r.blunt)
                {
                    sb.AppendLine("UMW_CETooltip_FloorBlunt".Translate(r.tool.LabelCap, r.bluntFloor.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                }
            }
            __result = sb.ToString();
        }
        catch (Exception e)
        {
            LogFailureOnce(e);
        }
    }

    private struct ToolReading
    {
        public Tool tool;
        public float sharp;      // CE's own value for this tool
        public float blunt;
        public float sharpFloor; // quality-scaled floor in CE units, or -1
        public float bluntFloor;

        public float SharpShown => Mathf.Max(sharp, sharpFloor);
        public float BluntShown => Mathf.Max(blunt, bluntFloor);
    }

    // Every tool's CE value and floor, or null when no floor beats CE's number (leave CE's text).
    private static List<ToolReading> Read(object worker, StatRequest req)
    {
        if (Verb_MeleeAttackCE_PenetrationFloor_Patch.Missing)
        {
            return null;
        }
        Thing weapon = req.Thing;
        if (weapon == null || weapon is Pawn || weapon.TryGetComp<CompUniqueWeapon>() == null)
        {
            return null;
        }
        List<Tool> tools = weapon.def.tools;
        if (tools.NullOrEmpty())
        {
            return null;
        }
        float factor = (float)penetrationFactorMethod.Invoke(worker, new object[] { req })
            * (float)skillFactorMethod.Invoke(worker, new object[] { req });
        foreach (float other in (IEnumerable<float>)otherFactorsMethod.Invoke(worker, new object[] { req }))
        {
            factor *= other;
        }
        var readings = new List<ToolReading>(tools.Count);
        bool anyLifted = false;
        for (int i = 0; i < tools.Count; i++)
        {
            Tool tool = tools[i];
            if (!toolCEType.IsInstanceOfType(tool))
            {
                return null; // CE logs its own error for a non-CE tool; leave its result alone
            }
            var r = new ToolReading
            {
                tool = tool,
                sharp = (float)sharpField.GetValue(tool) * factor,
                blunt = (float)bluntField.GetValue(tool) * factor,
                sharpFloor = Verb_MeleeAttackCE_PenetrationFloor_Patch.FloorFor(tool, weapon, sharp: true),
                bluntFloor = Verb_MeleeAttackCE_PenetrationFloor_Patch.FloorFor(tool, weapon, sharp: false),
            };
            anyLifted |= r.sharpFloor > r.sharp || r.bluntFloor > r.blunt;
            readings.Add(r);
        }
        return anyLifted ? readings : null;
    }

    private static void LogFailureOnce(Exception e)
    {
        if (loggedFailure)
        {
            return;
        }
        loggedFailure = true;
        Log.Warning("[Unique Melee Weapons] Could not add trait penetration minimums to a Combat Extended info card; showing Combat Extended's own numbers. " + e);
    }
}
