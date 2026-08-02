using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace UniqueMeleeWeapons;

// Spawns the warband into the site map on map-generation, then makes a defending lord for them.
//
// This is our faction-agnostic counterpart to vanilla RimWorld.QuestGen.QuestPart_SpawnPawnsInStructure,
// which we can't reuse for two reasons:
//   1. It hardcodes LordMaker.MakeNewLord(Faction.OfAncientsHostile, ...); ours must point at the
//      quest's temporary warband faction. We derive both the lord faction and the LordJob_SitePawns
//      defend-faction from pawns.First().Faction, so no faction is baked in.
//   2. It reads the "SpawnRect" map var, which only the AncientStructure mutator sets. Our site uses the
//      AbandonedColonyTribal mutator, whose worker (TileMutatorWorker_AbandonedColony) stores the ruined
//      settlement footprint in "SettlementRect" instead.
//
// Unlike the vanilla part we do NOT require spawn cells to be roofed: an abandoned tribal colony is built
// of small, deliberately-damaged huts (damageBuildings=true), so a roofed-only filter would often find too
// few cells. We keep the standable + reachable-to-map-edge checks so the band can't spawn sealed off.
public class QuestPart_SpawnWarband : QuestPart
{
    private List<Pawn> pawns;
    private string inSignal;

    public QuestPart_SpawnWarband()
    {
    }

    public QuestPart_SpawnWarband(List<Pawn> pawns, string inSignal)
    {
        this.pawns = pawns;
        this.inSignal = inSignal;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Deep);
        Scribe_Values.Look(ref inSignal, "inSignal");
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != inSignal)
        {
            return;
        }
        try
        {
            // Already spawned: the part stays alive on the quest after pawns.Clear(), so a
            // re-delivered MapGenerated signal (map regenerated on a later visit, or the list
            // scribed back empty) is a benign re-entry, not an error.
            if (pawns.NullOrEmpty())
            {
                return;
            }
            // No site map means the warband genuinely can't spawn; warn so the empty site is
            // diagnosable from the log. TryGetArg leaves arg null on failure, so one check covers
            // a missing SUBJECT and a MapParent whose map is gone.
            signal.args.TryGetArg("SUBJECT", out MapParent arg);
            if (arg?.Map == null)
            {
                Log.Warning($"[Unique Melee Weapons] Skipped spawning warband: signal '{signal.tag}' carried no map (SUBJECT={arg?.ToString() ?? "null"}).");
                return;
            }
            Map map = arg.Map;

            // The abandoned-settlement footprint, set during map-gen by TileMutatorWorker_AbandonedColony.
            // Fall back to a block around the map centre if the var is somehow absent.
            CellRect rect = MapGenerator.GetVar<CellRect>("SettlementRect");
            if (rect.Area <= 0)
            {
                rect = CellRect.CenteredOn(map.Center, 12).ClipInsideMap(map);
            }

            foreach (Pawn pawn in pawns)
            {
                if (!rect.TryFindRandomCell(out var cell, Validator))
                {
                    cell = CellFinder.RandomClosewalkCellNear(rect.CenterCell, map, 12);
                }
                GenSpawn.Spawn(pawn, cell, map);
            }

            LordMaker.MakeNewLord(pawns.First().Faction, new LordJob_SitePawns(pawns.First().Faction, rect.CenterCell, 180000), map, pawns);
            pawns.Clear();

            bool Validator(IntVec3 x)
            {
                if (!x.Standable(map))
                {
                    return false;
                }
                if (!map.generatorDef.isUnderground && !map.Tile.LayerDef.isSpace && !map.reachability.CanReachMapEdge(x, TraverseParms.For(TraverseMode.PassDoors)))
                {
                    return false;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Unique Melee Weapons] Failed to spawn warband in structure: {ex}");
        }
    }
}
