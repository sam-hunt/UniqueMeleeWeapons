using System.Collections.Generic;
using Verse;

namespace UniqueMeleeWeapons;

// Converts a unique weapon's base melee damage in place — rerouting one DamageDef
// to another on the hit the weapon would already deal, rather than stacking an extra hit the way
// MeleeOnHitEffect_ExtraDamage does. Attached to a WeaponTraitDef and applied by the
// DamageConversion postfix on Verb_MeleeAttackDamage.DamageInfosToApply. UMW_Serrated
// uses it to reroute Cut → the bleedier, scar-prone UMW_Cut_Ragged — same damage quantity,
// different wound. Keeping it a DefModExtension + postfix leaves the trait an ordinary def
// (vanilla generation/naming/stats untouched), mirroring MeleeTraitEffectExtension.
public class MeleeDamageConversionExtension : DefModExtension
{
    // Damage reroutes applied to the weapon's base melee hit. A hit whose DamageDef
    // matches no entry's DamageConversion.from passes through untouched.
    public List<DamageConversion> conversions;
}

// One from → to DamageDef reroute in a
// MeleeDamageConversionExtension.
public class DamageConversion
{
    public DamageDef from;
    public DamageDef to;
}
