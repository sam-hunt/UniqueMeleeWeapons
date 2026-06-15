using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Static handles for this mod's own defs that C# needs to reference by identity. Populated by
/// RimWorld at startup (fields match defName). Only add defs the code actually looks up.
/// </summary>
[DefOf]
public static class UMW_DefOf
{
    /// <summary>The blood-soaked Melee trait — looked up by <see cref="ThoughtWorker_BloodSoakedWeapon"/>
    /// to decide whether the wielder's primary weapon carries it.</summary>
    public static WeaponTraitDef UMW_BloodSoaked;

    static UMW_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(UMW_DefOf));
    }
}
