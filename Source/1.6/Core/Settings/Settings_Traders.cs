using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Traders" settings section: which tribal traders carry our unique weapons in stock. Both
// toggles default OFF — trader access is an opt-in economy change, unlike the reward pools the
// mod ships enabled.
//
// The def-writes here are heavier than the usual one-field overwrite, because vanilla gives a
// disabled state nothing to hold on to:
//  - Each enabled trader gets a StockGenerator_UMWUniqueMelee instance appended to its live
//    TraderKindDef (remove-then-add, so re-running is idempotent); the generator's own header
//    carries the rarity precedent and the war-merchant/shaman ultratech split.
//  - Our weapon defs ship <tradeability>Sellable</tradeability> (Odyssey-unique parity: reward
//    content, so traders never offer one). Trader stock hard-requires TraderCanSell — the stock
//    pipeline error-logs and drops a Sellable thing — so while either toggle is on, Sellable
//    tagged weapons are flipped to All, and flipped back when both are off. Provably inert
//    outside our own generator: the defs sit in the WeaponsUnique category with no tradeTags, so
//    no shipped StockGenerator handles them either way. Only defs recorded as flipped BY US are
//    ever reverted — a third-party weapon opted into UniqueWeaponDefs.Tag that ships its own
//    tradeability keeps it untouched in both directions.
// Both writes re-run on every play-data load (UMW_Startup.Run, after UniqueWeaponDefs.Rebuild)
// and on settings-window close (UniqueMeleeWeaponsMod.WriteSettings). Turning a trader off
// mid-save leaves an already-generated weapon sitting unpurchasable in that trader's inventory
// until they leave; nothing errors.
public partial class UniqueMeleeWeaponsSettings
{
    public bool warMerchantStocksUniques;
    public bool shamanStocksUniques;

    private const string WarMerchantDefName = "Caravan_Neolithic_WarMerchant";
    private const string ShamanDefName = "Caravan_Neolithic_ShamanMerchant";

    // Which defNames the tradeability write below flipped Sellable -> All, so only those revert.
    // Deliberately transient: an in-process reload hands us fresh def instances that are Sellable
    // again from XML, so a stale entry only ever re-writes the value a def already has.
    private static readonly HashSet<string> tradeabilityFlipped = new HashSet<string>();

    private void ExposeTraderSettings()
    {
        Scribe_Values.Look(ref warMerchantStocksUniques, "warMerchantStocksUniques", false);
        Scribe_Values.Look(ref shamanStocksUniques, "shamanStocksUniques", false);
    }

    private void ResetTraderSettings()
    {
        warMerchantStocksUniques = false;
        shamanStocksUniques = false;
    }

    public void ApplyTraderStock()
    {
        // The war party trades period arms, so its stock never rolls the Royalty-tech traits;
        // the shaman deals in relics beyond their tech and keeps the full roll.
        ApplyTo(WarMerchantDefName, warMerchantStocksUniques, allowUltratechTraits: false);
        ApplyTo(ShamanDefName, shamanStocksUniques, allowUltratechTraits: true);

        bool anyTraderStock = warMerchantStocksUniques || shamanStocksUniques;
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

    private static void ApplyTo(string traderDefName, bool enabled, bool allowUltratechTraits)
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

        listing.Gap(SectionGap);
    }
}
