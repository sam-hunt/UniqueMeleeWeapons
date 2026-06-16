using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Situational mood for carrying a UMW_BloodSoaked unique weapon as primary equipment. Backs
// both the penalty thought UMW_BloodSoakedWeapon and the bloodlust buff thought
// UMW_BloodSoakedWeapon_Bloodlust — each gets its own worker instance via def.Worker, and
// both stages are index 0, so the same ActiveAtStage(0) serves either def.
//
// This worker reports only the situation — "is the wielder's primary weapon blood-soaked?".
// All personality routing rides def fields, which Thought.MoodOffset /
// ThoughtUtility.CanGetThought honor for situational thoughts too (verified by decompile):
// the penalty's exemptions (Bloodlust / Psychopath / VTE Desensitized / World-weary, and the Biotech
// hemogenic gene) ride nullifyingTraits/nullifyingGenes — MoodOffset returns 0
// when ThoughtUtility.ThoughtNullified is true; the buff's gate rides requiredTraits —
// CanGetThought (called by SituationalThoughtHandler.TryCreateThought) returns false for
// non-bloodlusters. So no trait/gene check is needed here; mirrors how vanilla
// ThoughtWorker_ColonistLeftUnburied checks only the situation and lets the def exempt
// psychopaths.
public class ThoughtWorker_BloodSoakedWeapon : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        ThingWithComps weapon = p.equipment?.Primary;
        if (weapon == null)
        {
            return ThoughtState.Inactive;
        }

        CompUniqueWeapon comp = weapon.GetComp<CompUniqueWeapon>();
        if (comp?.TraitsListForReading.Contains(UMW_DefOf.UMW_BloodSoaked) != true)
        {
            return ThoughtState.Inactive;
        }

        return ThoughtState.ActiveAtStage(0);
    }
}
