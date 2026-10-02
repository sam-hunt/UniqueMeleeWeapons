using System.Collections.Generic;
using System.Linq;
using RimWorld;
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

    // Highest weapon techLevel the tribal consumers (warband quest, tribal trader stock) may hand out; the
    // outlander trader stock starts one step above it (OutlanderTechFloor below).
    // Medieval, not the Warband faction's own Neolithic: it is exactly the ceiling of the pre-VFEP
    // roster (knife/spear/breach axe Neolithic; longsword/gladius/mace/axe/warhammer Medieval), so it
    // changes nothing for them and keeps Industrial uniques (VFEP's warcasket weapons, equippable only
    // in a warcasket) away from tribals. Consumers filter with FitsTribal (a validator), never with
    // ThingSetMakerParams.techLevel, which also down-weights sub-cap Neolithic gear x0.1 (see
    // QuestNode_Root_Warband.RunInt).
    public const TechLevel TribalTechCap = TechLevel.Medieval;

    // Lowest weapon techLevel the outlander consumers (outlander settlement, combat supplier caravan and
    // trade ship stock) may hand out. Defined as one step above TribalTechCap rather than as a second
    // literal so the two bands PARTITION the roster: every unique is stocked by exactly one side, so
    // neither dilutes its pool with the other's stock and the choice of which trader to visit stays
    // meaningful (tribals deal in the medieval-and-below relics, outlanders in industrial-and-up gear
    // such as VFEP's warcasket pair). A roster with nothing at or above the floor (no VFEP, no
    // third-party tagged unique) leaves the outlander toggles inert and hidden (Settings_Traders).
    public const TechLevel OutlanderTechFloor = (TechLevel)((int)TribalTechCap + 1);

    public static bool FitsTribal(ThingDef def) => def.techLevel <= TribalTechCap;

    public static bool FitsOutlander(ThingDef def) => def.techLevel >= OutlanderTechFloor;

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
        return DefDatabase<ThingDef>.AllDefs.Where(d => IsOurs(d) && LoadedIntact(d)).OrderBy(d => d.label).ToList();
    }

    // A tagged def that lost its ParentName at XML load arrives with none of the inherited fields: no
    // category, tools, stats or techLevel (the game logs "Could not find parent node", then resolves
    // the node as a root and loads it anyway; decompile-verified 1.6). That happens when the base
    // weapon's mod loads AFTER us, which About.xml's loadAfter only warns about: the VFEP warcasket
    // pair is the live case, since a player who already had us above VFE Pirates keeps that order
    // when the pair ships. Vanilla's ThingSetMakerUtility.CanGenerate already rejects a
    // category-less def, but our own consumers read All directly (the trader stock generator, the
    // per-weapon settings rows, AnyWeaponEnabled), so the def is dropped here with an error naming
    // the fix. Logged once per play-data load (Build runs from UMW_Startup.Run).
    private static bool LoadedIntact(ThingDef def)
    {
        if (def.category == ThingCategory.Item)
        {
            return true;
        }
        Log.Error($"[Unique Melee Weapons] {def.defName} loaded without its base weapon (no category, tools or " +
                  "stats) and is excluded from every pool and setting. Its base weapon's mod must load before " +
                  "Unique Melee Weapons: move Unique Melee Weapons below it in the mod list and restart.");
        return false;
    }
}
