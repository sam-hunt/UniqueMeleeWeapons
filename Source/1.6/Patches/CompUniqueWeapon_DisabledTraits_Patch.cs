using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// The settings-driven trait veto: keeps every trait the player switched off in the Traits section
// (Settings_Traits.cs, disabledTraits) out of the trait roll, plus a scoped ban on the five
// Royalty-root ultratech traits (UMW_Monomolecular, UMW_PlasmaCored, UMW_ZeusHeaded and the eltex
// pair UMW_EltexPlated, UMW_EltexFitted; vanilla's eltex is techLevel Ultra) for the war merchant's
// stock. The root's sixth trait, UMW_LeadLined, is period-neutral and stays off the list.
// Everything else about a vetoed def stays untouched, so an already-generated weapon keeps the
// trait it rolled. Through 1.x this was a single allowUltratechTraits toggle over the Royalty three;
// the per-trait set subsumed it (Settings_Traits.cs migrates a saved "off" onto the whole Royalty
// group, lead-lined included, by decision: no special handling for a retired key).
//
// CompUniqueWeapon.CanAddTrait is the single filter InitializeTraits applies before weighting the
// candidates (`allDefs.Where(CanAddTrait)`, then RandomElementByWeight on commonality), so vetoing
// here removes the trait from the candidate set outright. That is deliberately NOT done by zeroing
// commonality at startup the way the ability tunings write their def fields: RandomElementByWeight
// skips zero-weight entries but errors and returns null if the WHOLE candidate set weighs zero, and
// a zeroed trait that happened to be the last candidate standing would hand InitializeTraits a null
// trait to add (vanilla's ConfigErrors also rejects a commonality of zero). Removing the candidate
// can't produce that. An EMPTY candidate set is survivable for the roll (InitializeTraits guards with
// Any) but not for the weapon's naming, which Settings_Traits.cs keeps impossible.
//
// Not gated on our weapons: a vetoed trait belongs to a UMW_* category, which no non-UMW weapon
// lists in its CompProperties_UniqueWeapon, so the vanilla ranged uniques never offer them anyway.
// A null DefOf field (Royalty absent) matches nothing, so the scoped ban is inert there too.
//
// banUltratechScope is raised by StockGenerator_UMWUniqueMelee around ThingMaker.MakeThing so the
// war merchant's stock never rolls the ultratech five while the shaman's still can. The flag works
// because InitializeTraits runs synchronously inside MakeThing (PostPostMake); it is [ThreadStatic]
// so a generation on a worker thread can never leak the ban into another thread's roll. The
// generator asks CanBanUltratechFor first: the settings window keeps every weapon one enabled
// first-pick trait, but it does not model this scoped ban, so a player who disabled every first
// pick of a weapon except an ultratech one would otherwise hand the war merchant's roll an empty
// candidate set and vanilla's namer an empty adjective list. For that weapon the ban is skipped
// for the roll (an ultratech trait at a war merchant over a naming error).
[HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.CanAddTrait))]
public static class CompUniqueWeapon_DisabledTraits_Patch
{
    [ThreadStatic] internal static bool banUltratechScope;

    public static void Postfix(WeaponTraitDef trait, ref bool __result)
    {
        if (!__result)
        {
            return;
        }
        if (UniqueMeleeWeaponsMod.Settings?.IsTraitDisabled(trait) == true
            || (banUltratechScope && IsUltratechTrait(trait)))
        {
            __result = false;
        }
    }

    // Whether raising the ban for a roll of `def` leaves it a first pick (class header).
    internal static bool CanBanUltratechFor(ThingDef def)
    {
        return UniqueMeleeWeaponsMod.Settings?.HasEnabledFirstPick(def, IsUltratechTrait) != false;
    }

    private static bool IsUltratechTrait(WeaponTraitDef trait)
    {
        return trait != null
            && (trait == UMW_DefOf.UMW_Monomolecular
                || trait == UMW_DefOf.UMW_PlasmaCored
                || trait == UMW_DefOf.UMW_ZeusHeaded
                || trait == UMW_DefOf.UMW_EltexPlated
                || trait == UMW_DefOf.UMW_EltexFitted);
    }
}
