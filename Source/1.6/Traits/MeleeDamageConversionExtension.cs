using System.Collections.Generic;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Converts a unique weapon's <em>base</em> melee damage in place — rerouting one <see cref="DamageDef"/>
/// to another on the hit the weapon would already deal, rather than stacking an extra hit the way
/// <see cref="MeleeOnHitEffect_ExtraDamage"/> does. Attached to a <c>WeaponTraitDef</c> and applied by the
/// <c>DamageConversion</c> postfix on <c>Verb_MeleeAttackDamage.DamageInfosToApply</c>. <c>UMW_Serrated</c>
/// uses it to reroute <c>Cut</c> → the bleedier, scar-prone <c>UMW_Cut_Ragged</c> — same damage quantity,
/// different wound. Keeping it a <see cref="DefModExtension"/> + postfix leaves the trait an ordinary def
/// (vanilla generation/naming/stats untouched), mirroring <see cref="MeleeTraitEffectExtension"/>.
/// </summary>
public class MeleeDamageConversionExtension : DefModExtension
{
    /// <summary>Damage reroutes applied to the weapon's base melee hit. A hit whose <see cref="DamageDef"/>
    /// matches no entry's <see cref="DamageConversion.from"/> passes through untouched.</summary>
    public List<DamageConversion> conversions;
}

/// <summary>One <c>from</c> → <c>to</c> <see cref="DamageDef"/> reroute in a
/// <see cref="MeleeDamageConversionExtension"/>.</summary>
public class DamageConversion
{
    public DamageDef from;
    public DamageDef to;
}
