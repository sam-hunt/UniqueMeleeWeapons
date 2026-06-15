using Verse;
using Verse.AI;

namespace UniqueMeleeWeapons;

/// <summary>
/// One on-hit effect carried by a <see cref="MeleeTraitEffectExtension"/>. Subclasses are selected in
/// XML via <c>&lt;li Class="UniqueMeleeWeapons.MeleeOnHitEffect_..."&gt;</c>. <see cref="Apply"/> runs
/// after a landed, wounding melee hit (gated by the verb postfix); <see cref="chance"/> is rolled per
/// hit by the caller, so implementations don't repeat the roll.
/// </summary>
public abstract class MeleeOnHitEffect
{
    /// <summary>Per-hit proc chance (0–1). Rolled by the postfix before <see cref="Apply"/> is called.</summary>
    public float chance = 1f;

    /// <param name="victim">The pawn that was struck (already confirmed alive and spawned).</param>
    /// <param name="attacker">The wielder (may be null for non-pawn casters).</param>
    /// <param name="weapon">The unique weapon that landed the hit.</param>
    public abstract void Apply(Pawn victim, Pawn attacker, ThingWithComps weapon);
}

/// <summary>
/// Applies an extra <see cref="DamageInfo"/> to the victim — our melee stand-in for the ranged-only
/// <c>extraDamages</c>. A low-AP <c>Cut</c> reads as a bleeding gash (Bladed); a high-AP <c>Stab</c>
/// reads as an armour-punching follow-through (Pointed). Re-entrancy is safe: this calls
/// <c>Thing.TakeDamage</c> directly, not the melee verb the postfix hooks.
/// </summary>
public class MeleeOnHitEffect_ExtraDamage : MeleeOnHitEffect
{
    /// <summary>Damage type of the extra hit (e.g. <c>Cut</c>, <c>Stab</c>). Resolved from the def database.</summary>
    public DamageDef def;
    /// <summary>Damage amount before the usual ±20% combat variance is applied here.</summary>
    public float amount = 1f;
    /// <summary>Armor penetration fraction (0–1+). Stored verbatim on the <see cref="DamageInfo"/>;
    /// there is no "auto" sentinel, so always set a sensible value.</summary>
    public float armorPenetration = 0f;

    public override void Apply(Pawn victim, Pawn attacker, ThingWithComps weapon)
    {
        if (def == null)
        {
            return;
        }
        float dealt = Rand.Range(amount * 0.8f, amount * 1.2f);
        var dinfo = new DamageInfo(def, dealt, armorPenetration, -1f, attacker, null, weapon?.def);
        victim.TakeDamage(dinfo);
    }
}

/// <summary>
/// Briefly stuns the victim — the Blunt-family "maul/concussion" effect. Mirrors vanilla, which
/// already derives a blunt stun of <c>damageAmount * 30</c> ticks in <c>StunHandler.Notify_DamageApplied</c>;
/// <c>StunFor</c> takes the max of the current and requested duration, so procs can't stack into a lock.
/// </summary>
public class MeleeOnHitEffect_Stun : MeleeOnHitEffect
{
    /// <summary>Stun duration in ticks (60 = 1 second).</summary>
    public int ticks = 60;

    public override void Apply(Pawn victim, Pawn attacker, ThingWithComps weapon)
    {
        victim.stances?.stunner?.StunFor(ticks, attacker, addBattleLog: false);
    }
}

/// <summary>
/// Tries to push the victim into a mental state on a wounding hit — the "dread"/terror effect (used by
/// the Blood-soaked trait's <c>PanicFlee</c>). Defaults to humanlikes only, since animals and
/// mechanoids don't panic at a fearsome blade. <c>TryStartMentalState</c> is non-forced, so it
/// respects the usual guards (already broken, can't take the state, etc.) and simply no-ops when the
/// roll can't apply.
/// </summary>
public class MeleeOnHitEffect_MentalState : MeleeOnHitEffect
{
    /// <summary>Mental state to start, resolved from the def database (e.g. <c>PanicFlee</c>, <c>Berserk</c>).</summary>
    public MentalStateDef stateDef;
    /// <summary>If true (default), only humanlike victims are affected.</summary>
    public bool humanlikeOnly = true;

    public override void Apply(Pawn victim, Pawn attacker, ThingWithComps weapon)
    {
        if (stateDef == null)
        {
            return;
        }
        if (humanlikeOnly && !victim.RaceProps.Humanlike)
        {
            return;
        }
        victim.mindState?.mentalStateHandler?.TryStartMentalState(stateDef);
    }
}
