using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Derives a WeaponTraitDef's effects as one short line each — the MODEL half of trait effect
// display. Rendering is somebody else's job: this only produces text and hangs it on the def as a
// TraitEffectLinesExtension, which every UI then draws in its own style.
//
// THE PROBLEM. Vanilla only ever displays a trait's statOffsets and statFactors (plus ranged-only
// fields): everything mechanically interesting about a RANGED trait rides those two lists. Nothing
// about a MELEE trait does — our effects live on the four DefModExtensions (see Source/1.6/Traits/)
// and on equippedHediffs/abilityProps — so traits like Bell-cast or Quilloned showed a description
// and a market value with no stated effect at all.
//
// WHY DATA AND NOT TEXT. Two earlier shapes were wrong and are worth not repeating. Patching the one
// renderer (CompUniqueWeapon.SpecialDisplayStats) fixes exactly one screen and leaves every other
// consumer — the companion mod's trait picker, a future UI of ours — to patch its own. Appending the
// lines to Def.description reaches all of them, but hands each a pre-styled blob it cannot lay out:
// in UWU's tooltip the lines landed inside the prose paragraph, above the neat "Effects:" list where
// they belonged. Structured lines on the def fix both — one derivation, N renderers, no renderer
// re-deriving anything. See TraitEffectLinesExtension for the cross-mod contract.
//
// Everything here is DERIVED from the def, never authored prose — a retuned number can't drift out of
// sync with its own summary. Lines carry stat-style value strings (ToStringNumberSense.Factor renders
// "x130%") and NO bullet, indent or trailing punctuation; the renderer adds those.
//
// Deliberately NOT rendered:
//   - MeleeToolModExtension figures already reach the top-level melee damage/AP rows (the stat workers
//     call the same AdjustedMeleeDamageAmount/AdjustedArmorPenetration overloads our postfixes hook,
//     decompile-verified) — but only as an unattributed aggregate, so the per-trait line still earns
//     its place. Head-weighted's AP correction is invisible without it.
//   - ForcedColorTwoExtension: cosmetic, and the player is looking at the weapon.
//   - burstShot*/additionalStoppingPower: read only by the projectile path, so printing them on a
//     melee weapon would state an effect that never happens (see CLAUDE.md's inert-fields rule).
public static class TraitEffectSummary
{
    // Derives each eligible trait's effect lines and attaches them as a TraitEffectLinesExtension.
    // Called from UMW_Startup, i.e. after defs are loaded AND translations injected — the lines are
    // built in the active language, so anything earlier would bake in the wrong one.
    public static void AttachToTraits()
    {
        foreach (WeaponTraitDef trait in DefDatabase<WeaponTraitDef>.AllDefsListForReading)
        {
            if (!ShouldPublish(trait))
            {
                continue;
            }
            var lines = new List<string>();
            AppendLines(trait, lines);

            // Idempotent: a play-data reload (as a language change triggers) rebuilds the
            // DefDatabase and re-runs this, but dropping any previous instance first also keeps a
            // stray second call from stacking duplicates onto a def that survived.
            trait.modExtensions?.RemoveAll(e => e is TraitEffectLinesExtension);
            if (lines.Count == 0)
            {
                continue;
            }
            (trait.modExtensions ??= new List<DefModExtension>())
                .Add(new TraitEffectLinesExtension { lines = lines });
        }
    }

    // Our own traits, plus any third-party trait built on our extension layer — but never a vanilla
    // trait. Odyssey's EMPPulser would otherwise pick up a "grants ability" line off abilityProps,
    // and annotating base-game defs is not ours to do.
    private static bool ShouldPublish(WeaponTraitDef trait)
    {
        return trait.modContentPack == UniqueMeleeWeaponsMod.ContentPack
            || trait.HasModExtension<MeleeTraitEffectExtension>()
            || trait.HasModExtension<MeleeToolModExtension>()
            || trait.HasModExtension<MeleeDamageConversionExtension>()
            || trait.HasModExtension<MeleeParryExtension>()
            || trait.HasModExtension<ForcedArtExtension>();
    }

    // Appends every summary line for trait, or nothing when it has no effects to describe.
    public static void AppendLines(WeaponTraitDef trait, List<string> lines)
    {
        AppendToolMods(trait, lines);
        AppendDamageConversions(trait, lines);
        AppendOnHitEffects(trait, lines);
        AppendParry(trait, lines);
        AppendWielderEffects(trait, lines);
        AppendAbility(trait, lines);
        AppendForcedArt(trait, lines);
    }

