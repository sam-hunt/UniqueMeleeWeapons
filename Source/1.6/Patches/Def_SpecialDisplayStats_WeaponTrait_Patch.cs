using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Gives a WeaponTraitDef an info card worth opening. Vanilla never opens one (a weapon's card folds
// its traits into one "Traits" entry, CompUniqueWeapon_TraitStats_Patch), so WeaponTraitDef does not
// override Def.SpecialDisplayStats and Dialog_InfoCard(traitDef) shows the description and nothing
// else. The settings window's trait rows carry an (i) that opens exactly that card
// (Core/Settings/Settings_Traits.cs), so this postfix on the base Def.SpecialDisplayStats (which a
// WeaponTraitDef still dispatches to, having no override of its own) adds, under the Weapon
// category: one row per non-neutral stat offset or factor, labelled with the stat and hoverable
// for the stat's own description, then one row per effect line from the trait's
// TraitEffectLinesExtension (the same lines the weapon card and the settings tooltip show, derived
// by TraitEffectSummary, never authored here). Every WeaponTraitDef gets this, not only ours: the
// rows come from vanilla fields and an extension that is simply absent on a trait without one, and
// a third-party melee trait in our pools then reads the same way.
//
// Rows are label-only StatDrawEntries (no StatDef behind them) on purpose: an entry backed by the
// StatDef would ask its worker to explain the value against an empty StatRequest, which the
// weapon-stat workers do not all survive. Vanilla's ranged-only fields (burst, stopping power) are
// left out, as the weapon card's rewrite leaves them out: inert on melee (1.6/Defs/CLAUDE.md).
//
// Discovered automatically by PatchAll().
[HarmonyPatch(typeof(Def), nameof(Def.SpecialDisplayStats))]
public static class Def_SpecialDisplayStats_WeaponTrait_Patch
{
    // Below vanilla's own Weapon-category entries (damage, cooldown, ...), which sit in the
    // thousands and above; the trait's rows run down from here in list order.
    private const int DisplayPriority = 900;

    public static void Postfix(Def __instance, ref IEnumerable<StatDrawEntry> __result)
    {
        if (__instance is WeaponTraitDef trait)
        {
            __result = Append(__result, trait);
        }
    }

    private static IEnumerable<StatDrawEntry> Append(IEnumerable<StatDrawEntry> entries, WeaponTraitDef trait)
    {
        if (entries != null)
        {
            foreach (StatDrawEntry entry in entries)
            {
                yield return entry;
            }
        }

        List<StatDrawEntry> rows = new List<StatDrawEntry>();
        AddModifierRows(rows, trait.statOffsets, ToStringNumberSense.Offset, 0f);
        AddModifierRows(rows, trait.statFactors, ToStringNumberSense.Factor, 1f);
        List<string> lines = trait.GetModExtension<TraitEffectLinesExtension>()?.lines;
        if (lines != null)
        {
            string effectLabel = "UMW_TraitStat_Effect".Translate();
            foreach (string line in lines)
            {
                rows.Add(new StatDrawEntry(StatCategoryDefOf.Weapon, effectLabel, line, line, DisplayPriority - rows.Count));
            }
        }
        foreach (StatDrawEntry row in rows)
        {
            yield return row;
        }
    }

    // Same no-op skip as the weapon card's rewrite: a neutral modifier says nothing.
    private static void AddModifierRows(List<StatDrawEntry> rows, List<StatModifier> modifiers,
        ToStringNumberSense sense, float noOp)
    {
        if (modifiers == null)
        {
            return;
        }
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.stat == null || Mathf.Approximately(modifier.value, noOp))
            {
                continue;
            }
            string value = modifier.stat.Worker.ValueToString(modifier.value, finalized: false, sense);
            string report = modifier.stat.LabelCap.Colorize(ColorLibrary.Yellow) + "\n\n" + modifier.stat.description;
            rows.Add(new StatDrawEntry(StatCategoryDefOf.Weapon, modifier.stat.LabelCap, value, report,
                DisplayPriority - rows.Count));
        }
    }
}
