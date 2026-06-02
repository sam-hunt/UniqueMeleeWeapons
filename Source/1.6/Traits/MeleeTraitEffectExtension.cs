using System.Collections.Generic;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Attaches on-hit melee behaviour to a unique-weapon <c>WeaponTraitDef</c>.
/// <para>
/// Vanilla's mechanically interesting trait fields — <c>damageDefOverride</c>,
/// <c>extraDamages</c>, <c>additionalStoppingPower</c>, <c>burstShot*</c> — are read
/// <em>only</em> by <c>Projectile</c>/<c>Verb_LaunchProjectile</c>, so they silently do nothing on a
/// melee weapon. To give the melee mechanism categories (Bladed/Pointed/Blunt) real, distinct
/// effects we layer them on via this <see cref="DefModExtension"/> instead of subclassing
/// <c>WeaponTraitDef</c> — the trait stays an ordinary def, so vanilla generation, naming, and the
/// stat system keep working untouched. The effects are fired by the
/// <c>Verb_MeleeAttackDamage</c> postfix in <c>UniqueMeleeWeapons.Patches</c>.
/// </para>
/// </summary>
public class MeleeTraitEffectExtension : DefModExtension
{
    /// <summary>Effects rolled (each against its own <see cref="MeleeOnHitEffect.chance"/>) on every
    /// landed, wounding melee hit by a weapon carrying this trait. Polymorphic — list items declare a
    /// concrete <c>Class=</c> (e.g. <see cref="MeleeOnHitEffect_ExtraDamage"/>).</summary>
    public List<MeleeOnHitEffect> onHitEffects;
}
