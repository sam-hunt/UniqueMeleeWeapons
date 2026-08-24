namespace UniqueMeleeWeapons;

// Startup work that must run against the CURRENT DefDatabase: the def cache and the
// settings-driven def-field writes. Runs once per play-data LOAD, not once per process —
// deliberately NOT [StaticConstructorOnStartup], whose once-per-process contract is too weak
// for def-mutating work: an in-process reload (a mid-session language change) replaces every
// def instance and a type initializer never re-runs. Invoked instead by
// Patches/StaticConstructorOnStartupUtility_CallAll_Patch.cs at exactly the moment static
// ctors run — after defs, DefOf rebinding and full language injection — on every load; that
// file carries the verified load ordering, the DoPlayLoad trap, and the hot-reload caveat.
//
// Everything called here must stay idempotent (reloads and re-patching make it fire more than
// once per process).
public static class UMW_Startup
{
    public static void Run()
    {
        // Rebuild the set of weapons we own (per-weapon settings rows, pool filtering, the
        // warband quest gate) from the fresh DefDatabase, so the cached instances — and the
        // rows' label sort order — follow the active language.
        UniqueWeaponDefs.Rebuild();
        UniqueMeleeWeaponsMod.Settings.ApplyWarbandQuestWeight();
        UniqueMeleeWeaponsMod.Settings.ApplyAbilityTuning();
        // After Rebuild: the trader def-writes iterate UniqueWeaponDefs.All.
        UniqueMeleeWeaponsMod.Settings.ApplyTraderStock();
        TraitEffectSummary.AttachToTraits();
    }
}
