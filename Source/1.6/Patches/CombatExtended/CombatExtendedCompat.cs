using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches.CombatExtended;

// Combat Extended compat: restores the trait layer on CE's melee verb. The only foreign-target
// patches in this assembly, and the only ones resolved by name. This file owns the lifecycle
// (gate, timing, apply, degraded mode); each hook lives in its own file beside it, named after
// the vanilla patch it mirrors:
//   Verb_MeleeAttackCE_OnHitTraits_Patch       <- Verb_MeleeAttackDamage_OnHitTraits_Patch
//   Verb_MeleeAttackCE_DamageConversion_Patch  <- Verb_MeleeAttackDamage_DamageConversion_Patch
//   Verb_MeleeAttackCE_Parry_Patch             <- Verb_MeleeAttackDamage_Parry_Patch
//   Verb_MeleeAttackCE_PenetrationFloor_Patch  <- the AP half of Verb_MeleeAttack_ToolMods_Patch
//   StatWorker_MeleeArmorPenetrationCE_Floor_Patch <- the same floors on CE's info card (vanilla's
//                                                 card shares the swing's getter, so needs none)
//   StatWorker_MeleeDamageBaseCE_ToolDamage_Patch  <- per-tool damageFactor on CE's damage cards
//                                                 (combat needs no CE hook for it, see its header)
// Hooks 1 to 4 each expose the same three members: TryApply (resolve its target by name, patch,
// return the count applied), Missing (set when its lookup failed) and Vetoes (which traits to keep
// out of the rolls while Missing); a hook that compares against a CE StatDef adds ResolveStat,
// called on every Run (see GUARDS). Hooks 5 and 6 are display-only: each exposes Missing and
// TryApply, vetoes nothing and is left out of AnyMissing, because a miss there costs only the
// readout while combat still applies the effect (their headers). The per-hook rationale is in each
// file's header.
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
// which CE still calls; cooldown, mass, HP and CE's own MeleePenetrationFactor are stat reads). The
// XML half of the compat root (1.6/Mods/CombatExtended/Patches/) moves the factor-shaped AP traits
// onto MeleePenetrationFactor statFactors and strips the vanilla per-tool AP fields; the four verb
// hooks cover what no stat can express, and hooks 5 and 6 make CE's cards show the per-tool
// numbers. Decompile-verified against CE 16.7.4.0.
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
// leaves that hook unapplied and vetoes the traits that depend on it from future rolls (below), so
// a CE refactor degrades to "those traits do not roll" rather than a startup error. Each hook's
// apply runs in its own try/catch (TryHook), so a throw inside one marks only THAT hook missing and
// the hooks that did apply keep their traits rolling; only a failure outside any hook (the verb
// type lookup itself) marks all four verb hooks; hooks 5 and 6 resolve their own types. Patches are
// applied once per process (Harmony would dedupe by id, but the flag keeps a reload from repeating
// the warning); the StatDef references the parry and floor hooks hold are re-resolved on EVERY Run,
// because a mid-session language change replaces every def instance and a reference cached at first
// apply would then compare unequal forever (Quilloned silently inert after a language switch). No
// assembly reference to CombatExtended.dll: every type the hook bodies touch is vanilla (Verb,
// DamageInfo, DamageResult, LocalTargetInfo, Pawn, StatDef).
//
// DEGRADED MODE. If a lookup misses, the traits whose only mechanism rides that hook would roll
// inert and advertise an effect they cannot deliver. Defs are never removed (saves reference
// them; the pool rule is "filter, never remove"); instead a postfix on
// CompUniqueWeapon.CanAddTrait, applied ONLY on a miss so the golden path carries no extra postfix
// on trait generation, vetoes by extension rather than defName (a future trait inherits the right
// behaviour); each hook's Vetoes names its extension. Weapons that already carry a vetoed trait
// keep it, inert, as today under CE without these files. A hook with two patches (the
// penetration floor) that throws on its second is marked missing with its first in place:
// conservative on purpose, its traits stop rolling while existing ones keep the half that landed.
//
// Vanilla patches stay as they are: both verbs can coexist in one game (a mod's maneuver naming
// RimWorld.Verb_MeleeAttackDamage outright escapes CE's swap), and a swing executes exactly one
// verb class, so nothing double-fires.
public static class CombatExtendedCompat
{
    // Must equal the IfModActive value on the CE roots in LoadFolders.xml (1.6/Mods/CLAUDE.md).
    public const string CePackageId = "CETeam.CombatExtended";

    internal const string VerbTypeName = "CombatExtended.Verb_MeleeAttackCE";
    internal const string LogPrefix = "[Unique Melee Weapons] Combat Extended is active but ";

