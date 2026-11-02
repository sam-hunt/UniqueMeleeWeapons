using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended compat: restores the trait layer on CE's melee verb. The only foreign-target
// patches in this assembly, and the only ones resolved by name.
//
// WHY. CombatExtended.Verb_MeleeAttackCE derives from the abstract Verb_MeleeAttack, not from
// Verb_MeleeAttackDamage, and reimplements the whole swing (hit, dodge, parry, crit, damage) with
// its own DamageInfosToApply and ApplyMeleeDamageToTarget. CE's Maneuvers patch swaps every
// maneuver's verbClass to it, so under CE none of our Verb_MeleeAttackDamage postfixes ever runs:
// the on-hit effects, the damage conversions, the parry and the per-tool armor penetration all go
// inert while the info card keeps promising them. Base weapon stats need nothing: our uniques
// inherit CE's ToolCE tools, Bulk, counter-parry and tags from their bases through ParentName
// (patches apply to the unified XML before inheritance resolves). What survives natively is
// anything riding a weapon stat (MeleeWeapon_DamageMultiplier is inside AdjustedMeleeDamageAmount,
// which CE still calls; cooldown, mass, HP and CE's own MeleePenetrationFactor are stat reads).
// The XML half of the compat root (1.6/Mods/CombatExtended/Patches/) moves the factor-shaped AP
// traits onto MeleePenetrationFactor statFactors and strips the vanilla per-tool AP fields; the
// four hooks here cover what no stat can express. Decompile-verified against CE 16.7.4.0.
//
// THE HOOKS, each a postfix on Verb_MeleeAttackCE:
//   1. ApplyMeleeDamageToTarget(LocalTargetInfo): on-hit effects, the vanilla body verbatim via
//      MeleeOnHitTraitResolver. CE's swing is a LIST of DamageInfos and the method returns the
//      LAST entry's DamageResult with only totalDamageDealt summed across entries; on a blunt crit
//      against flesh (and every blunt surprise attack, which CE forces to crit) the last entry is
//      CE's appended Stun, whose worker deals no hit points, so that result's `wounded` is false
//      even though the blow wounded. Reading `wounded` alone left every on-hit trait inert on
//      roughly 40 to 60% of mace and warhammer head hits (found 2026-11-03 review). The hook
//      therefore treats the swing as wounding when `wounded` OR the summed damage is positive;
//      DamageWorker_AddInjury sets the two together, and Stun adds 0, so the sum is the same
//      answer vanilla's flag gives. Consequence worth knowing: BellCast now also procs on CE's
//      crits, where its StunFor and CE's crit stun take the longer duration, never the sum.
//   2. DamageInfosToApply(LocalTargetInfo, bool): damage conversion, the vanilla passthrough via
//      MeleeDamageConversionResolver. CE's armorPenetration rides ArmorPenetrationInt unchanged,
//      so a Sharp-to-Sharp swap keeps the right CE number. Known limit: CE's own internal-hit roll
//      runs before the rebuild, on the original def, and presets HitPart only when it fires; on
//      the common other path UMW_Stab_Deep's higher chance still applies through vanilla
//      DamageWorker_Stab.ChooseHitPart (CE does not patch that worker).
//   3. GetComparativeChanceAgainst(Pawn, Pawn, StatDef, float, float), protected static: the
//      helper every parry and riposte computation and the melee tooltip go through, returning
//      Clamp01(base + first.stat - second.stat * mult). For the parry calls the FIRST pawn is the
//      one being attacked (CE's parameter names are reversed, hence the positional __0). When the
//      stat is CE's MeleeParryChance and that pawn's primary carries a MeleeParryExtension trait,
//      the trait's chance is added post-curve to the final roll: CE's parry stat is skill-driven
//      and compressed by a postProcessCurve, so adding before the curve would shrink a weapon
//      property to a few points. Our vanilla parry PREFIX is deliberately not ported: under CE a
//      prefix returning false would leave TryCastShot logging a hit with no damage after CE's own
//      parry resolution has already run; CE's parry (block, weapon damage, riposte, log line) is
//      the better outcome anyway. Decided: the bonus also applies when a worn shield is CE's parry
//      item, because the chance is one number from this helper and the item only decides what
//      absorbs the blow; Quilloned rolls on the gladius, a one-hander, and skipping behind a
//      shield would punish the one weapon the trait exists for.
//   4. The ArmorPenetrationSharp / ArmorPenetrationBlunt getters: the quality-scaled floors
//      (MeleeToolMod.ceArmorPenetration*QualityFloor), compared against CE's fully-scaled value
//      (tool field x part/life-stage/damage-factor^0.75 x skill x MeleePenetrationFactor) and
//      multiplied by the quality factor of MeleePenetrationFactor read through an abstract
//      StatRequest (def + quality; no stuff, no thing), exactly as the vanilla floor uses
//      MeleeWeapon_DamageMultiplier's. The getters feed the hit, the Sharp crit doubling (applied
//      after the getter) and the Sharp-deflected-into-Blunt path (LastAttackVerb), so two patches
//      cover every read. CE's info card reads ToolCE fields directly and never shows the floor;
//      our trait block does (TraitEffectSummary's CE-unit line).
//
// TIMING (the BetterTradersGuild v1.1.0 lesson, Source/1.6/CLAUDE.md). Applying a detour JITs
// the target and runs its declaring type's static constructor; CE's verb touches CE_StatDefOf, so
// a patch from the Mod ctor would run that cctor into an empty DefDatabase. Run() is invoked from
// UMW_Startup.Run, i.e. from the StaticConstructorOnStartupUtility.CallAll postfix: after defs,
// cross-refs, DefOf rebinding, DefInjected, and after every mod's [StaticConstructorOnStartup]
// has run, CE's included. The opposite end of the load from the incident.
//
// GUARDS. Nothing happens unless CE is active (packageId gate, ignorePostfix so a Workshop copy
// beside a local one still matches). Every target is resolved by name; a miss logs ONE warning,
// leaves that hook unapplied and vetoes the traits that depend on it from future rolls (below),
// so a CE refactor degrades to "those traits do not roll" rather than a startup error. The whole
// apply is in try/catch. Patches are applied once per process (Harmony would dedupe by id, but
// the flag keeps a reload from repeating the warning); the two StatDef references are re-resolved
// on EVERY Run, because a mid-session language change replaces every def instance and a
// reference cached at first apply would then compare unequal forever (Quilloned silently inert
// after a language switch). No assembly reference to CombatExtended.dll: every type the hook
// bodies touch is vanilla (Verb, DamageInfo, DamageResult, LocalTargetInfo, Pawn, StatDef).
//
// DEGRADED MODE. If a lookup misses, the traits whose only mechanism rides that hook would roll
// inert and advertise an effect they cannot deliver. Defs are never removed (saves reference
// them; the pool rule is "filter, never remove"); instead a postfix on CompUniqueWeapon.CanAddTrait,
// applied ONLY on a miss so the golden path carries no extra postfix on trait generation, vetoes
// by extension rather than defName (a future trait inherits the right behaviour): a Hook 1 miss
// vetoes MeleeTraitEffectExtension, Hook 2 MeleeDamageConversionExtension, Hook 3
// MeleeParryExtension, Hook 4 any MeleeToolModExtension entry carrying a CE floor. Weapons that
// already carry a vetoed trait keep it, inert, as today under CE without this file.
//
// Vanilla patches stay as they are: both verbs can coexist in one game (a mod's maneuver naming
// RimWorld.Verb_MeleeAttackDamage outright escapes CE's swap), and a swing executes exactly one
// verb class, so nothing double-fires.
public static class CombatExtendedCompat
{
    // Must equal the IfModActive value on the CE roots in LoadFolders.xml (1.6/Mods/CLAUDE.md).
    public const string CePackageId = "CETeam.CombatExtended";

