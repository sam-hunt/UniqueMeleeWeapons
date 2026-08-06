using Verse;

namespace UniqueMeleeWeapons;

// Runs once on the main thread after all defs are loaded, translations injected and DefOf fields
// injected — the earliest point where settings that override def fields can be
// applied. (The Mod constructor is too early: it runs while mod assemblies are
// still loading, before any def exists.)
//
// Once per PROCESS, not per play-data load: StaticConstructorOnStartupUtility.CallAll goes through
// RuntimeHelpers.RunClassConstructor, and a type initializer never runs twice. An in-process
// play-data reload (mid-session language change, dev-mode def hot reload) rebuilds the DefDatabase
// WITHOUT re-running this, so until the next restart the def-field overrides revert to their
// shipped XML values (re-applied on the next settings-window close, which calls the same Apply
// methods) and the caches built here keep the previous DefDatabase's def instances (harmless to the
// pool/stuff patches, which key on tags and defNames, not cached references).
// (Decompile-verified, RimWorld 1.6.)
[StaticConstructorOnStartup]
public static class UMW_Startup
{
    static UMW_Startup()
    {
        // Cache the set of weapons we own (per-weapon settings rows, pool filtering). Eager rather than
        // lazy only so it is built at the same well-defined point as the rest of this startup work;
        // there is no rebuild-on-reload angle, since this never re-runs (see the class comment).
        UniqueWeaponDefs.Rebuild();
        // Warm the relevant-stat gate (see its comment) at the same well-defined point; reload-safe
        // because it is keyed on defNames, so no rebuild-on-reload angle either.
        _ = Patches.StatWorker_UniqueTraitStatOffsets.RelevantStatNames;
        UniqueMeleeWeaponsMod.Settings.ApplyWarbandQuestWeight();
        UniqueMeleeWeaponsMod.Settings.ApplyAbilityTuning();
        TraitEffectSummary.AttachToTraits();
    }
}
