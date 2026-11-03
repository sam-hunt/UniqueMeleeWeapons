# C# guidance (`Source/1.6/`)

Rules that constrain more than one file. Each file's header is the full story for its own
mechanism; a pointer here says which header. Cross-cutting conventions (namespaces, warnings as
errors, localization, mod detection, the published contracts) are in the root `CLAUDE.md`.

## Patching

- `PatchAll()` in `Core/UniqueMeleeWeaponsMod.cs` picks up every `[HarmonyPatch]` class. Three
  patch files deliberately carry no attribute and are applied conditionally, so an install with no
  consumer places no detour at all: `ShaderDatabase_LoadShader_Fallback_Patch` (from the Mod ctor
  while VFEP is active, because the shader loader runs in def `PostLoad`, before `CallAll`),
  `PawnRenderUtility_DrawCarriedWeapon_Patch` (from `UMW_Startup.Run`, only if a def consumes its
  extension) and the Combat Extended hooks under `Patches/CombatExtended/` (from `UMW_Startup.Run`,
  only while CE is active; `CombatExtendedCompat.cs` owns the lifecycle, one file per hook beside
  it). Their headers say why; don't "tidy" them back into `PatchAll`.
- **Patch-timing hazard (foreign targets):** `PatchAll()` runs from the `Mod` ctor, before any defs
  load. Applying a detour JIT-compiles the target and runs its declaring type's static ctor, so a
  patch on ANOTHER MOD's method can permanently break that mod when its cctor resolves defs (the
  BetterTradersGuild v1.1.0 CWTL incident). The only foreign targets are the Combat Extended hooks,
  which follow the rule: resolved by name from `UMW_Startup.Run` (after every mod's static ctor),
  a miss logs once and degrades, and any def they cache is re-resolved on every load. Copy that
  shape for the next foreign target; worked example also in BetterTradersGuild's
  `Core/DeferredModPatches.cs`.
- **The vanilla melee verb and CE's are siblings, not parent and child.** A patch on
  `Verb_MeleeAttackDamage` never fires during a Combat Extended swing. A new melee trait mechanism
  that needs a verb hook needs both: put the body in a shared resolver (the on-hit and conversion
  patches are the pattern) and add a sibling `Verb_MeleeAttackCE_*_Patch.cs` under
  `Patches/CombatExtended/` wired into `CombatExtendedCompat` (its header lists the three members
  a hook file exposes), or the trait ships inert under CE while its info-card line still promises
  the effect.
- **A def is kept out of the pools by filtering `ThingSetMakerUtility.CanGenerate`, never by
  removing the def.** It is the one choke point every `ThingSetMaker` funnels through, so the
  per-weapon toggles need only `Patches/ThingSetMakerUtility_CanGenerate_Patch.cs`, and estimates,
  "can this maker generate?" checks and saves that already contain the weapon stay consistent. The
  wood-free material roll filters its own single choke point the same way
  (`Patches/GenStuff_ExcludeWoodStuff_Patch.cs`).
- **Tribal consumers are tech-capped by validator, never by `ThingSetMakerParams.techLevel`**,
  which also down-weights sub-cap Neolithic gear x0.1. Use `UniqueWeaponDefs.FitsTribal` as a
  `validator` (quest) or `maxTechLevelGenerate` (our stock generator); the cap and floor rationale
  sits on the constants in `Core/UniqueWeaponDefs.cs`. Other pools stay uncapped by decision.

## Startup and load

- **Startup def-writes and def caches re-run on every play-data load, not once per process.** A
  mid-session language change reloads all play data in-process and replaces every def instance;
  `[StaticConstructorOnStartup]` never re-runs, so anything it wrote onto defs goes stale. All such
  work lives in `Core/UMW_Startup.cs` (`Run` must stay idempotent), invoked from
  `Patches/StaticConstructorOnStartupUtility_CallAll_Patch.cs`, whose header carries the verified
  load ordering and the traps.
- **Never evaluate a stat inside `ExposeData`** (any mode, including `PostLoadInit`): a stat read
  runs every mod's stat patches, and one that lazily loads its settings on first use opens a second
  Scribe session mid-load, which force-stops the real one and aborts the load (issue #2, 1.4.0 to
  1.4.1). Queue stat-dependent load repairs through `LongEventHandler.ExecuteWhenFinished`;
  `Things/UniqueMeleeWeapon.cs` is the pattern, including the guard its callback needs.
- **A stat that only weapon traits modify is cached per thing forever.** Vanilla's mutable-stat
  scan reads a trait's `equippedStatOffsets` only, never `statOffsets`/`statFactors`, so a
  part-less stat nothing else touches is marked immutable and its first per-thing read is final,
  while the info card recomputes and disagrees (how Carbonized shipped with no HP cut through
  1.4.0-rc.1). Which stats those are depends on the whole mod list.
  `Patches/CompUniqueWeapon_TraitStatCache_Patch.cs` clears every stat a trait names when it lands
  and clamps HP; its header has the precedents and why the game-wide "mark it mutable" alternative
  was rejected. A trait touching a new immutable stat needs nothing more, but a new *path* that
  reads a weapon stat before its traits exist does. In-world trait edits are deliberately left to
  Unique Weapons Unbound.

## Trait mechanics: the extension layer

- Anything a trait needs beyond the four vanilla fields that reach melee (`statOffsets`/
  `statFactors`, `equippedHediffs`, `abilityProps`, `forcedColor`; see `1.6/Defs/CLAUDE.md`) goes
  through a `DefModExtension` on the trait plus a Harmony postfix, so the trait stays an ordinary
  def and vanilla generation, naming and stats keep working. Six exist under `Traits/`:
  `MeleeTraitEffectExtension` (on-hit extra damage, stun, stagger, mental state),
  `MeleeDamageConversionExtension` (reroute the base hit's `DamageDef`), `MeleeToolModExtension`
  (per-tool damage/AP), `MeleeParryExtension` (defender-side negate with its own battle-log
  outcome), `ForcedColorTwoExtension` (forced body colour), `ForcedArtExtension` (guaranteed
  inscription). Prefer extending one over a new mechanism.
- **An effect outside `statOffsets`/`statFactors` is invisible until you describe it, as data on
  the def, never as text in a renderer.** Vanilla only ever displays those two lists.
  `Traits/TraitEffectSummary.cs` derives one short **unstyled** line per effect *from the def*
  (never authored prose, so a retuned number can't drift from its summary; keep it that way) and
  attaches them on every play-data load as a `TraitEffectLinesExtension`; renderers add bullets and
  layout (`Patches/CompUniqueWeapon_TraitStats_Patch.cs` for the info card). **A new on-hit effect
  subclass, extension or trait mechanism must gain a case in `TraitEffectSummary`**, or it ships
  undocumented in-game. Strings live in `Keyed/UMW_Stats.xml`. The summary's header records the two
  rejected shapes (patch the info card alone; append to `description`).
- `TraitEffectLinesExtension` is a published contract (root `CLAUDE.md`); the only test of its
  reader lives in the UWU repo and cannot see a rename on this side.
- A wielder-side effect that is a combat *outcome* rather than a stat gets its own mechanism
  (Quilloned's parry, `Traits/MeleeParryExtension.cs`), never an `equippedHediffs` entry.
- **Material is surfaced explicitly**, because a unique name hides the stuff an ordinary label
  shows: `Things/UniqueMeleeWeapon.cs` adds an inspect-pane line and
  `Patches/NameGenerator_StuffAdjective_Patch.cs` injects the `stuff_adjective` grammar symbol.

## Settings

`UniqueMeleeWeaponsSettings` is one partial class split per UI section under `Core/Settings/`, so
a setting is a one-file edit. The recipe and the patterns to copy (a gated row, a grouped block of
def rows, a collection-valued setting, a setting that overrides a def field, and why a setting that
gates an XML patch through `Core/PatchOperation_UMWSetting.cs` is restart-to-apply and its row must
say so) are in the header of `Core/UniqueMeleeWeaponsSettings.cs`. The family-wide settings-window
rules live in the template (`~/dev/RimworldModTemplate`, `Source/<version>/Settings/CLAUDE.md`);
this mod is the reference implementation of its grouped-rows pattern, and `Core/ModIntegrations.cs`
is the one place an optional mod's name, colour, activity and owned defs are looked up. The
per-trait opt-out is a `CanAddTrait` veto, never a commonality write, and must leave every unique
weapon one enabled first-pick trait or vanilla's naming errors on an empty adjective list
(`Core/Settings/Settings_Traits.cs` header). Trader stock is entirely runtime def-writes from
`Core/Settings/Settings_Traders.cs` and `Traders/StockGenerator_UMWUniqueMelee.cs`; nothing
trader-related ships in XML.
