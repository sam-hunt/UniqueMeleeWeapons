using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Root node of the warband opportunity-site quest -- the low-tech (tribal) sibling of Odyssey's
// AncientMercenaries. Structurally it mirrors QuestNode_Root_AncientMercenaries (roll one unique weapon
// scaled to points, generate a leader and band, spawn a claimable site with a 30-day timeout, succeed on
// AllEnemiesDefeated) with four deliberate differences:
//
//   * Faction: a TEMPORARY, hidden, hostile faction created here (Beggars-style) instead of the permanent
//     Faction.OfAncientsHostile. It is reserved to the quest so it survives while active, and is removed
//     automatically once the quest ends and the site world object is destroyed (FactionManager).
//   * Reward pool: UMW_Reward_UniqueWeapon (our stuff-aware, melee-only pool) instead of Reward_UniqueWeapon,
//     filtered to UniqueWeaponDefs.TribalTechCap so a tribal band never carries an Industrial unique.
//   * Leader/band: vanilla tribal pawnkinds (Tribal_ChiefMelee + the faction's Combat pawnGroupMaker)
//     instead of AncientSoldier_Leader / ancients-hostile.
//   * Site theme: the AbandonedColonyTribal tile mutator (a ruined, pawn-less tribal settlement) instead
//     of an AncientStructure, and our own QuestPart_SpawnWarband (which reads "SettlementRect" and makes
//     the lord for our temporary faction).
//
// See the "Warband quest" architecture note in CLAUDE.md for the full rationale.
public class QuestNode_Root_Warband : QuestNode
{
    // 30 days, matching AncientMercenaries.
    private const int TimeoutTicks = 1800000;

    protected override bool TestRunInt(Slate slate)
    {
        // Requires Odyssey: the unique-weapon value pool and the AbandonedColonyTribal mutator are both
        // Odyssey content. Mirrors AncientMercenaries' own Odyssey gate.
        //
        // Also requires at least one of our weapons to still be enabled in the settings: the entire
        // reward is one rolled unique, so with all of them switched off the quest would offer a leader
        // holding nothing. Stopping it from being offered is the honest outcome, and it comes back the
        // moment a weapon is re-enabled. Counted under the same tech cap as the roll in RunInt, or
        // leaving only (say) the VFEP warcasket uniques enabled would offer a quest that rolls nothing.
        return ModsConfig.OdysseyActive
            && UniqueMeleeWeaponsMod.Settings.AnyWeaponEnabled(UniqueWeaponDefs.FitsTribal);
    }

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;
        float points = slate.Get("points", 0f);
        PlanetTile siteTile = slate.Get<PlanetTile>("siteTile");

        // 1. Temporary hidden faction, hostile to the player. Reserve it to the quest so it isn't removed
        //    while active (FactionCanBeRemoved gates on !IsReservedByAnyQuest).
        Faction faction = GenerateTemporaryWarbandFaction();
        quest.ReserveFaction(faction);

        // 2. Roll exactly one of OUR melee uniques, value-scaled to points (same window as AncientMercenaries).
        //    Capped at UniqueWeaponDefs.TribalTechCap via validator, NOT parms.techLevel: that one also makes
        //    ThingSetMakerByTotalStatUtility weight sub-cap Neolithic weapons x0.1 (so a Medieval cap would
        //    make the knife/spear/breach axe 10x rarer) and feeds the stuff roll. The pool's own fixedParams
        //    set no validator, so ours passes through ApplyFixedParams untouched.
        ThingSetMakerParams parms = new ThingSetMakerParams
        {
            validator = UniqueWeaponDefs.FitsTribal,
            makingFaction = faction,
            countRange = new IntRange(1, 1),
            totalMarketValueRange = new FloatRange(0.7f, 1.3f) * QuestTuning.PointsToRewardMarketValueCurve.Evaluate(points)
        };
        List<Thing> weapons = UMW_QuestDefOf.UMW_Reward_UniqueWeapon.root.Generate(parms);
        if (weapons.Count != 1)
        {
            Log.Error($"[Unique Melee Weapons] Expected 1 unique weapon for the warband quest, got {weapons.Count}.");
        }
        ThingWithComps weapon = weapons.FirstOrDefault() as ThingWithComps;

        // 3. Leader: a tribal melee chief, stripped and handed the unique (the AncientSoldier_Leader path).
        Pawn leader = PawnGenerator.GeneratePawn(UMW_QuestDefOf.Tribal_ChiefMelee, faction, siteTile);
        if (weapon != null)
        {
            leader.equipment.DestroyAllEquipment();
            leader.equipment.AddEquipment(weapon);
        }

        // 4. Band from the faction's Combat pawnGroupMaker at points/2 (floored to the faction's minimum).
        IEnumerable<Pawn> band = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Combat,
            faction = faction,
            tile = siteTile,
            points = Mathf.Max(points / 2f, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat) * 1.05f)
        });
        List<Pawn> allPawns = new List<Pawn> { leader };
        allPawns.AddRange(band);

        // 5. Theme the site map as a ruined, abandoned tribal settlement (no defenders of its own --
        //    generatePawns=false -- so only our band occupies it). Its footprint lands in "SettlementRect".
        siteTile.Tile.AddMutator(UMW_QuestDefOf.AbandonedColonyTribal);

        // 6. Spawn the claimable site, owned by our faction.
        Site site = QuestGen_Sites.GenerateSite(new[]
        {
            new SitePartDefWithParams(UMW_QuestDefOf.UMW_WarbandCamp, new SitePartParams())
        }, siteTile, faction, hiddenSitePartsPossible: false, null, WorldObjectDefOf.ClaimableSite);
        quest.SpawnWorldObject(site);

        slate.Set("site", site);
        slate.Set("LEADER", leader);
        slate.Set("WEAPON", weapon);
        slate.Set("warbandList", PawnUtility.PawnKindsToLineList(allPawns.Select(p => p.kindDef), "  - ", ColoredText.ThreatColor));
        slate.Set("atLandmark", site.Tile.Tile.Landmark != null);
        slate.Set("landmarkName", site.Tile.Tile.Landmark?.name ?? "");

        // 7. Spawn the band into the abandoned settlement on map-gen and make their defending lord.
        string mapGeneratedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
        string allEnemiesDefeatedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.AllEnemiesDefeated");
        string mapRemovedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
        quest.AddPart(new QuestPart_SpawnWarband(allPawns, mapGeneratedSignal));

        // 8. 30-day timeout; success on clearing the camp; quest ends (Unknown) if the map is abandoned.
        quest.WorldObjectTimeout(site, TimeoutTicks);
        quest.Delay(TimeoutTicks, delegate
        {
            QuestGen_End.End(quest, QuestEndOutcome.Fail);
        });
        quest.End(QuestEndOutcome.Success, 0, null, allEnemiesDefeatedSignal);
        quest.End(QuestEndOutcome.Unknown, 0, null, mapRemovedSignal);
    }

    // Beggars-style temporary faction: hostile to the player, neutral to everyone else, hidden, and
    // flagged temporary so FactionManager auto-removes it once unreferenced (quest ended + site destroyed).
    private static Faction GenerateTemporaryWarbandFaction()
    {
        List<FactionRelation> relations = new List<FactionRelation>();
        foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
        {
            relations.Add(new FactionRelation
            {
                other = other,
                kind = other.IsPlayer ? FactionRelationKind.Hostile : FactionRelationKind.Neutral
            });
        }
        Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(UMW_QuestDefOf.UMW_Warband, relations, hidden: true);
        faction.temporary = true;
        Find.FactionManager.Add(faction);
        return faction;
    }
}
