using System.Collections.Generic;
using System.Linq;
using Verse;

namespace UniqueMeleeWeapons;

// The set of weapons this mod owns, and the single answer to "is this def one of ours?".
//
// Identity is the UMW_UniqueMelee thingSetMakerTag rather than a defName prefix or the mod's
// ContentPack, because that tag is already the contract our reward pool selects on (see
// ThingSetMaker_UMWUnique and UMW_Reward_UniqueWeapon) — one marker, one meaning. Changing Tag means
// changing the weapon defs and our pool def's filter in lockstep.
//
// All is derived from the DefDatabase rather than hand-listed, which is what makes every consumer
// DLC-correct with no ModsConfig check: a MayRequire-gated weapon (the Royalty axe and warhammer) never
// enters the DefDatabase without its DLC, so it is simply absent — including from the per-weapon settings
// rows. Ordered by label so those rows have a stable, alphabetical order per language. The cache is
// rebuilt by UMW_Startup.Run on EVERY play-data load (via the CallAll postfix — see
// Patches/StaticConstructorOnStartupUtility_CallAll_Patch.cs), so after an in-process reload such as a
// mid-session language change All already holds the new DefDatabase's instances and the rows follow the
// active language. The list is only as fresh as the last load, so consumers must not long-term-cache
// All's instances themselves; everything load-bearing keys on tags or defNames anyway (IsOurs reads the
// passed def live; the settings set stores defNames).
public static class UniqueWeaponDefs
{
    // Must match the thingSetMakerTag on every *_Unique weapon def and the allow-filter in
    // UMW_Reward_UniqueWeapon.
    public const string Tag = "UMW_UniqueMelee";

    private static List<ThingDef> all;

    public static List<ThingDef> All => all ??= Build();

    public static void Rebuild()
    {
        all = Build();
    }

    public static bool IsOurs(BuildableDef def)
    {
        return def is ThingDef thing && thing.thingSetMakerTags?.Contains(Tag) == true;
    }

    private static List<ThingDef> Build()
    {
        return DefDatabase<ThingDef>.AllDefs.Where(d => IsOurs(d)).OrderBy(d => d.label).ToList();
    }
}
