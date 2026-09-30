using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Traders" settings section: which vanilla traders carry our unique weapons in stock. Every toggle
// defaults OFF — trader access is an opt-in economy change, unlike the reward pools the mod ships
// enabled.
//
// Two trader bands, split by weapon tech level so that each stocks a DIFFERENT slice of the roster:
//  - Tribal (war merchant, shaman merchant): uniques at or below UniqueWeaponDefs.TribalTechCap.
//  - Outlander (the settlement, the combat supplier caravan, the combat supplier trade ship — the
//    same three TraderKindDefs VFE Pirates patches its exotic warcasket weapons onto): uniques at or
//    above UniqueWeaponDefs.OutlanderTechFloor, one step above the cap.
// The bands partition rather than overlap on purpose: tribals deal in low-tech relics, outlanders
// in industrial-and-up gear, so neither dilutes its pool with the other's stock and the choice of
// whom to trade with stays meaningful instead of "go to the outlanders, they carry everything".
// The floor is what puts VFEP's warcasket pair (Industrial) in reach of a trader at all; the vanilla
// roster tops out at Medieval, so without an Industrial-plus unique in the DefDatabase the outlander
// rows are hidden (nothing to stock; their stored values are left alone, like the Royalty-only row
// in Settings_Generation) and their generators, if a saved toggle is on, find no candidates.
//
// The def-writes here are heavier than the usual one-field overwrite, because vanilla gives a
// disabled state nothing to hold on to:
//  - Each enabled trader gets a StockGenerator_UMWUniqueMelee instance appended to its live
//    TraderKindDef (remove-then-add, so re-running is idempotent); the generator's own header
//    carries the rarity precedent, the tech band and the war-merchant/shaman ultratech split.
//  - Our weapon defs ship <tradeability>Sellable</tradeability> (Odyssey-unique parity: reward
//    content, so traders never offer one). Trader stock hard-requires TraderCanSell — the stock
//    pipeline error-logs and drops a Sellable thing — so while any toggle is on, Sellable
//    tagged weapons are flipped to All, and flipped back when all are off. Provably inert
//    outside our own generator: the defs sit in the WeaponsUnique category (vanilla melee stock
//    is a WeaponsMelee category generator, whose candidate set is that category's descendants),
//    and although they inherit tradeTags (WeaponMelee from BaseMeleeWeapon, plus VFE Pirates'
//    VFEP_WarcasketWeapon on the warcasket pair), no shipped StockGenerator stocks those: vanilla's
//    only WeaponMelee generators are Royalty's Empire ones, which also require
//    UltratechMelee/Bladelink weaponTags, and VFEP stocks only VFEP_WarcasketWeaponExotic. Only defs
//    recorded as flipped BY US are ever reverted — a third-party weapon opted into
//    UniqueWeaponDefs.Tag that ships its own tradeability keeps it untouched in both directions.
// Both writes re-run on every play-data load (UMW_Startup.Run, after UniqueWeaponDefs.Rebuild)
// and on settings-window close (UniqueMeleeWeaponsMod.WriteSettings). Turning a trader off
// mid-save leaves an already-generated weapon sitting unpurchasable in that trader's inventory
// until they leave; nothing errors.
public partial class UniqueMeleeWeaponsSettings
{
    public bool warMerchantStocksUniques;
    public bool shamanStocksUniques;
    public bool outlanderSettlementStocksUniques;
    public bool combatSupplierCaravanStocksUniques;
    public bool combatSupplierShipStocksUniques;

    private const string WarMerchantDefName = "Caravan_Neolithic_WarMerchant";
    private const string ShamanDefName = "Caravan_Neolithic_ShamanMerchant";
    private const string OutlanderSettlementDefName = "Base_Outlander_Standard";
    private const string CombatSupplierCaravanDefName = "Caravan_Outlander_CombatSupplier";
    private const string CombatSupplierShipDefName = "Orbital_CombatSupplier";

    // Which defNames the tradeability write below flipped Sellable -> All, so only those revert.
    // Deliberately transient: an in-process reload hands us fresh def instances that are Sellable
    // again from XML, so a stale entry only ever re-writes the value a def already has.
    private static readonly HashSet<string> tradeabilityFlipped = new HashSet<string>();

    private bool AnyTraderStock =>
        warMerchantStocksUniques
        || shamanStocksUniques
        || outlanderSettlementStocksUniques
        || combatSupplierCaravanStocksUniques
        || combatSupplierShipStocksUniques;

    private void ExposeTraderSettings()
    {
        Scribe_Values.Look(ref warMerchantStocksUniques, "warMerchantStocksUniques", false);
        Scribe_Values.Look(ref shamanStocksUniques, "shamanStocksUniques", false);
        Scribe_Values.Look(ref outlanderSettlementStocksUniques, "outlanderSettlementStocksUniques", false);
        Scribe_Values.Look(ref combatSupplierCaravanStocksUniques, "combatSupplierCaravanStocksUniques", false);
        Scribe_Values.Look(ref combatSupplierShipStocksUniques, "combatSupplierShipStocksUniques", false);
    }

    private void ResetTraderSettings()
    {
        warMerchantStocksUniques = false;
        shamanStocksUniques = false;
        outlanderSettlementStocksUniques = false;
        combatSupplierCaravanStocksUniques = false;
        combatSupplierShipStocksUniques = false;
    }

