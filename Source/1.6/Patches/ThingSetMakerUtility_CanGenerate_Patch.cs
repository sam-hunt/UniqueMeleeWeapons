using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Setting-gated (the per-weapon toggles, all on by default): keeps a weapon the player switched off out
// of every generation and reward pool.
//
// ThingSetMakerUtility.CanGenerate is the single choke point for that. Every ThingSetMaker that can
// contain our weapons — ours (UMW_Reward_UniqueWeapon, which the warband quest rolls from) and the
// tag-based makers behind ancient crates, fishing, map-gen loot and Reward_ItemsStandard — reaches its
// candidate set through ThingSetMakerUtility.GetAllowedThingDefs, whose final Where clause calls
// CanGenerate on each def. (The repointed vanilla unique-weapon consumers do NOT route through the
// utility — but they exclude our weapons by construction, so there is nothing there to gate.) Filtering here therefore also keeps each maker's count/value/mass
// pre-estimates consistent with what it can actually pick, and a maker that ends up with nothing left
// reports it cannot generate rather than producing a broken set.
//
// This is the pool gate ONLY. It deliberately does not remove the def from the DefDatabase or from
// trade/storage/debug: a disabled weapon still exists, so a save that already contains one keeps it
// working, and re-enabling is a settings flip rather than a migration.
//
// Coverage boundary: a ThingSetMaker that hand-picks defs without going through ThingSetMakerUtility
// bypasses this gate. Stock ThingSetMaker_UniqueWeapon is the one such class that can see our weapons
// (it picks any CompUniqueWeapon def straight off the DefDatabase); vanilla's only two uses of it are
// repointed onto our replacement by RepointUniqueWeaponPool.xml, so the gate holds for everything shipped —
// but a THIRD-PARTY def using that stock class (or its own hand-rolled maker) is not covered, which is
// why player-facing copy says "vanilla" pools (Keyed/UMW_UI.xml). Trade and raider kit need no gate at
// all: the weapons are tradeability=Sellable (never stocked) and generateAllowChance=0 (never spawned
// equipped). (Decompile-verified, RimWorld 1.6: CanGenerate's only callers are
// GetAllowedThingDefs and Reset; the leaf makers reaching weapons — Count, MarketValue, StackCount —
// all funnel through GetAllowedThingDefs.)
//
// The one place eligibility is cached instead of asked live (ThingSetMakerUtility.allGeneratableItems,
// built once at play-data load) is refreshed by settings.ApplyWeaponAvailability when the window closes;
// see the comment there for why nothing else needs invalidating.
//
// The null-conditional on Settings is defensive only: the mod constructor assigns Settings before
// PatchAll installs this postfix, so no call our patch can observe precedes it.
[HarmonyPatch(typeof(ThingSetMakerUtility), nameof(ThingSetMakerUtility.CanGenerate))]
public static class ThingSetMakerUtility_CanGenerate_Patch
{
    public static void Postfix(ThingDef thingDef, ref bool __result)
    {
        if (__result && UniqueMeleeWeaponsMod.Settings?.IsWeaponDisabled(thingDef) == true)
        {
            __result = false;
        }
    }
}
