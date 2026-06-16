using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Static handles for this mod's own defs that C# needs to reference by identity. Populated by
// RimWorld at startup (fields match defName). Only add defs the code actually looks up.
[DefOf]
public static class UMW_DefOf
{
    // The blood-soaked Melee trait — looked up by ThoughtWorker_BloodSoakedWeapon
    // to decide whether the wielder's primary weapon carries it.
    public static WeaponTraitDef UMW_BloodSoaked;

    static UMW_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(UMW_DefOf));
    }
}