    // Per-tool damage/AP changes, one line per changed quantity.
    private static void AppendToolMods(WeaponTraitDef trait, List<string> lines)
    {
        List<MeleeToolMod> mods = trait.GetModExtension<MeleeToolModExtension>()?.mods;
        if (mods == null)
        {
            return;
        }
        foreach (MeleeToolMod mod in mods)
        {
            string scope = mod.AppliesToAllTools
                ? "UMW_TraitStat_AllTools".Translate().ToString()
                : mod.capacities.Select(c => c.label).ToCommaList(useAnd: true);

            if (!IsNeutralFactor(mod.damageFactor))
            {
                lines.Add("UMW_TraitStat_ToolDamage".Translate(scope).CapitalizeFirst() + " " + Factor(mod.damageFactor));
            }
            // Report the NET armor-penetration change, not the raw factor: MeleeWeapon_DamageMultiplier
            // scales tool AP as well as damage (VerbProperties.AdjustedArmorPenetration, decompile-
            // verified), and a trait that raises that stat typically carries an AP factor purely to
            // cancel the side effect (UMW_HeadWeighted's 1/1.3). Printing the raw 77% would claim an
            // armor penalty the weapon does not have; folding the trait's own multiplier in makes it
            // net out to 1 and drop off the list, which is the truth.
            float netArmorPenetration = mod.armorPenetrationFactor * DamageMultiplier(trait);
            if (!IsNeutralFactor(netArmorPenetration))
            {
                lines.Add("UMW_TraitStat_ToolArmorPenetration".Translate(scope).CapitalizeFirst() + " " + Factor(netArmorPenetration));
            }
            if (mod.armorPenetrationFloor >= 0f)
            {
                lines.Add("UMW_TraitStat_ToolArmorPenetration".Translate(scope).CapitalizeFirst() + " "
                    + "UMW_TraitStat_Minimum".Translate(mod.armorPenetrationFloor.ToStringPercent()));
            }
        }
    }

    // A rerouted base hit reads as a different WOUND, not a different damage type: the conversion defs
    // deliberately keep the vanilla damage label so the combat log stays clean ("cut", not "ragged
    // cut"), and carry their identity on the injury hediff or on where the wound lands. Emit one line
    // per delta the conversion actually makes: a changed injury hediff is named (with the bleed-rate
    // ratio, the one hediff delta that reads as a plain number — scar chance renders as "x1000%" and
    // reads as noise next to it), and a raised forced-internal chance (UMW_Stab_Deep) is stated as
    // the absolute organ-strike chance, the number the DamageWorker actually rolls.
    private static void AppendDamageConversions(WeaponTraitDef trait, List<string> lines)
    {
        List<DamageConversion> conversions = trait.GetModExtension<MeleeDamageConversionExtension>()?.conversions;
        if (conversions == null)
        {
            return;
        }
        foreach (DamageConversion conversion in conversions)
        {
            if (conversion.to == null)
            {
                continue;
            }
            HediffDef to = conversion.to.hediff;
            if (to != null && to != conversion.from?.hediff)
            {
                string line = "UMW_TraitStat_WoundType".Translate(to.label);

                float toBleed = to.injuryProps?.bleedRate ?? 0f;
                float fromBleed = conversion.from?.hediff?.injuryProps?.bleedRate ?? 0f;
                if (fromBleed > 0f && !Mathf.Approximately(toBleed, fromBleed))
                {
                    line += " (" + "UMW_TraitStat_BleedRate".Translate(Factor(toBleed / fromBleed)) + ")";
                }
                lines.Add(line);
            }

            float toInternal = conversion.to.stabChanceOfForcedInternal;
            float fromInternal = conversion.from?.stabChanceOfForcedInternal ?? 0f;
            if (!Mathf.Approximately(toInternal, fromInternal))
            {
                lines.Add("UMW_TraitStat_ForcedInternal".Translate(toInternal.ToStringPercent()));
            }
        }
    }

    // One line per on-hit effect, wrapped in its proc chance.
    private static void AppendOnHitEffects(WeaponTraitDef trait, List<string> lines)
    {
        List<MeleeOnHitEffect> effects = trait.GetModExtension<MeleeTraitEffectExtension>()?.onHitEffects;
        if (effects == null)
        {
            return;
        }
        foreach (MeleeOnHitEffect effect in effects)
        {
            string described = Describe(effect);
            if (described == null)
            {
                continue;
            }
            if (effect.fleshOnly)
            {
                described = "UMW_TraitStat_FleshOnly".Translate(described);
            }
            lines.Add(effect.chance >= 1f
                ? "UMW_TraitStat_OnHit".Translate(described)
                : "UMW_TraitStat_OnHitChance".Translate(effect.chance.ToStringPercent(), described));
        }
    }