    public void ApplyTraderStock()
    {
        // Tribal band. The war party trades period arms, so its stock never rolls the Royalty-tech
        // traits; the shaman deals in relics beyond their tech and keeps the full roll.
        ApplyTo(WarMerchantDefName, warMerchantStocksUniques,
            TechLevel.Undefined, UniqueWeaponDefs.TribalTechCap, allowUltratechTraits: false);
        ApplyTo(ShamanDefName, shamanStocksUniques,
            TechLevel.Undefined, UniqueWeaponDefs.TribalTechCap, allowUltratechTraits: true);

        // Outlander band. Vanilla already has these three selling spacer-tech guns with no cap, so
        // an ultratech-kitted melee weapon on the same shelf is in period for them.
        ApplyTo(OutlanderSettlementDefName, outlanderSettlementStocksUniques,
            UniqueWeaponDefs.OutlanderTechFloor, TechLevel.Archotech, allowUltratechTraits: true);
        ApplyTo(CombatSupplierCaravanDefName, combatSupplierCaravanStocksUniques,
            UniqueWeaponDefs.OutlanderTechFloor, TechLevel.Archotech, allowUltratechTraits: true);
        ApplyTo(CombatSupplierShipDefName, combatSupplierShipStocksUniques,
            UniqueWeaponDefs.OutlanderTechFloor, TechLevel.Archotech, allowUltratechTraits: true);

        bool anyTraderStock = AnyTraderStock;
        foreach (ThingDef weapon in UniqueWeaponDefs.All)
        {
            if (anyTraderStock && weapon.tradeability == Tradeability.Sellable)
            {
                weapon.tradeability = Tradeability.All;
                tradeabilityFlipped.Add(weapon.defName);
            }
            else if (!anyTraderStock && tradeabilityFlipped.Contains(weapon.defName))
            {
                weapon.tradeability = Tradeability.Sellable;
            }
        }
        if (!anyTraderStock)
        {
            tradeabilityFlipped.Clear();
        }
    }

    private static void ApplyTo(
        string traderDefName, bool enabled, TechLevel minTech, TechLevel maxTech, bool allowUltratechTraits)
    {
        // SilentFail: another mod may remove or rename the vanilla trader; the toggle then just
        // does nothing rather than erroring on every load.
        TraderKindDef trader = DefDatabase<TraderKindDef>.GetNamedSilentFail(traderDefName);
        if (trader?.stockGenerators == null)
        {
            return;
        }
        trader.stockGenerators.RemoveAll(g => g is StockGenerator_UMWUniqueMelee);
        if (!enabled)
        {
            return;
        }
        StockGenerator_UMWUniqueMelee generator = new StockGenerator_UMWUniqueMelee
        {
            // Royalty's bladelink rarity, orbital variant — see the generator's header.
            countRange = new IntRange(0, 1),
            minTechLevelGenerate = minTech,
            maxTechLevelGenerate = maxTech,
            allowUltratechTraits = allowUltratechTraits,
        };
        generator.ResolveReferences(trader);
        trader.stockGenerators.Add(generator);
    }

    private void DrawTradersSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsTraders".Translate());

        // Rows are labelled with the traders' own def labels, so they track vanilla's translation;
        // a trader another mod removed gets no row (same absence the def-write tolerates above).
        TraderKindDef warMerchant = DefDatabase<TraderKindDef>.GetNamedSilentFail(WarMerchantDefName);
        if (warMerchant != null)
        {
            listing.CheckboxLabeled(
                "UMW_TraderStocksUniques".Translate(warMerchant.LabelCap),
                ref warMerchantStocksUniques,
                "UMW_TraderStocksUniquesWarMerchantDesc".Translate(warMerchant.label));
        }
        TraderKindDef shaman = DefDatabase<TraderKindDef>.GetNamedSilentFail(ShamanDefName);
        if (shaman != null)
        {
            listing.CheckboxLabeled(
                "UMW_TraderStocksUniques".Translate(shaman.LabelCap),
                ref shamanStocksUniques,
                "UMW_TraderStocksUniquesShamanDesc".Translate(shaman.label));
        }

        // Outlander rows only while some unique clears the floor (see class header). The settlement
        // trader kind has no def label (vanilla shows the settlement's faction instead), so its row
        // is a fixed string; the two combat suppliers share one vanilla label and are told apart by
        // the caravan/trade-ship qualifier in ours.
        if (!UniqueWeaponDefs.All.Any(UniqueWeaponDefs.FitsOutlander))
        {
            listing.Gap(SectionGap);
            return;
        }
        if (DefDatabase<TraderKindDef>.GetNamedSilentFail(OutlanderSettlementDefName) != null)
        {
            listing.CheckboxLabeled(
                "UMW_TraderStocksUniquesOutlanderSettlement".Translate(),
                ref outlanderSettlementStocksUniques,
                "UMW_TraderStocksUniquesOutlanderSettlementDesc".Translate());
        }
        TraderKindDef caravan = DefDatabase<TraderKindDef>.GetNamedSilentFail(CombatSupplierCaravanDefName);
        if (caravan != null)
        {
            listing.CheckboxLabeled(
                "UMW_TraderStocksUniquesCaravan".Translate(caravan.LabelCap),
                ref combatSupplierCaravanStocksUniques,
                "UMW_TraderStocksUniquesCaravanDesc".Translate(caravan.label));
        }
        TraderKindDef ship = DefDatabase<TraderKindDef>.GetNamedSilentFail(CombatSupplierShipDefName);
        if (ship != null)
        {
            listing.CheckboxLabeled(
                "UMW_TraderStocksUniquesShip".Translate(ship.LabelCap),
                ref combatSupplierShipStocksUniques,
                "UMW_TraderStocksUniquesShipDesc".Translate(ship.label));
        }

        listing.Gap(SectionGap);
    }
}
