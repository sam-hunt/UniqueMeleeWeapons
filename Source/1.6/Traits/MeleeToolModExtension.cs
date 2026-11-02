using System.Collections.Generic;
using Verse;

namespace UniqueMeleeWeapons;

// Per-tool combat modifier carried by a WeaponTraitDef and applied at runtime by the
// Verb_MeleeAttack_ToolMods patches. Lets a rolled trait change a SINGLE tool's armor
// penetration or damage independently of the rest of the weapon — something XML alone cannot do,
// because melee damage and AP both ride one weapon-wide stat (MeleeWeapon_DamageMultiplier) and a
// weapon's <tools> are shared by every instance of its def. Mirrors MeleeDamageConversionExtension
// (DefModExtension + postfix) so the trait stays an ordinary def.
public class MeleeToolModExtension : DefModExtension
{
    // One entry per tool group this trait modifies (e.g. one for Cut, one for Stab).
    public List<MeleeToolMod> mods;
}

// A modifier targeting the weapon's tools whose capacities intersect this entry's `capacities` or
// `alsoMatches` (an empty/absent `capacities` list matches every tool).
public class MeleeToolMod
{
    // Tool capacities this entry applies to (e.g. Cut, Stab). Empty/null = all of the weapon's tools.
    public List<ToolCapacityDef> capacities;

    // Further capacities matched exactly like `capacities` but left out of the info-card line, which
    // names only `capacities` (TraitEffectSummary). For weapon-specific variants of a named capacity,
    // so the line stays short and general: e.g. Demolish (the breach axe head) and VFE Pirates'
    // VFEP_GravityHammerAttack are both BluntBase damages, so a Blunt entry lists them here and
    // still reads "blunt". Only meaningful alongside a non-empty `capacities`.
    public List<ToolCapacityDef> alsoMatches;

    // Multiplies the matched tool's melee damage (1 = no change).
    public float damageFactor = 1f;

    // Raises the matched tool's armor penetration to at least this value (-1 = no floor). A FLAT
    // floor, compared against the fully quality- and stuff-scaled value: a rescue baseline that
    // lifts a poor roll and leaves a fine one alone. No shipped trait uses it since 2026-11: a unique
    // never rolls below Masterwork, so a flat floor sized against Normal-quality bases was dead on the
    // heavier weapons (see Razored/ArmorSpike/Flanged headers). Kept for third-party traits built on
    // this extension and for the CE compat root's patches, which remove AP fields by name.
    public float armorPenetrationFloor = -1f;

    // Raises the matched tool's armor penetration to at least this value TIMES the weapon's quality
    // factor for MeleeWeapon_DamageMultiplier (-1 = no floor). Stated in Normal-quality units: this
    // is how vanilla scales a tool's own explicit <armorPenetration> (VerbProperties.
    // AdjustedArmorPenetration multiplies it by that stat), so a value copied from a vanilla tool
    // makes the trait "this tool's edge at this weapon's quality" (Monomolecular's 0.9 is the
    // monosword's). Only the stat's quality part is used, never the weapon's live stat value, which
    // would fold in co-rolled traits' factors (HeadWeighted's x1.3 exists to cancel exactly that AP
    // side effect) and has no stuff part anyway. A weapon without CompQuality counts as Normal.
    // Where both floors are set the higher result wins.
    public float armorPenetrationQualityFloor = -1f;

    // Multiplies the matched tool's armor penetration (1 = no change). Applied before the floors.
    public float armorPenetrationFactor = 1f;

    // True when this entry has no capacity filter (applies to every tool).
    public bool AppliesToAllTools => capacities == null || capacities.Count == 0;

    // Whether this entry targets the given tool, by capacity intersection.
    public bool Matches(Tool tool)
    {
        if (AppliesToAllTools)
        {
            return true;
        }
        if (tool?.capacities == null)
        {
            return false;
        }
        for (int i = 0; i < tool.capacities.Count; i++)
        {
            if (capacities.Contains(tool.capacities[i]) || alsoMatches?.Contains(tool.capacities[i]) == true)
            {
                return true;
            }
        }
        return false;
    }
}
