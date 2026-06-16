using System.Collections.Generic;
using Verse;

namespace UniqueMeleeWeapons;

// Attaches on-hit melee behaviour to a unique-weapon WeaponTraitDef.
//
// Vanilla's mechanically interesting trait fields — damageDefOverride,
// extraDamages, additionalStoppingPower, burstShot* — are read
// only by Projectile/Verb_LaunchProjectile, so they silently do nothing on a
// melee weapon. To give the melee mechanism categories (Bladed/Pointed/Blunt) real, distinct
// effects we layer them on via this DefModExtension instead of subclassing
// WeaponTraitDef — the trait stays an ordinary def, so vanilla generation, naming, and the
// stat system keep working untouched. The effects are fired by the
// Verb_MeleeAttackDamage postfix in UniqueMeleeWeapons.Patches.
public class MeleeTraitEffectExtension : DefModExtension
{
    // Effects rolled (each against its own MeleeOnHitEffect.chance) on every
    // landed, wounding melee hit by a weapon carrying this trait. Polymorphic — list items declare a
    // concrete Class= (e.g. MeleeOnHitEffect_ExtraDamage).
    public List<MeleeOnHitEffect> onHitEffects;
}
