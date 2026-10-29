using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Draws each trait's effect lines into vanilla's "Weapon traits" info-card entry — the info card's
// renderer for the TraitEffectLinesExtension that TraitEffectSummary attaches at startup. It derives
// nothing itself; the companion mod's trait-picker tooltip draws the same lines its own way.
//
// CompUniqueWeapon.SpecialDisplayStats builds ONE StatDrawEntry whose hover report is a pre-rendered
// string: per trait, a yellow label, the description, then its statOffsets and statFactors. The entry
// is kept and only its report text replaced, via the public SetReportText — category, label, value
// string and display priority stay vanilla's, so an upstream change to any of those carries through
// untouched. Safe to call here because StatDrawEntry caches the rendered explanation lazily on first
// read, and nothing has read it yet.
//
// The per-trait blocks are rebuilt rather than string-spliced, which means this file mirrors
// vanilla's layout: header, blank line, then each trait's block separated by a blank line. Effect
// lines sit between the prose and the stat lines, so a trait reads as description → what it does →
// what it costs. Vanilla uses no "Effects:" heading and neither do we; the " - " prefix is the
// convention it already established for a trait's own stat lines (ToLineList(" - ")).
//
// Drift after a RimWorld update is cosmetic only. The ranged-only burstShot*/additionalStoppingPower
// lines vanilla also emits are intentionally dropped — they are inert on a melee weapon (1.6/Defs/CLAUDE.md),
// so printing them would claim an effect that never fires.
//
// Discovered automatically by PatchAll().
[HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.SpecialDisplayStats))]
public static class CompUniqueWeapon_TraitStats_Patch
{
    public static void Postfix(CompUniqueWeapon __instance, ref IEnumerable<StatDrawEntry> __result)
    {
        if (__result == null)
        {
            return;
        }
        __result = Rewrite(__instance, __result);
    }

    private static IEnumerable<StatDrawEntry> Rewrite(CompUniqueWeapon comp, IEnumerable<StatDrawEntry> entries)
    {
        List<WeaponTraitDef> traits = comp.TraitsListForReading;
        bool anyLines = false;
        for (int i = 0; i < traits.Count; i++)
        {
            if (EffectLines(traits[i]) != null)
            {
                anyLines = true;
                break;
            }
        }

        foreach (StatDrawEntry entry in entries)
        {
            // Only the trait entry is a label-only entry (stat == null); leave anything else,
            // ours or another mod's, alone.
            if (anyLines && entry != null && entry.stat == null)
            {
                entry.SetReportText(BuildReport(traits));
            }
            yield return entry;
        }
    }

    private static List<string> EffectLines(WeaponTraitDef trait)
    {
        return trait.GetModExtension<TraitEffectLinesExtension>()?.lines;
    }

    // Mirrors CompUniqueWeapon.SpecialDisplayStats' own report layout, with the effect lines added to
    // each trait's block.
    private static string BuildReport(List<WeaponTraitDef> traits)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Stat_ThingUniqueWeaponTrait_Desc".Translate());
        sb.AppendLine();

        for (int i = 0; i < traits.Count; i++)
        {
            WeaponTraitDef trait = traits[i];
            sb.AppendLine(trait.LabelCap.Colorize(ColorLibrary.Yellow));
            sb.AppendLine(trait.description);

            List<string> lines = EffectLines(trait);
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    sb.AppendLine(" - " + line);
                }
            }
            AppendStatModifiers(sb, trait.statOffsets, ToStringNumberSense.Offset, 0f);
            AppendStatModifiers(sb, trait.statFactors, ToStringNumberSense.Factor, 1f);

            if (i < traits.Count - 1)
            {
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    // Vanilla prints every entry in the list; skipping no-op values keeps a trait that carries a
    // neutral modifier from showing a line that says nothing.
    private static void AppendStatModifiers(StringBuilder sb, List<StatModifier> modifiers, ToStringNumberSense sense, float noOp)
    {
        if (modifiers == null)
        {
            return;
        }
        foreach (StatModifier modifier in modifiers)
        {
            if (Mathf.Approximately(modifier.value, noOp))
            {
                continue;
            }
            sb.AppendLine(" - " + modifier.stat.LabelCap + " "
                + modifier.stat.Worker.ValueToString(modifier.value, finalized: false, sense));
        }
    }
}