    private const string VerbTypeName = "CombatExtended.Verb_MeleeAttackCE";
    private const string LogPrefix = "[Unique Melee Weapons] Combat Extended is active but ";

    private static bool applied;

    // Hook-miss flags, read by the roll veto. Set once at apply time, never cleared.
    private static bool onHitHookMissing;
    private static bool conversionHookMissing;
    private static bool parryHookMissing;
    private static bool floorHookMissing;

    // CE's stats, re-resolved on every play-data load (see header). Null when CE lacks them.
    private static StatDef parryStat;
    private static StatDef penetrationFactorStat;

    // Called from UMW_Startup.Run on every play-data load.
    public static void Run()
    {
        if (ModLister.GetActiveModWithIdentifier(CePackageId, ignorePostfix: true) == null)
        {
            return;
        }
        parryStat = DefDatabase<StatDef>.GetNamedSilentFail("MeleeParryChance");
        penetrationFactorStat = DefDatabase<StatDef>.GetNamedSilentFail("MeleePenetrationFactor");
        if (applied)
        {
            return;
        }
        applied = true;
        try
        {
            ApplyHooks();
        }
        catch (Exception e)
        {
            Log.Warning(LogPrefix + "its melee hooks could not be applied; the trait effects that depend on them stay inert. " + e);
            onHitHookMissing = conversionHookMissing = parryHookMissing = floorHookMissing = true;
        }
        if (onHitHookMissing || conversionHookMissing || parryHookMissing || floorHookMissing)
        {
            ApplyRollVeto();
        }
    }

