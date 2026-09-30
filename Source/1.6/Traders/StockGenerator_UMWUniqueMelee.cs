using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UniqueMeleeWeapons.Patches;
using Verse;

namespace UniqueMeleeWeapons;

// Puts our unique melee weapons into a trader's stock. Never referenced from XML: instances are
// built and attached to the vanilla TraderKindDefs by Settings_Traders.ApplyTraderStock, so the
// whole feature is settings-gated at runtime with vanilla defs untouched while it is off (and the
// countRange/tech-band/ultratech knobs live there, next to the toggle that owns them).
//
// Rarity for the tribal instances follows Royalty's precedent for bladelink weapons in Imperial
// stock: countRange 0~1, the range the orbital Empire trader uses (the Imperial caravan actually
// carries exactly 1 every visit; 0~1 is the rarer of the two shipped variants and fits "occasionally
// carries a relic"). The outlander instances roll -3~1 (one in five): IntRange.RandomInRange is
// inclusive, and the loop below runs zero times on a non-positive roll, exactly as vanilla's
// TryMakeForStock does for the -10~1 exotic pin VFE Pirates puts on the same traders. Why the bands
// differ is on Settings_Traders.
// The weapon def is picked uniformly rather than through StockGenerator_MarketValue's
// cheaper-is-likelier curve — the pool is our ~8 similarly-priced weapons, not a whole tag's worth
// of vanilla gear, so the curve would add a knob with nothing to turn.
//
// The candidate list re-derives the same gates the reward pools apply: the per-weapon settings
// toggles (which normally act through ThingSetMakerUtility.CanGenerate — a StockGenerator never
// routes through that utility, so the check is repeated here) and TraderCanSell (the caller,
// ThingSetMaker_TraderStock.Generate, error-logs and drops anything failing it; our defs pass only
// while Settings_Traders' tradeability def-write is active). Stuff rolls through
// GenStuff.TryRandomStuffFor exactly like ThingSetMaker_UMWUnique, so the exclude-wood setting's
// AllowedStuffsFor patch applies for free, and quality needs no handling here because
// CompUniqueWeapon.PostPostMake forces its own Super roll on every unique.
//
// allowUltratechTraits=false (the war merchant instance) keeps the three Royalty-tech traits out
// of the trait roll for weapons THIS generator makes, via the scoped veto in
// CompUniqueWeapon_UltratechTraits_Patch — the trait never enters the candidate set, so another
// trait rolls in its place and no generate-and-discard retry is needed.
//
// Candidates are also banded by tech level: capped at maxTechLevelGenerate, mirroring vanilla
// StockGenerator_MiscItems (StockGenerator declares the field but its base never applies it), and
// floored at our own minTechLevelGenerate (vanilla has no floor field; its traders only ever cap).
// The tribal instances set the cap to UniqueWeaponDefs.TribalTechCap and the outlander instances the
// floor to UniqueWeaponDefs.OutlanderTechFloor, one step above it, so the two bands partition the
// roster (rationale on those constants). The band gates what a trader SELLS only: HandlesThingDef
// below keeps the vanilla cap-only buy side, so an outlander trader still buys a medieval unique.
public class StockGenerator_UMWUniqueMelee : StockGenerator
{
    // Whether weapons generated here may roll the Royalty-tech traits (see class header). The
    // global allowUltratechTraits setting still applies on top: off there means off everywhere.
    public bool allowUltratechTraits = true;

    // Lowest weapon techLevel this instance stocks (see class header). Undefined (the enum's zero)
    // admits everything, matching the unbounded default of the vanilla cap it pairs with.
    public TechLevel minTechLevelGenerate = TechLevel.Undefined;

    public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
    {
        if (!ModsConfig.OdysseyActive)
        {
            yield break;
        }
        List<ThingDef> candidates = UniqueWeaponDefs.All
            .Where(d => d.tradeability.TraderCanSell()
                && d.techLevel >= minTechLevelGenerate
                && d.techLevel <= maxTechLevelGenerate
                && UniqueMeleeWeaponsMod.Settings?.IsWeaponDisabled(d) != true)
            .ToList();
        if (candidates.Count == 0)
        {
            yield break;
        }
        int count = countRange.RandomInRange;
        for (int i = 0; i < count; i++)
        {
            ThingDef def = candidates.RandomElement();
            ThingDef stuff = null;
            if (def.MadeFromStuff && !GenStuff.TryRandomStuffFor(def, out stuff))
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            Thing thing;
            try
            {
                CompUniqueWeapon_UltratechTraits_Patch.banUltratechScope = !allowUltratechTraits;
                thing = ThingMaker.MakeThing(def, stuff);
            }
            finally
            {
                CompUniqueWeapon_UltratechTraits_Patch.banUltratechScope = false;
            }
            yield return thing;
        }
    }

    // Same shape as StockGenerator_Tag's: also what lets the trader BUY these from the player
    // (TraderKindDef.WillTrade asks every generator), so a shaman who can stock a unique can be
    // sold one too. Deliberately not gated on the per-weapon toggles or on minTechLevelGenerate:
    // those govern generation, and refusing to buy a weapon the player already owns would only
    // strand it.
    public override bool HandlesThingDef(ThingDef thingDef)
    {
        return UniqueWeaponDefs.IsOurs(thingDef)
            && thingDef.tradeability != Tradeability.None
            && thingDef.techLevel <= maxTechLevelBuy;
    }
}
