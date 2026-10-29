using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// The gizmo for an ability whose CompAbilityEffect_GroundShockwave carries travelsThroughGround.
// Selected per-ability by AbilityDef.gizmoClass (a plain Type field that Ability.GetGizmos feeds to
// Activator.CreateInstance), so this needs no Harmony patch — see Defs/AbilityDefs/Earthshake.xml.
//
// Exists only to keep the hover preview honest, which this repo treats as a rule (see 1.6/Defs/CLAUDE.md): the
// burst is flood-filled through conducting ground, so a plain radius ring would promise cells across a
// gap that the shock never reaches. Vanilla cannot draw that. Command_Ability.GizmoUpdateOnMouseover
// hands off to VerbProperties.DrawRadiusRing, whose only cell filter is a hardcoded
// GenSight.LineOfSight test behind drawHighlightWithLineOfSight — there is no hook for an arbitrary
// predicate, so the ring has to be drawn directly rather than configured. GenDraw.DrawRadiusRing's
// predicate overload is public and is the same one vanilla's line-of-sight path uses, so this is the
// vanilla drawing routine with a different filter, not a reimplementation of it.
//
// The preview and the burst call the SAME CompAbilityEffect_GroundShockwave.GroundConnectedCells with
// the same radius read off the same comp. That sharing is the point: it makes the ring structurally
// unable to disagree with the effect, rather than relying on two copies being kept in step.
public class Command_Ability_GroundShockwave : Command_Ability
{
    // Rebuilt every frame the cursor rests on the gizmo. Static so the per-frame cost is a ~45-cell
    // refill with no allocation; the predicate below captures only this static, so the compiler caches
    // the delegate too. Cheaper than the GenSight raycast-per-cell that vanilla's own
    // drawHighlightWithLineOfSight does on the same code path.
    private static readonly HashSet<IntVec3> previewCells = new HashSet<IntVec3>();

    // Must match Command_Ability's signature: Ability.GetGizmos constructs gizmoClass via
    // Activator.CreateInstance(def.gizmoClass, this, pawn).
    public Command_Ability_GroundShockwave(Ability ability, Pawn pawn)
        : base(ability, pawn)
    {
    }

    public override void GizmoUpdateOnMouseover()
    {
        if (!TryDrawGroundRing())
        {
            // Not a ground shockwave after all (no comp, feature off, off-map): let vanilla draw its
            // ordinary ring. Note base also calls OnGizmoUpdate, so this must not fall through.
            base.GizmoUpdateOnMouseover();
            return;
        }

        // The other half of what base does, which is unrelated to the ring and still wanted.
        ability.OnGizmoUpdate();
    }

    private bool TryDrawGroundRing()
    {
        CompAbilityEffect_GroundShockwave comp = GroundShockwaveComp();
        if (comp?.TravelsThroughGround != true)
        {
            return false;
        }
        if (!(ability.verb is Verb_CastAbility verb) || verb.caster == null)
        {
            return false;
        }

        // The caster's own map rather than Find.CurrentMap (which is what vanilla's DrawRadiusRing
        // uses): terrain is per-map, so reading the wrong one would fill against the wrong ground.
        Map map = verb.caster.Map;
        if (map == null)
        {
            return false;
        }

        IntVec3 center = verb.caster.Position;
        CompAbilityEffect_GroundShockwave.GroundConnectedCells(center, map, comp.Radius, previewCells);
        GenDraw.DrawRadiusRing(center, comp.Radius, Color.white, CellIsReached);
        return true;
    }

    private static bool CellIsReached(IntVec3 cell)
    {
        return previewCells.Contains(cell);
    }

    private CompAbilityEffect_GroundShockwave GroundShockwaveComp()
    {
        List<CompAbilityEffect> comps = ability?.EffectComps;
        if (comps == null)
        {
            return null;
        }
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is CompAbilityEffect_GroundShockwave shockwave)
            {
                return shockwave;
            }
        }
        return null;
    }
}
