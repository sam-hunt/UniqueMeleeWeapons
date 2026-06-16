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

// A modifier targeting the weapon's tools whose capacities intersect this entry's `capacities`
// (an empty/absent `capacities` list matches every tool).
public class MeleeToolMod
{
    // Tool capacities this entry applies to (e.g. Cut, Stab). Empty/null = all of the weapon's tools.
    public List<ToolCapacityDef> capacities;

    // Multiplies the matched tool's melee damage (1 = no change).
    public float damageFactor = 1f;

    // Raises the matched tool's armor penetration to at least this value (-1 = no floor).
    public float armorPenetrationFloor = -1f;

    // Multiplies the matched tool's armor penetration (1 = no change). Applied before the floor.
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
            if (capacities.Contains(tool.capacities[i]))
            {
                return true;
            }
        }
        return false;
    }
}