    // The effect clause inside "On hit (x%): ...", or null for an effect that can't be described
    // (a subclass added later, or one missing its def).
    private static string Describe(MeleeOnHitEffect effect)
    {
        switch (effect)
        {
            case MeleeOnHitEffect_ExtraDamage extra when extra.def != null:
                string amount = extra.amount.ToString("0.#");
                if (extra.ignoreArmor)
                {
                    return "UMW_TraitStat_ExtraDamageIgnoreArmor".Translate(amount, extra.def.label).ToString();
                }
                return extra.armorPenetration > 0f
                    ? "UMW_TraitStat_ExtraDamageAP".Translate(amount, extra.def.label, extra.armorPenetration.ToStringPercent()).ToString()
                    : "UMW_TraitStat_ExtraDamage".Translate(amount, extra.def.label).ToString();

            case MeleeOnHitEffect_Stun stun:
                return "UMW_TraitStat_Stun".Translate(Seconds(stun.ticks)).ToString();

            case MeleeOnHitEffect_Stagger stagger:
                return "UMW_TraitStat_Stagger".Translate(Seconds(stagger.ticks), Factor(stagger.moveSpeedFactor)).ToString();

            case MeleeOnHitEffect_MentalState mental when mental.stateDef != null:
                string state = mental.stateDef.label;
                return mental.humanlikeOnly ? "UMW_TraitStat_HumanlikeOnly".Translate(state).ToString() : state;

            default:
                return null;
        }
    }

    // Defender-side parry (MeleeParryExtension): one chance line, derived from the same field the
    // combat patch rolls, so a retune can't drift from its own summary.
    private static void AppendParry(WeaponTraitDef trait, List<string> lines)
    {
        MeleeParryExtension ext = trait.GetModExtension<MeleeParryExtension>();
        if (ext?.parryChance > 0f)
        {
            lines.Add("UMW_TraitStat_Parry".Translate(ext.parryChance.ToStringPercent()));
        }
    }

    // Wielder-side stat effects: an equipped hediff (vanilla-applied via WeaponTraitWorker) is the
    // one live vehicle — trait-level equippedStatOffsets is bladelink-only in vanilla and therefore
    // inert on our comp, so it must NOT be printed here (same rule as burstShot* above: never state
    // an effect that doesn't happen). For a hediff, print its stage-0 stat modifiers rather
    // than its name, since that is what the trait actually does; fall back to the label if it
    // carries none.
    private static void AppendWielderEffects(WeaponTraitDef trait, List<string> lines)
    {
        if (trait.equippedHediffs == null)
        {
            return;
        }
        foreach (HediffDef hediff in trait.equippedHediffs)
        {
            HediffStage stage = hediff.stages?.FirstOrDefault();
            int before = lines.Count;
            if (stage?.statOffsets != null)
            {
                foreach (StatModifier modifier in stage.statOffsets)
                {
                    lines.Add("UMW_TraitStat_Wielder".Translate(StatLine(modifier, ToStringNumberSense.Offset)));
                }
            }
            if (stage?.statFactors != null)
            {
                foreach (StatModifier modifier in stage.statFactors)
                {
                    lines.Add("UMW_TraitStat_Wielder".Translate(StatLine(modifier, ToStringNumberSense.Factor)));
                }
            }
            if (lines.Count == before)
            {
                lines.Add("UMW_TraitStat_Wielder".Translate(hediff.LabelCap));
            }
        }
    }

    private static void AppendAbility(WeaponTraitDef trait, List<string> lines)
    {
        AbilityDef ability = trait.abilityProps?.abilityDef;
        if (ability != null)
        {
            lines.Add("UMW_TraitStat_GrantsAbility".Translate(ability.label));
        }
    }

    // Forced art inscription (ForcedArtExtension). One fixed line: the guarantee is the effect —
    // the inscription's content is a per-instance rolled tale, so there is no number to derive.
    private static void AppendForcedArt(WeaponTraitDef trait, List<string> lines)
    {
        if (trait.HasModExtension<ForcedArtExtension>())
        {
            lines.Add("UMW_TraitStat_ForcedArt".Translate());
        }
    }

    // The trait's own MeleeWeapon_DamageMultiplier statFactor (1 when it sets none). Only this
    // trait's contribution — a co-rolled trait's multiplier is that trait's line to explain.
    private static float DamageMultiplier(WeaponTraitDef trait)
    {
        return trait.statFactors.GetStatFactorFromList(StatDefOf.MeleeWeapon_DamageMultiplier);
    }

    // Same rendering vanilla uses for a trait's own statOffsets/statFactors lines.
    private static string StatLine(StatModifier modifier, ToStringNumberSense sense)
    {
        return modifier.stat.LabelCap + " " + modifier.stat.Worker.ValueToString(modifier.value, finalized: false, sense);
    }

    // Whether a factor would render as "x100%" and so say nothing. Tested at the DISPLAYED precision,
    // not Mathf.Approximately: a cancelling pair of hand-rounded factors lands near 1 without hitting
    // it (UMW_HeadWeighted's 0.7692 x 1.3 = 0.99996), and would otherwise print a no-op line.
    private static bool IsNeutralFactor(float value)
    {
        return Mathf.RoundToInt(value * 100f) == 100;
    }

    // "x130%" — vanilla's factor rendering.
    private static string Factor(float value)
    {
        return value.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor);
    }

    // "2s" / "1.6s", following AbilityDef.StatSummary's ToString() + "LetterSecond" precedent.
    private static string Seconds(int ticks)
    {
        return (ticks / 60f).ToString("0.#") + "LetterSecond".Translate();
    }
}
