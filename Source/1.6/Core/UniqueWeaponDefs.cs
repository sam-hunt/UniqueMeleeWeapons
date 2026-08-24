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
// The tag is a PUBLISHED CROSS-MOD CONTRACT, like the stuff_adjective grammar symbol and
// TraitEffectLinesExtension: a third-party melee unique (a ThingDef with CompUniqueWeapon) that adds
// UMW_UniqueMelee to its thingSetMakerTags opts into FULL membership — eligible for the warband quest's
// reward pool (UMW_Reward_UniqueWeapon allow-lists the tag), excluded from Odyssey's unique pools
// (ThingSetMaker_UMWUnique's candidate set is comp-minus-tag), given a per-weapon toggle in our mod
// settings, and counted by AnyWeaponEnabled. It is all-or-nothing by design — one marker, one meaning —
// so don't rename the tag and don't add partial-membership carve-outs keyed on it.
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
