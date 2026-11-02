using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Fires unique-weapon trait on-hit effects after a landed melee blow.
//
// Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget is the concrete melee-weapon hit path; it
// runs once per non-dodged swing and returns the resolved DamageResult. We postfix it,
// confirm the blow actually wounded a living pawn, then replay each equipped trait's
// MeleeTraitEffectExtension effects. Gating on EquipmentSource having a
// CompUniqueWeapon makes natural attacks and non-unique weapons no-ops. This mirrors how
// vanilla Verb already reaches trait data off the verb (the burstShotSpeedMultiplier
// loop). Discovered automatically by PatchAll() in UniqueMeleeWeaponsMod.
//
// The body lives in MeleeOnHitTraitResolver so the Combat Extended hook on CE's sibling verb
// (Patches/CombatExtended/CombatExtended_MeleePatches.cs) runs the identical logic off a plain
// Verb: CE's verb is not a Verb_MeleeAttackDamage, so this patch never fires during a CE swing.
[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
public static class Verb_MeleeAttackDamage_OnHitTraits_Patch
{
    public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, DamageWorker.DamageResult __result)
    {
        MeleeOnHitTraitResolver.Fire(__instance, target, __result?.wounded == true);
    }
}

// The shared on-hit body: given the verb that swung, its target and whether the swing wounded,
// rolls every on-hit effect of every trait on the verb's unique weapon. Reads nothing beyond the
// Verb base class, so it serves vanilla's damage verb and CE's alike. The caller decides
// `wounded` because the two verbs report it differently: vanilla returns the one DamageResult of
// the one blow, so its `wounded` flag is the answer; CE returns the LAST entry's result of a
// multi-entry swing (its blunt crit appends a Stun entry whose result is never `wounded`) with
// only `totalDamageDealt` summed, so the CE hook reads the sum instead.
internal static class MeleeOnHitTraitResolver
{
    internal static void Fire(Verb verb, LocalTargetInfo target, bool wounded)
    {
        // Only on a hit that actually wounded a living, spawned pawn — deflected/missed blows do nothing.
        if (!wounded)
        {
            return;
        }
        if (!(target.Thing is Pawn victim) || victim.Dead || !victim.Spawned)
        {
            return;
        }

        ThingWithComps weapon = verb.EquipmentSource;
        CompUniqueWeapon comp = weapon?.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return;
        }

        Pawn attacker = verb.CasterPawn;
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            var ext = traits[i].GetModExtension<MeleeTraitEffectExtension>();
            if (ext?.onHitEffects == null)
            {
                continue;
            }
            for (int j = 0; j < ext.onHitEffects.Count; j++)
            {
                MeleeOnHitEffect effect = ext.onHitEffects[j];
                if (effect.fleshOnly && !victim.RaceProps.IsFlesh)
                {
                    continue; // e.g. Opiated's sedative has nothing to act on in a mechanoid
                }
                if (Rand.Chance(effect.chance))
                {
                    effect.Apply(victim, attacker, weapon);
                }
            }
        }
    }
}
