using System;
using HarmonyLib;
using RimWorld;

namespace UniqueMeleeWeapons.Patches;

// Setting-gated (allowUltratechTraits, default on): keeps our three Royalty-tech traits —
// UMW_Monomolecular, UMW_PlasmaCored, UMW_ZeusHeaded — out of the trait roll, for players running a
// medieval-only colony where a mechanite edge or a zeus-cell head reads as out of period. Everything
// else about those defs stays untouched, so an already-generated weapon keeps the trait it rolled.
//
// CompUniqueWeapon.CanAddTrait is the single filter InitializeTraits applies before weighting the
// candidates (`allDefs.Where(CanAddTrait)`, then RandomElementByWeight on commonality), so vetoing
// here removes the trait from the candidate set outright. That is deliberately NOT done by zeroing
// commonality at startup the way the ability tunings write their def fields: RandomElementByWeight
// skips zero-weight entries but errors and returns null if the WHOLE candidate set weighs zero, and
// a zeroed trait that happened to be the last candidate standing would hand InitializeTraits a null
// trait to add. Removing the candidate can't produce that.
//
// Not gated on our weapons: these three traits belong to UMW_Bladed/UMW_Blunt, which no non-UMW
// weapon lists in its CompProperties_UniqueWeapon, so the vanilla ranged uniques never offer them
// anyway. A null DefOf field (Royalty absent) matches nothing, so the patch is inert there too.
//
// Besides the global setting, the same veto serves a second, scoped consumer: banUltratechScope,
// raised by StockGenerator_UMWUniqueMelee around ThingMaker.MakeThing so the war merchant's stock
// never rolls these traits while the shaman's still can. The flag works because InitializeTraits
// runs synchronously inside MakeThing (PostPostMake); it is [ThreadStatic] so a generation on a
// worker thread can never leak the ban into another thread's roll.
[HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.CanAddTrait))]
public static class CompUniqueWeapon_UltratechTraits_Patch
{
    [ThreadStatic] internal static bool banUltratechScope;

    public static void Postfix(WeaponTraitDef trait, ref bool __result)
    {
        if (__result
            && (UniqueMeleeWeaponsMod.Settings?.allowUltratechTraits == false || banUltratechScope)
            && IsUltratechTrait(trait))
        {
            __result = false;
        }
    }

    private static bool IsUltratechTrait(WeaponTraitDef trait)
    {
        return trait != null
            && (trait == UMW_DefOf.UMW_Monomolecular
                || trait == UMW_DefOf.UMW_PlasmaCored
                || trait == UMW_DefOf.UMW_ZeusHeaded);
    }
}
