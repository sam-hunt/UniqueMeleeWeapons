using Verse;
using Verse.AI;

namespace UniqueMeleeWeapons;

// One on-hit effect carried by a MeleeTraitEffectExtension. Subclasses are selected in
// XML via <li Class="UniqueMeleeWeapons.MeleeOnHitEffect_...">. Apply runs
// after a landed, wounding melee hit (gated by the verb postfix); chance is rolled per
// hit by the caller, so implementations don't repeat the roll.
public abstract class MeleeOnHitEffect
{
    // Per-hit proc chance (0–1). Rolled by the postfix before Apply is called.
    public float chance = 1f;

    // victim: The pawn that was struck (already confirmed alive and spawned).
    // attacker: The wielder (may be null for non-pawn casters).
    // weapon: The unique weapon that landed the hit.
    public abstract void Apply(Pawn victim, Pawn attacker, ThingWithComps weapon);
}

// Applies an extra DamageInfo to the victim — our melee stand-in for the ranged-only
// extraDamages. A low-AP Cut reads as a bleeding gash (Bladed); a high-AP Stab
// reads as an armour-punching follow-through (Pointed). Re-entrancy is safe: this calls
// Thing.TakeDamage directly, not the melee verb the postfix hooks.
public class MeleeOnHitEffect_ExtraDamage : MeleeOnHitEffect
{
    // Damage type of the extra hit (e.g. Cut, Stab). Resolved from the def database.
    public DamageDef def;
    // Damage amount before the usual ±20% combat variance is applied here.
    public float amount = 1f;
    // Armor penetration fraction (0–1+). Stored verbatim on the DamageInfo;
    // there is no "auto" sentinel, so always set a sensible value.
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

// Briefly stuns the victim — the Blunt-family "maul/concussion" effect. Mirrors vanilla, which
// already derives a blunt stun of damageAmount * 30 ticks in StunHandler.Notify_DamageApplied;
// StunFor takes the max of the current and requested duration, so procs can't stack into a lock.
public class MeleeOnHitEffect_Stun : MeleeOnHitEffect
{
    // Stun duration in ticks (60 = 1 second).
    public int ticks = 60;

    public override void Apply(Pawn victim, Pawn attacker, ThingWithComps weapon)
    {
        victim.stances?.stunner?.StunFor(ticks, attacker, addBattleLog: false);
    }
}

// Tries to push the victim into a mental state on a wounding hit — the "dread"/terror effect (used by
// the Blood-soaked trait's PanicFlee). Defaults to humanlikes only, since animals and
// mechanoids don't panic at a fearsome blade. TryStartMentalState is non-forced, so it
// respects the usual guards (already broken, can't take the state, etc.) and simply no-ops when the
// roll can't apply.
public class MeleeOnHitEffect_MentalState : MeleeOnHitEffect
{
    // Mental state to start, resolved from the def database (e.g. PanicFlee, Berserk).
    public MentalStateDef stateDef;
    // If true (default), only humanlike victims are affected.
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
