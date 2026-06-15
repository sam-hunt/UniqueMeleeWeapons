using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Situational mood for carrying a <c>UMW_BloodSoaked</c> unique weapon as primary equipment. Backs
/// <em>both</em> the penalty thought <c>UMW_BloodSoakedWeapon</c> and the bloodlust buff thought
/// <c>UMW_BloodSoakedWeapon_Bloodlust</c> — each gets its own worker instance via <c>def.Worker</c>, and
/// both stages are index 0, so the same <c>ActiveAtStage(0)</c> serves either def.
/// <para>
/// This worker reports only the <em>situation</em> — "is the wielder's primary weapon blood-soaked?".
/// All <em>personality</em> routing rides def fields, which <c>Thought.MoodOffset</c> /
/// <c>ThoughtUtility.CanGetThought</c> honor for situational thoughts too (verified by decompile):
/// the penalty's exemptions (Bloodlust / Psychopath / VTE Desensitized / World-weary, and the Biotech
/// hemogenic gene) ride <c>nullifyingTraits</c>/<c>nullifyingGenes</c> — <c>MoodOffset</c> returns 0
/// when <c>ThoughtUtility.ThoughtNullified</c> is true; the buff's gate rides <c>requiredTraits</c> —
/// <c>CanGetThought</c> (called by <c>SituationalThoughtHandler.TryCreateThought</c>) returns false for
/// non-bloodlusters. So no trait/gene check is needed here; mirrors how vanilla
/// <c>ThoughtWorker_ColonistLeftUnburied</c> checks only the situation and lets the def exempt
/// psychopaths.
/// </para>
/// </summary>
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
        if (comp == null || !comp.TraitsListForReading.Contains(UMW_DefOf.UMW_BloodSoaked))
        {
            return ThoughtState.Inactive;
        }

        return ThoughtState.ActiveAtStage(0);
    }
}
