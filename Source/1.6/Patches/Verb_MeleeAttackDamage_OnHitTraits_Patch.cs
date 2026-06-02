using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

/// <summary>
/// Fires unique-weapon trait on-hit effects after a landed melee blow.
/// <para>
/// <c>Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget</c> is the concrete melee-weapon hit path; it
/// runs once per non-dodged swing and returns the resolved <c>DamageResult</c>. We postfix it,
/// confirm the blow actually wounded a living pawn, then replay each equipped trait's
/// <see cref="MeleeTraitEffectExtension"/> effects. Gating on <c>EquipmentSource</c> having a
/// <c>CompUniqueWeapon</c> makes natural attacks and non-unique weapons no-ops. This mirrors how
/// vanilla <c>Verb</c> already reaches trait data off the verb (the <c>burstShotSpeedMultiplier</c>
/// loop). Discovered automatically by <c>PatchAll()</c> in <c>UniqueMeleeWeaponsMod</c>.
/// </para>
/// </summary>
[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
public static class Verb_MeleeAttackDamage_OnHitTraits_Patch
{
    public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, DamageWorker.DamageResult __result)
    {
        // Only on a hit that actually wounded a living, spawned pawn — deflected/missed blows do nothing.
        if (__result == null || !__result.wounded)
        {
            return;
        }
        if (!(target.Thing is Pawn victim) || victim.Dead || !victim.Spawned)
        {
            return;
        }

        ThingWithComps weapon = __instance.EquipmentSource;
        CompUniqueWeapon comp = weapon?.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return;
        }

        Pawn attacker = __instance.CasterPawn;
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
                if (Rand.Chance(effect.chance))
                {
                    effect.Apply(victim, attacker, weapon);
                }
            }
        }
    }
}
