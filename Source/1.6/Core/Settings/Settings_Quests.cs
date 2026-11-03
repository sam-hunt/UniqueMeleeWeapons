using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Quests" settings section: how often the warband opportunity site is offered.
public partial class UniqueMeleeWeaponsSettings
{
    // Selection weight of the warband opportunity-site quest. This is the real
    // default; the XML rootSelectionWeight is overwritten by ApplyWarbandQuestWeight
    // at startup, so the two only need to agree for documentation's sake.
    public const float WarbandQuestWeightDefault = 0.6f;
    public float warbandQuestWeight = WarbandQuestWeightDefault;

    private void ExposeQuestSettings()
    {
        Scribe_Values.Look(ref warbandQuestWeight, "warbandQuestWeight", WarbandQuestWeightDefault);
    }

    private void ResetQuestSettings()
    {
        warbandQuestWeight = WarbandQuestWeightDefault;
    }

    // Writes the configured weight onto the live quest def. rootSelectionWeight is
    // read fresh from the def on every opportunity-site roll, so a def-field write
    // is all an override takes. Called after defs load (UMW_Startup) and whenever
    // the settings window closes (UniqueMeleeWeaponsMod.WriteSettings).
    public void ApplyWarbandQuestWeight()
    {
        if (UMW_QuestDefOf.UMW_OpportunitySite_Warband != null)
        {
            UMW_QuestDefOf.UMW_OpportunitySite_Warband.rootSelectionWeight = warbandQuestWeight;
        }
    }

    private void DrawQuestsSection(Listing_Standard listing)
    {
        // Same vanilla-reuse rule as the Abilities header: the quests main-tab button's def label
        // is the localized word "quests" in every language, capitalized by LabelCap exactly as the
        // bottom bar renders it. There is no vanilla Keyed key for it (the tab is a MainButtonDef).
        SectionHeader(listing, MainButtonDefOf.Quests.LabelCap);

        // The quest's subsection (SubsectionHeader): named after the warband faction, as the quest
        // itself is, with the camp's world-map icon (the site part's expanding icon, what the player
        // sees on the map) and a Keyed description of the quest as the tooltip. The site part's own
        // description is one line about the camp, not the quest, and the quest's description is a
        // grammar with placeholders, so neither can stand in. The Keyed text names the ancient
        // mercenaries quest's DLC, injected coloured (family rule 6).
        SubsectionHeader(listing, UMW_QuestDefOf.UMW_Warband.LabelCap,
            UMW_QuestDefOf.UMW_WarbandCamp?.ExpandingIconTexture,
            HeaderTip(UMW_QuestDefOf.UMW_Warband.LabelCap,
                "UMW_WarbandQuestDesc".Translate(ModIntegrations.Odyssey.ColoredName).Resolve()));

        // Annotated at 1.0, which is AncientMercenaries' own rootSelectionWeight: our quest shares the
        // opportunity-site giver pool with it, so that mark is the reference point for "as often as the
        // vanilla one". The name comes from that quest's site-part def (see UMW_QuestDefOf.BanditGang)
        // so it is the same localized string the world map shows, not a second copy of it here.
        warbandQuestWeight = SliderRow(
            listing, "UMW_QuestCommonality", "UMW_WarbandQuestWeightDesc", null,
            warbandQuestWeight, WarbandQuestWeightDefault,
            min: 0f, max: 2f, step: 0.05f, format: "0.00",
            annotateAt: 1f, annotationLabel: UMW_QuestDefOf.BanditGang?.label);

        EndSubsection(listing);
        EndSection(listing);
    }
}