    private static bool applied;

    // Called from UMW_Startup.Run on every play-data load.
    public static void Run()
    {
        if (ModLister.GetActiveModWithIdentifier(CePackageId, ignorePostfix: true) == null)
        {
            return;
        }
        Verb_MeleeAttackCE_Parry_Patch.ResolveStat();
        Verb_MeleeAttackCE_PenetrationFloor_Patch.ResolveStat();
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
            MarkAllMissing();
        }
        if (AnyMissing)
        {
            ApplyRollVeto();
        }
    }

    private static bool AnyMissing =>
        Verb_MeleeAttackCE_OnHitTraits_Patch.Missing
        || Verb_MeleeAttackCE_DamageConversion_Patch.Missing
        || Verb_MeleeAttackCE_Parry_Patch.Missing
        || Verb_MeleeAttackCE_PenetrationFloor_Patch.Missing;

    private static void MarkAllMissing()
    {
        Verb_MeleeAttackCE_OnHitTraits_Patch.Missing = true;
        Verb_MeleeAttackCE_DamageConversion_Patch.Missing = true;
        Verb_MeleeAttackCE_Parry_Patch.Missing = true;
        Verb_MeleeAttackCE_PenetrationFloor_Patch.Missing = true;
    }

    private static void ApplyHooks()
    {
        Type verbType = AccessTools.TypeByName(VerbTypeName);
        if (verbType == null)
        {
            Log.Warning(LogPrefix + VerbTypeName + " was not found; on-hit, conversion, parry and penetration-floor traits will not roll.");
            MarkAllMissing();
            return;
        }
        Harmony harmony = UniqueMeleeWeaponsMod.Harmony;
        int count = TryHook("on-hit", () => Verb_MeleeAttackCE_OnHitTraits_Patch.TryApply(verbType, harmony),
                () => Verb_MeleeAttackCE_OnHitTraits_Patch.Missing = true)
            + TryHook("damage conversion", () => Verb_MeleeAttackCE_DamageConversion_Patch.TryApply(verbType, harmony),
                () => Verb_MeleeAttackCE_DamageConversion_Patch.Missing = true)
            + TryHook("parry", () => Verb_MeleeAttackCE_Parry_Patch.TryApply(verbType, harmony),
                () => Verb_MeleeAttackCE_Parry_Patch.Missing = true)
            + TryHook("penetration floor", () => Verb_MeleeAttackCE_PenetrationFloor_Patch.TryApply(verbType, harmony),
                () => Verb_MeleeAttackCE_PenetrationFloor_Patch.Missing = true)
            + TryHook("penetration readout", () => StatWorker_MeleeArmorPenetrationCE_Floor_Patch.TryApply(harmony),
                () => StatWorker_MeleeArmorPenetrationCE_Floor_Patch.Missing = true)
            + TryHook("damage readout", () => StatWorker_MeleeDamageBaseCE_ToolDamage_Patch.TryApply(harmony),
                () => StatWorker_MeleeDamageBaseCE_ToolDamage_Patch.Missing = true);
        Log.Message($"[Unique Melee Weapons] Combat Extended detected; applied {count} melee trait hooks to {VerbTypeName}.");
    }

    // One hook's apply in isolation (GUARDS in the header): a throw marks only this hook missing.
    private static int TryHook(string name, Func<int> apply, Action markMissing)
    {
        try
        {
            return apply();
        }
        catch (Exception e)
        {
            Log.Warning(LogPrefix + "its " + name + " hook could not be applied; the trait effects that depend on it stay inert. " + e);
            markMissing();
            return 0;
        }
    }

    // Degraded mode: applied only when some lookup missed.
    private static void ApplyRollVeto()
    {
        System.Reflection.MethodInfo canAdd = AccessTools.Method(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.CanAddTrait));
        UniqueMeleeWeaponsMod.Harmony.Patch(canAdd, postfix: new HarmonyMethod(typeof(CombatExtendedCompat), nameof(CanAddTraitPostfix)));
    }

    public static void CanAddTraitPostfix(WeaponTraitDef trait, ref bool __result)
    {
        if (!__result || trait == null)
        {
            return;
        }
        if (Verb_MeleeAttackCE_OnHitTraits_Patch.Vetoes(trait)
            || Verb_MeleeAttackCE_DamageConversion_Patch.Vetoes(trait)
            || Verb_MeleeAttackCE_Parry_Patch.Vetoes(trait)
            || Verb_MeleeAttackCE_PenetrationFloor_Patch.Vetoes(trait))
        {
            __result = false;
        }
    }
}
