using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Drop-in replacement for Odyssey's ThingSetMaker_UniqueWeapon, swapped in (via XPath, see
// Patches/RepointUniqueWeaponPool.xml) on the two vanilla consumers that roll a random unique weapon:
// Reward_UniqueWeapon (the AncientMercenaries quest reward) and MapGen_OrbitalItemStash. The stock class
// is wrong for us on two counts:
//
//   1. It calls ThingMaker.MakeThing(def) with no stuff. Our melee uniques ARE stuffable, so that
//      logs a red "madeFromStuff but stuff=null" error every roll and forces them to plain steel.
//   2. It rolls ANY def with CompUniqueWeapon, so our melee weapons dilute Odyssey's ranged-unique pool.
//
// This class is a FAITHFUL PORT of the stock algorithm — make a random candidate, keep it if its actual
// market value fits the window, retry up to 999 times — differing only in (a) rolling a random stuff for
// stuffable candidates (fixes 1) and (b) excluding our weapons from the candidate set (fixes 2).
//
// It deliberately does NOT subclass ThingSetMaker_MarketValue, though that also fixes (1). MarketValue
// inverts the stock semantics: it draws ONE target value uniformly from the window and then only picks
// candidates whose value fits UNDER that target. MapGen_OrbitalItemStash's call site
// (RoomContents_Stockpile.FillRoom, decompile-verified 1.6) passes a hardcoded
// totalMarketValueRange=(2200,2200) and NO countRange; ThingSetMaker_Sum zeroes the min for non-final
// options, so the unique option sees (0,2200) — under MarketValue that meant a uniform draw from
// 0..2200 and NO unique whenever the draw landed below the cheapest Super-quality unique (most stashes),
// where the stock class reliably produced exactly one. The retry loop restores "exactly one unique, any
// that fits", at both call sites. (AncientMercenaries passes countRange=(1,1) and a points-scaled window,
// so it was near-correct under MarketValue too; the warband quest mirrors those params against our own
// UMW_Reward_UniqueWeapon pool, which stays a plain allow-list MarketValue def.)
//
// Quality needs no handling here: CompUniqueWeapon.PostPostMake rolls the weapon's quality itself
// (QualityGenerator.Super, decompile-verified), so each made thing's MarketValue is already the real
// Super-quality value — exactly what the stock class's window test relied on.
//
// Keeping the candidate set COMP-based (not tag-based) is deliberate: third-party mods' unique weapons
// stay in the pool even if they never carry the UniqueWeapon tag (vanilla never required it) — only ours
// are removed. Ours is the UMW_UniqueMelee marker tag; UniqueWeaponDefs owns that constant and the test.
//
// LIMITATION (accepted): this only bites where the maker def is repointed to this class (the two known
// consumers). A future mod that uses the raw ThingSetMaker_UniqueWeapon class in a NEW def would still
// hit problem (1) on our weapons (non-fatal: logs + forces steel). Covering it would require patching
// the stock class itself (fragile — its candidate filter is an inlined lambda); revisit only if it occurs.
public class ThingSetMaker_UMWUnique : ThingSetMaker
{
    protected override bool CanGenerateSub(ThingSetMakerParams parms)
    {
        // Mirrors the stock class's checks exactly (it too gates on Odyssey — always true for us, since
        // the mod requires the DLC, but kept for faithfulness).
        if (!ModsConfig.OdysseyActive)
        {
            return false;
        }
        if (parms.countRange.HasValue && parms.countRange.Value.max <= 0)
        {
            return false;
        }
        if (parms.totalMarketValueRange.HasValue && parms.totalMarketValueRange.Value.max <= 0f)
        {
            return false;
        }
        return AllGeneratableThingsDebugSub(parms).Any();
    }

    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        List<ThingDef> candidates = AllGeneratableThingsDebugSub(parms).ToList();
        if (candidates.Count == 0)
        {
            return;
        }
        int count = Mathf.Max(parms.countRange?.RandomInRange ?? 1, 1);
        FloatRange window = parms.totalMarketValueRange ?? new FloatRange(0f, float.MaxValue);
        float valueSoFar = 0f;
        for (int i = 0; i < count; i++)
        {
            // Only the last weapon must also lift the running total over the window's minimum — the
            // stock class's rule, which lets earlier picks stay small and the final pick top the set up.
            bool last = i == count - 1;
            int retries = 999;
            Thing thing;
            do
            {
                thing = MakeWithRandomStuff(candidates.RandomElement(), parms);
            }
            while (retries-- > 0 && (valueSoFar + thing.MarketValue > window.max
                                     || (last && valueSoFar + thing.MarketValue < window.min)));
            if (retries <= 0)
            {
                // Window unsatisfiable (stock behaviour: give up, possibly returning fewer than count).
                break;
            }
            valueSoFar += thing.MarketValue;
            outThings.Add(thing);
        }
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
    {
        return DefDatabase<ThingDef>.AllDefs
            .Where(d => d.HasComp<CompUniqueWeapon>() && !UniqueWeaponDefs.IsOurs(d));
    }

    private static Thing MakeWithRandomStuff(ThingDef def, ThingSetMakerParams parms)
    {
        ThingDef stuff = null;
        if (def.MadeFromStuff && !GenStuff.TryRandomStuffFor(def, out stuff, parms.techLevel.GetValueOrDefault()))
        {
            stuff = GenStuff.DefaultStuffFor(def);
        }
        return ThingMaker.MakeThing(def, stuff);
    }
}