    private static void ApplyHooks()
    {
        Type verbType = AccessTools.TypeByName(VerbTypeName);
        if (verbType == null)
        {
            Log.Warning(LogPrefix + VerbTypeName + " was not found; on-hit, conversion, parry and penetration-floor traits will not roll.");
            onHitHookMissing = conversionHookMissing = parryHookMissing = floorHookMissing = true;
            return;
        }
        Harmony harmony = UniqueMeleeWeaponsMod.Harmony;
        int count = 0;

        MethodInfo onHit = AccessTools.Method(verbType, "ApplyMeleeDamageToTarget", new[] { typeof(LocalTargetInfo) });
        if (onHit == null)
        {
            Log.Warning(LogPrefix + VerbTypeName + ".ApplyMeleeDamageToTarget was not found; on-hit traits will not roll.");
            onHitHookMissing = true;
        }
        else
        {
            harmony.Patch(onHit, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(OnHitPostfix)));
            count++;
        }

        MethodInfo damageInfos = AccessTools.Method(verbType, "DamageInfosToApply", new[] { typeof(LocalTargetInfo), typeof(bool) });
        if (damageInfos == null)
        {
            Log.Warning(LogPrefix + VerbTypeName + ".DamageInfosToApply was not found; damage-conversion traits will not roll.");
            conversionHookMissing = true;
        }
        else
        {
            harmony.Patch(damageInfos, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(DamageInfosPostfix)));
            count++;
        }

        MethodInfo comparative = AccessTools.Method(verbType, "GetComparativeChanceAgainst",
            new[] { typeof(Pawn), typeof(Pawn), typeof(StatDef), typeof(float), typeof(float) });
        if (comparative == null || parryStat == null)
        {
            Log.Warning(LogPrefix + VerbTypeName + ".GetComparativeChanceAgainst or the MeleeParryChance stat was not found; parry traits will not roll.");
            parryHookMissing = true;
        }
        else
        {
            harmony.Patch(comparative, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(ComparativeChancePostfix)));
            count++;
        }

        MethodInfo sharpGetter = AccessTools.PropertyGetter(verbType, "ArmorPenetrationSharp");
        MethodInfo bluntGetter = AccessTools.PropertyGetter(verbType, "ArmorPenetrationBlunt");
        if (sharpGetter == null || bluntGetter == null || penetrationFactorStat == null)
        {
            Log.Warning(LogPrefix + VerbTypeName + ".ArmorPenetrationSharp/Blunt or the MeleePenetrationFactor stat was not found; penetration-floor traits will not roll.");
            floorHookMissing = true;
        }
        else
        {
            harmony.Patch(sharpGetter, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(SharpPenetrationPostfix)));
            harmony.Patch(bluntGetter, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(BluntPenetrationPostfix)));
            count += 2;
        }

        Log.Message($"[Unique Melee Weapons] Combat Extended detected; applied {count} melee trait hooks to {VerbTypeName}.");
    }

    // Hook 1.
    public static void OnHitPostfix(Verb __instance, LocalTargetInfo target, DamageWorker.DamageResult __result)
    {
        // Summed damage, not the last entry's flag: see hook 1 in the header.
        bool wounded = __result != null && (__result.wounded || __result.totalDamageDealt > 0f);
        MeleeOnHitTraitResolver.Fire(__instance, target, wounded);
    }

    // Hook 2 (passthrough).
    public static IEnumerable<DamageInfo> DamageInfosPostfix(IEnumerable<DamageInfo> values, Verb __instance)
    {
        return MeleeDamageConversionResolver.Apply(values, __instance.EquipmentSource);
    }

    // Hook 3. __0 is CE's first parameter: for the parry and tooltip calls, the pawn being attacked.
    public static void ComparativeChancePostfix(ref float __result, Pawn __0, StatDef stat)
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

    // Hook 4.
    public static void SharpPenetrationPostfix(ref float __result, Verb __instance)
    {
        ApplyFloor(ref __result, __instance, sharp: true);
    }

    public static void BluntPenetrationPostfix(ref float __result, Verb __instance)
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

    // Degraded mode: applied only when some lookup missed.
    private static void ApplyRollVeto()
    {
        MethodInfo canAdd = AccessTools.Method(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.CanAddTrait));
        UniqueMeleeWeaponsMod.Harmony.Patch(canAdd, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(CanAddTraitPostfix)));
    }

    public static void CanAddTraitPostfix(WeaponTraitDef trait, ref bool __result)
    {
        if (!__result || trait == null)
        {
            return;
        }
        if ((onHitHookMissing && trait.HasModExtension<MeleeTraitEffectExtension>())
            || (conversionHookMissing && trait.HasModExtension<MeleeDamageConversionExtension>())
            || (parryHookMissing && trait.HasModExtension<MeleeParryExtension>())
            || (floorHookMissing && HasCeFloor(trait)))
        {
            __result = false;
        }
    }

    private static bool HasCeFloor(WeaponTraitDef trait)
    {
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
