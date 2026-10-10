# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project Overview

**Unique Melee Weapons** is a RimWorld 1.6 mod adding individually-designed unique melee
weapons — stuffable variants of vanilla melee weapons that roll random traits, colours and
names. Requires Harmony and the Odyssey DLC (a few traits additionally `MayRequire` Royalty).

**Key technologies:** C# (.NET Framework 4.7.2), Harmony, RimWorld modding API, XML defs.

### Where documentation lives

**This file holds only cross-cutting rules and rationale.** Per-item values, tuning numbers,
decompile-verified call paths and the history behind a mechanism (what was tried and why it was
dropped) live in the header comment of the file they describe — every `.cs` file, every
`WeaponCategoryDef`, and every non-obvious trait/hediff/damage def carries one. When adding or
changing something, put the *why* there and only add a line here if it constrains work in other
files. Do not restate def values or call paths here; they drift. Where a bullet below names a
file, that file's header is the full story and this file is the pointer.

Vanilla reference defs studied for the quest work live in the gitignored, non-deployed
`Docs/odyssey-reference/`.

## Build Commands

```bash
# Build (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build UniqueMeleeWeapons.sln -c Release

# Stage the mod into an arbitrary folder (used by CI; same manifest as the local deploy)
dotnet build Source/1.6/UniqueMeleeWeapons.csproj -c Release \
  -t:StageMod -p:StageDir=/path/to/output/UniqueMeleeWeapons
```

The build auto-detects the RimWorld install (Windows/Linux/Mac, including WSL targeting a Windows
install), falling back to the `Krafs.Rimworld.Ref` NuGet package in CI.

**WSL setup:** `RIMWORLD_PATH` in `~/.bashrc` pointing at the Windows install, e.g.
`/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`.

### Deployment

The repo lives outside the Mods folder; every local build redeploys automatically and atomically.

- **One manifest, one place:** the `_ModFiles` ItemGroup in the `StageMod` target of
  `Source/1.6/UniqueMeleeWeapons.csproj` (its comments cover how it globs and what it excludes).
  It is generic over folders, so a new `1.7/` or `Sounds/` needs no build change; only a brand-new
  *file type* does. Local deploy and CI release both call it, so they can't drift.
- **Asset bundles are committed binaries** (CI has no Unity and stages whatever is committed).
  `1.6/Mods/VanillaFactionsExpandedPirates/AssetBundles/umw_shaders_{win,linux,mac}` carry the mix
  shader (see "Stuffable uniques are double-masked" below); they sit in the VFEP compat root
  because only the warcasket pair uses it and bundles load per active root exactly as Defs do, so
  no bundle is opened without VFEP. Rebuild all three with `Source/UnityShaders/build.sh` whenever
  RimWorld moves Unity version; its header and the editor script under `Assets/Editor/` carry the
  pinned Unity version, the OS build modules and the load-bearing package-manifest entry.
  `Source/` never deploys, so the Unity project stays out of the mod and the C# build.
- **Stop hook (`.claude/hooks/sync-mod.sh`):** rebuilds+redeploys after a turn when
  mod-relevant files changed, logging to `$TMPDIR/UniqueMeleeWeapons-build.log`. On failure it exits 2 with the errors on stderr, which
  Claude Code feeds back to the agent so the turn continues; a second failure in the same turn
  (`stop_hook_active`) only warns, so it cannot loop. Its header carries the rule that its
  `find` watch list must cover every content root `StageMod` ships, or edits under a missed root
  silently stop redeploying. Tracked and wired by the tracked `.claude/settings.json`; the script
  is byte-identical across the mod family and derives the solution, project folder and mod name
  itself, so change it in the template and copy it verbatim, never per repo. It bails when no
  RimWorld install is found, so CI and contributors without the game are unaffected.

**`.claude/` is only partly gitignored.** `.gitignore` carries `.claude/*` followed by
`!.claude/skills/`, `!.claude/hooks/` and `!.claude/settings.json`, so the skills, the Stop hook
and its wiring are tracked and shared while `settings.local.json` (personal permissions) stays
local per machine. Editing a skill is therefore a committed, team-visible change and must keep in step
with whatever it automates: `/release`'s step 7 encodes this repo's CHANGELOG layout and the
version scheme (release candidates are `X.Y.Z-rc.N` tags, CHANGELOG-less and Workshop-less, with
the suffix in `modVersion` and `AssemblyInformationalVersion` only; `release.yml` treats any
suffixed tag as a prerelease to match), and `/translate`'s glossary encodes per-language
terminology decisions. Changing the thing without changing the skill leaves an instruction
pointing at something that no longer exists, and nothing fails until the next release run.

## Architecture

### Naming conventions

- All defs use the `UMW_` prefix. A unique variant of a vanilla weapon mirrors Odyssey's
  convention: base weapon name + `_Unique` (`UMW_LongSword_Unique`).
- **One def per file** in every `Defs/` `.xml`, named after the def with the `UMW_` prefix
  stripped and the meaningful `_Unique` suffix kept. Trait files sit in a subfolder named after
  their `WeaponCategoryDef` (`Melee/`, `Bladed/`, `Pointed/`, `Blunt/`, `Heavy/`, `Guarded/`), so
  a trait's `<weaponCategory>` is obvious from its path. Defs load recursively and the deploy
  manifest globs, so new files/subfolders need no build change.
- **Textures** follow Odyssey: per-weapon folder, variants `Unique<Weapon><Letter>.png` plus a
  matching `_m` mask. They live at the repo root, not under a version folder — art is not
  version-scoped (a DLC-gated weapon's art goes under `Mods/<DLC>/Textures/`, see Optional-DLC
  content below). `texPath` points at the *folder*, with
  `graphicClass>UniqueMeleeWeapons.Graphic_RandomComplex`.
- **C#:** root namespace `UniqueMeleeWeapons`; patch classes use a `.Patches` suffix to avoid
  RimWorld type-name conflicts. `PatchAll()` in `UniqueMeleeWeaponsMod` picks up every
  `[HarmonyPatch]` class in the assembly. Two patches deliberately carry no attribute and are
  applied conditionally, so an install with no consumer places no detour on the shader loader or
  the render path at all; the `Mod` header names them and each patch's header says why, and why it
  applies where it does (the shader loader runs in def `PostLoad`, before `CallAll`, so it cannot
  wait for `UMW_Startup`). Don't "tidy" them back into `PatchAll`.
- **Warnings are build errors.** The csproj sets `TreatWarningsAsErrors`, so every compiler and
  analyzer warning fails the build, locally, in the Stop hook and in CI. Severities are pinned in
  `.editorconfig`: `warning` blocks the build, `suggestion` is IDE-only. Fix the code, not the
  severity, unless the rule is wrong for this domain.
- **Patch-timing hazard (other mods' methods):** `PatchAll()` runs from the `Mod` subclass
  constructor — BEFORE any defs are loaded. Applying a detour JIT-compiles the target and runs its
  declaring type's static ctor, so a patch targeting ANOTHER MOD's method can permanently break
  that mod when its cctor resolves defs (the BetterTradersGuild v1.1.0 CWTL incident). All current
  targets are vanilla (safe); before ever adding a foreign-target patch, defer its application
  until after defs load — worked example: BetterTradersGuild's `Core/DeferredModPatches.cs`.
- **Settings:** every user-facing string is localized — through `.Translate()` against `UMW_UI.xml`,
  except where vanilla already localizes the exact string (reuse the vanilla Keyed key or def label
  rather than duplicating it), and strings that name game content inject the def label as a
  placeholder rather than restating it. `UniqueMeleeWeaponsSettings` is one partial class split per
  UI section under `Core/Settings/`, so adding a setting is a one-file edit. The recipe and the
  patterns to copy (a DLC-only row, a collection-valued setting, a setting that *overrides a def
  field*, and why a setting that gates an XML patch is restart-to-apply) are in the header of
  `Core/UniqueMeleeWeaponsSettings.cs`.
- **Keyed files split by purpose:** `UMW_UI.xml` settings strings, `UMW_Combat.xml` in-combat
  floating text, `UMW_Stats.xml` info-card trait-effect lines.
- **No em dashes in player-facing text** (def labels/descriptions, `Keyed/`, `About.xml`) — reflow
  the sentence instead. This file, code comments and def comments are unaffected.

### Design rules for weapons and traits

- **A trait must read as a physical property of the weapon** — flanges, studs, guards, coatings,
  provenance — never an unexplained wielder blessing. Ability traits are the accepted exception,
  tone-checked case by case.
- **Trait names name the physical feature** (quillons, a dead-blow head, an opiated point), never
  the effect verb. The granted *ability* and any wielder-side hediff name the act instead.
- **Control-effect budget:** a new stun/stagger/control proposal must *displace* an existing
  control effect, not add to the census (held at Odyssey's ~1-in-8 share).
- **Category taxonomy is locked at 6:** `UMW_Melee` (universal), the mechanism categories
  `UMW_Bladed`/`UMW_Pointed`/`UMW_Blunt`, the handling category `UMW_Heavy`, and the
  weapon-gating category `UMW_Guarded`. Membership, trait families, the global exclusion-token
  registry and the per-category rationale live on the `WeaponCategoryDef` files — read the
  relevant one before adding a trait. Single-trait categories are fine (Odyssey ships several);
  the bar is that each trait be mechanically meaningful (a `UMW_Reach` category for the spear was
  rejected: melee has no reach mechanic, so its traits would be flavour-only).
- **A category gates by weapon; `exclusionTags` only prevent co-rolls.** Use a new category when
  a trait requires a construction feature the weapon may not have (`UMW_Guarded`); use a token
  when traits are alternatives within a family.
- **Generation throws** unless a weapon's categories include a `canGenerateAlone=true` trait that
  yields a `traitAdjective`. `UMW_Melee`'s universal `UMW_Lightweight` guarantees this — don't
  remove it, and don't make it inheritance-dependent (see `WeaponCategoryDefs/Melee.xml` for why
  the six Odyssey ports are deliberate self-contained copies, *not* XML inheritance).
- **`MarketValue`: factor only for value-scaling, flat offset for everything else.** Following
  Odyssey — factor ⟺ precious-inlay scaling or devaluation (`Ugly`/`Cumbersome`-likes);
  offset ⟺ any added capability. A trait that is both carries both halves. Size offsets from the
  nearest Odyssey analog and name it inline in the trait file.
- **Royalty-analog traits are def-level `MayRequire`-gated**, as are any mod-owned defs only they
  consume. A skipped def never enters the `DefDatabase`, so category rolls simply don't see it.

### Hard-won constraints (violate these and it fails silently)

- **Most mechanically interesting `WeaponTraitDef` fields do nothing on melee.**
  `damageDefOverride`, `extraDamages`, `additionalStoppingPower`, `burstShot*` and
  `ignoresAccuracyMaluses` are read only by `Projectile`/`Verb_LaunchProjectile`;
  `marketValueOffset`, `killThought`, the `bonded*` fields and `equippedStatOffsets` are read only
  by bladelink (persona) weapons. All of those are **silently inert** on `CompUniqueWeapon` —
  never use them, and don't patch one live either: a 1.0.x patch routing `equippedStatOffsets`
  was retired because it rode vanilla's stat pipeline per frame for one trait (the swap and its
  tuning equivalence are in `WeaponTraitDefs/Pointed/NeedlePoint.xml`).
  What reaches melee natively: `statOffsets`/`statFactors`, `equippedHediffs`, `abilityProps`,
  `forcedColor`. **Wielder-side effects are expressed as weapon stats first**: a trait is a
  physical property of the weapon, and Odyssey prices every trait buff/malus as a weapon-thing
  stat, so `equippedHediffs` — the one vanilla-applied wielder-side vehicle — stays an unused
  escape hatch, not a precedented tool. A wielder effect that is a combat *outcome* rather than a
  stat gets its own mechanic instead (Quilloned's parry — `MeleeParryExtension`). Market value
  rides `statOffsets`/`statFactors → MarketValue`.
- **Trait stat mods reach any stat of the weapon *thing***, not just combat ones — item-condition
  stats (`MaxHitPoints`, `DeteriorationRate`, `Flammability`) are fair game. Note melee damage and
  armor pen share the single `MeleeWeapon_DamageMultiplier` stat: there is **no** melee AP stat, so
  raising AP via stats always raises damage. Use `MeleeToolModExtension` for independent per-tool
  changes.
- **A stat that only weapon traits modify is cached per thing forever.** Vanilla's mutable-stat scan
  (`StatDef.PopulateMutableStats`) reads a `WeaponTraitDef`'s `equippedStatOffsets` only, never its
  `statOffsets`/`statFactors`, so a part-less stat nothing else touches is marked immutable and its
  first per-thing read is final; the info card recomputes and disagrees with the weapon. Which stats
  those are depends on the whole mod list (with no other mods: `MaxHitPoints`, `Flammability` and
  `MeleeWeapon_CooldownMultiplier`; Biotech's FireResistant gene already makes `Flammability`
  mutable). `Thing.PostMake` reads `MaxHitPoints` before traits roll, which is how Carbonized shipped
  with no HP cut from its introduction through 1.4.0-rc.1.
  `Patches/CompUniqueWeapon_TraitStatCache_Patch.cs` clears the caches of every stat a trait names
  when it lands (vanilla's own stuff-change idiom; its header has the precedents and why the
  game-wide "mark it mutable" alternative was rejected) and clamps HP; a trait that touches a new
  immutable stat needs nothing more, but a new *path* that reads a weapon stat before its traits
  exist does. **Never evaluate a stat inside `ExposeData` (any mode, including `PostLoadInit`):**
  a stat read runs every mod's stat patches, and one that lazily loads its settings on first use
  opens a second Scribe session mid-load, which force-stops the real one and aborts the load
  (issue #2, 1.4.0 to 1.4.1). Queue stat-dependent load repairs through
  `LongEventHandler.ExecuteWhenFinished` instead; `Things/UniqueMeleeWeapon.cs` is the pattern,
  including the guard its callback needs. In-world trait edits are deliberately left to Unique
  Weapons Unbound, which marks every trait-named part-less stat mutable **game-wide**; so before a
  new trait names a part-less stat, check how hot its readers are, because under UWU that stat
  loses its per-thing cache for every thing in the game (the current three are read per attack,
  per fire-spread check, or behind a 10-tick cache, so they cost nothing measurable).
- **Anything a weapon needs beyond those four fields goes through our own extension layer** — a
  `DefModExtension` on the trait plus a Harmony postfix, so the trait stays an ordinary def and
  vanilla generation/naming/stats keep working. Six exist, each documented in
  `Source/1.6/Traits/`: `MeleeTraitEffectExtension` (on-hit: extra damage, stun, stagger, mental
  state), `MeleeDamageConversionExtension` (reroute the *base* hit's `DamageDef`),
  `MeleeToolModExtension` (per-tool damage/AP), `MeleeParryExtension` (defender-side chance to
  negate a melee blow, with its own battle-log outcome), `ForcedColorTwoExtension` (forced body
  colour), `ForcedArtExtension` (guaranteed inscription regardless of quality). Prefer extending
  one of these over a new mechanism.
- **An effect outside `statOffsets`/`statFactors` is invisible until you describe it — as *data on
  the def*, never as text in a renderer.** Vanilla only ever displays those two lists, so every
  extension-borne effect, `equippedHediffs` and `abilityProps` would otherwise show a description
  and a market value with no stated effect. `Traits/TraitEffectSummary.cs` derives one short
  **unstyled** line per effect *from the def* (never authored prose, so a retuned number can't
  drift from its own summary — keep it that way) and attaches them on every play-data load as a
  `TraitEffectLinesExtension`; renderers add their own bullets and layout
  (`Patches/CompUniqueWeapon_TraitStats_Patch.cs` for the info card). **A new on-hit effect
  subclass, extension or trait mechanism must gain a case in `TraitEffectSummary`**, or it ships
  undocumented in-game. Strings live in `Keyed/UMW_Stats.xml`. The summary's header records the two
  shapes that were tried and rejected (patch the info card alone; append to `description`).
- **`TraitEffectLinesExtension` is a published cross-mod contract**, like the `stuff_adjective`
  symbol: Unique Weapons Unbound's trait-picker tooltip finds it by duck-typing — a type whose
  **simple name** is `TraitEffectLinesExtension` with a public `List<string> lines` field — so
  neither assembly references the other. Renaming either compiles clean here and silently empties
  that tooltip; the only test of the reader lives in that repo and can't see a rename on this side.
- **On-hit `DamageDef` payloads work for free.** `DamageDef.additionalHediffs` and the damage
  workers (`Flame`'s ignition, `EMP`'s stun) are applied source-agnostically by every
  `Thing.TakeDamage`, so an extra-damage effect carrying the right `DamageDef` needs no new C#.
  Reuse a Core `DamageDef` where one fits; clone only when a field must change.
- **Every weapon def must carry a `CompEquippable`-derived ability comp.** `CompUniqueWeapon.Setup`
  dereferences `CompEquippableAbilityReloadable` with no null check whenever a rolled trait carries
  `abilityProps`, and only one such comp is allowed per thing, so all 10 unique defs replace their
  inherited comps wholesale (`<comps Inherit="False">`, uniform across the 10 even where no ability
  can currently roll). **If base-game weapon comps change in a vanilla update, replicate the change
  in all 10 files.** The one sanctioned difference: **a unique mirrors its base's quality**, so the
  quality-less VFEP warcasket pair carries no `CompQuality` but keeps `CompArt` for `UMW_Storied`
  alone (`UniqueMeleeWeapon.PostMake` initializes the inscription only when a rolled trait forces
  one; with no quality to earn it, the pair otherwise bears none).
- **An AoE ability's radius lives in two places and must agree, at `X.9`.** The gizmo-hover preview
  reads `verbProperties.range` and *never* a comp field, so a mismatch draws a ring that lies about
  the effect; and `X.0` or the wrong `X.9` are trap values that draw a filled square or a sparse
  diamond. Both ability defs carry the worked arithmetic; `AbilityDefs/Earthshake.xml` has the
  fullest version.
- **Stuffable uniques are double-masked** (`Things/UniqueMeleeWeapon.cs`): mask **red** → colour
  one (the unique accent, supplied by vanilla), mask **green** → colour two (the material tint, or
  a trait-forced body colour). This is the load-bearing trick of the mod — Odyssey's ranged uniques
  are not stuffable and don't need it. **Art rule:** the weapon silhouette must be all red/green
  with **no black** (black means "not painted" and would ignore the material entirely), and the
  diffuse must stay light/neutral so the multiply yields a clean tint. There are only two
  channels, so a forced body colour *replaces* the material tint — one body-colour trait per
  weapon, gated by its exclusion token; it can still co-occur with a colour-one inlay. A
  **non-stuffable** unique (the VFEP warcasket pair) has no material tint, so its body placeholder
  is the def's `graphicData.colorTwo`, which a forced body colour still replaces.
  The uniques draw with vanilla `CutoutComplex`, except the warcasket pair, whose feathered
  red|green mask edges draw with our `UMW_CutoutComplexMix`: vanilla's shader with the two tints
  **mixed** instead of **stacked**, bit-identical on pure red, green and black texels. Its
  ShaderTypeDefs, bundles and only two consumers live in the VFEP compat root (source
  `Source/UnityShaders/`; the ShaderTypeDef header has the formulas). Hard red|green edges stay
  the safer default for new art, because the vanilla fallback below stacks again.
- **The mix shader must fail to vanilla `CutoutComplex`, never `Cutout`** — vanilla's own fallback
  for a shader it can't find, which drops the mask and colour two.
  `Patches/ShaderDatabase_LoadShader_Fallback_Patch.cs` is the full rationale: the ways the load
  fails (including a stale cache after a mid-session language change), why it must hook the loader
  rather than `UMW_Startup`, and why both the map and UI paths swap together. It is applied only
  while VFEP is active and touches nothing unless a call names one of our two paths. Those paths
  live in three places that move together: the ShaderTypeDefs, that patch's constants, and the
  Unity project's asset paths (`Assets/Data/<packageId>/Materials/<path>.shader` — packageId,
  because a Workshop mod's folder name is a numeric id).
- **A mask edge must sit in dark ink or on a pixel-exact colour edge, never along an anti-aliased
  colour change.** `CutoutComplex` is a per-texel multiply, so a texel on the wrong side of a tint
  edge (an anti-alias ramp texel, or one the mask spills onto or misses) renders darker or brighter
  than both neighbours, and the mask's staircase reads as a torn, dashed seam. Ink hides it
  (Odyssey keeps ~80% of its tint edges there). Feathering the mask can't fix it: no single mask
  value makes a multiply reproduce a blend for every colour, so the fix is in the art. Masks that
  follow the art rule above are safe by construction; it bites wherever untinted art sits beside
  tinted art, as on the warcasket pair. An edge that only the mask draws, over flat art, can't
  speck. (A feathered red|green edge is a different class: the mix shader renders it exactly.)
  Separately, load-time DXT5 compression stores each 4×4 block as one straight colour ramp, so
  red, green and black sharing a block come out wrong.
- **Vanilla melee weapons have no `Name=`, so they can't be `ParentName` targets.**
  `Patches/AddNameToBaseMeleeWeapons.xml` adds one per base weapon we mirror (add-if-missing, so it
  stacks safely with other mods; DLC-gated by node existence). Add an Operation there for each new
  base weapon; unique defs then override only their deltas and inherit tools/stats/stuff.
  **A base from a third-party mod also needs that mod in `About.xml` `loadAfter`.**
  Inheritance only accepts a parent owned by a mod at or before the child's load order, and our
  Name-add doesn't change ownership (DLCs always load first, so Core/DLC bases are safe).
  Misordered, the unique logs "Could not find parent node" and still loads, parentless (no
  tools/stats); nothing guards beyond the vanilla mod-list warning, and the smoke test can't catch
  it because it writes its own pinned order.
- **Back-reference the base weapon via `<descriptionHyperlinks>`.** Our `UMW_` prefix means the
  base def isn't derivable from the unique's defName, so the explicit link is required.
- **Nullified *situational* thoughts still render as a grey "0" row** (only memories are dropped at
  `MoodOffset()==0`). So a trait-flipped mood must be **one multi-stage def with a stage-routing
  worker**, not a penalty def plus a `requiredTraits` buff def — the latter shows a duplicate row.
  Every other personality exemption stays declarative (`nullifyingTraits`/`nullifyingGenes`, with
  `MayRequire` on mod-specific entries). See `Traits/ThoughtWorker_BloodStainedWeapon.cs`.
- **Reward pools are split in def space, not by Harmony.** Odyssey's `ThingSetMaker_UniqueWeapon`
  makes things with no stuff, which both errors on our stuffable weapons and dilutes the ranged
  pool. Every `*_Unique` weapon carries a `UMW_UniqueMelee` tag;
  `Patches/RepointUniqueWeaponPool.xml` repoints the two class-based vanilla consumers onto
  `ThingSetMaker_UMWUnique`, and our own pool filters on the tag (tag-based makers — crates,
  fishing, map-gen loot — pass a stuff already).
  The tag is also how C# asks "is this def one of ours?" (`UniqueWeaponDefs` owns the constant and
  the test — never re-derive it from a defName prefix) and a **published opt-in cross-mod contract**
  like `stuff_adjective`: a third-party melee unique carrying it joins our pool, settings and
  exclusion machinery wholesale. Changing it means changing the weapon defs, our pool def and that
  constant in lockstep.
- **A def is kept out of the pools by filtering `ThingSetMakerUtility.CanGenerate`, never by removing
  the def.** That is the one choke point every `ThingSetMaker` funnels through, so the per-weapon
  settings toggles need the single postfix in `Patches/ThingSetMakerUtility_CanGenerate_Patch.cs`,
  and estimates, "can this maker generate?" checks and saves that already contain the weapon all
  stay consistent.
- **Tribal consumers are tech-capped by validator, never by `ThingSetMakerParams.techLevel`.** The
  Warband quest and the tribal trader stock hand out only weapons at or below
  `UniqueWeaponDefs.TribalTechCap`. `techLevel` would filter too, but it also down-weights sub-cap
  Neolithic gear ×0.1, making those uniques 10× rarer. Use `UniqueWeaponDefs.FitsTribal` as a
  `validator` (quest) or `maxTechLevelGenerate` (our stock generator); the cap/floor rationale sits
  on the constants in `UniqueWeaponDefs.cs`. Other pools stay uncapped by decision.
- **Material must be surfaced explicitly**, because a unique name hides the stuff an ordinary label
  shows. `UniqueMeleeWeapon` adds an inspect-pane line, and
  `Patches/NameGenerator_StuffAdjective_Patch.cs` injects a `stuff_adjective` grammar symbol into
  name generation — also a **dependency-free integration contract** with Unique Weapons Unbound,
  which publishes the material under that symbol while we supply the grammar. Don't rename it.
- **Startup def-writes and def caches re-run on every play-data load, not once per process.** A
  mid-session language change reloads all play data in-process and replaces every def instance;
  `[StaticConstructorOnStartup]` never re-runs, so anything it wrote onto defs goes stale. All such
  work therefore lives in `UMW_Startup.Run` (which must stay idempotent), invoked from
  `Patches/StaticConstructorOnStartupUtility_CallAll_Patch.cs` — its header carries the verified
  load ordering and the traps in full.

### Notable features

- **Warband quest** (`Source/1.6/Quests/`) — a low-tech tribal sibling of Odyssey's
  `AncientMercenaries` handing out our uniques via a temporary hidden faction, our reward pool,
  reused vanilla tribal pawnkinds and a ruined tribal site. Rationale per difference is in
  `QuestNode_Root_Warband.cs`.
- **Wood-free material rolls** (`Patches/GenStuff_ExcludeWoodStuff_Patch.cs`) — setting-gated,
  def-gated to our weapons, filtering the single choke point every generation path funnels through.
- **Trader stock** (`Traders/StockGenerator_UMWUniqueMelee.cs`,
  `Core/Settings/Settings_Traders.cs`) — five default-off toggles put uniques in vanilla traders'
  stock, entirely through runtime def-writes (nothing trader-related ships in XML). Two tech bands
  *partition* the roster so the choice of trader stays meaningful: tribal traders carry uniques at
  or below `UniqueWeaponDefs.TribalTechCap`, outlander ones at or above `OutlanderTechFloor`. The
  outlander rows appear only while some unique in the roster clears the floor (derived from the
  tag-built def list, not a mod check). Rates, precedents and the ultratech-trait scoping are in
  the two headers.

## Localization

English (Keyed files + def fields) is the source of truth; other languages derive from it via the
`/translate` skill (`.claude/skills/translate/SKILL.md` — this mod's translation surface, grounding
domain and glossary) and are validated deterministically by `python3 Scripts/check-translations.py`
(also a CI release gate). The DefInjected expected set is the checked-in sidecar
`Scripts/expected-injections.json`: a dump of every injection point the *live* game sees for this
mod — including vanilla-inherited fields and C#-default comp strings that never appear in this
repo's XML — produced by `Scripts/refresh-translation-expectations.py` driving the L10nProbe dev
mod (source `l10n/probe/`; build/deploy it only from the canonical `~/dev/rimworld-l10n` checkout)
through the game's own walker. The checker refuses to run against stale expectations (an unseen
defName, or drifted label/description text), so new content forces a regen; the release skill
regenerates every release, which also covers vanilla updates changing inherited text. **Never
hand-edit the sidecar — regenerate it.** Its fields mirror the live game, not the XML, and the
staleness check doesn't cover all of them: a hand-patched `normalized` comp index (`086c554`) passed
the checker and sat wrong until the next release's regen. The public
language roster lives in CONTRIBUTING.md and must move in the same commit as any language change.

- **Shared l10n toolkit (`l10n/` submodule):** the family-wide process, per-language references,
  cross-language lessons and the checker/refresh/smoke engines live in the `rimworld-l10n` repo
  (canonical checkout `~/dev/rimworld-l10n`); the `Scripts/` files are thin per-repo config shims
  over its engines. If `l10n/` is empty, run `git submodule update --init`. Never edit `l10n/` in
  place here: mod-independent learnings go upstream in the canonical checkout, mod-specific ones
  (this mod's coined weapon-trait/name-grammar vocabulary) go in this repo's skill/glossary.
  Upstream ships semver tags (a major means this repo's shim or flow needs an edit); the pin moves
  only at release (release skill step 3), at the start of a translation pass, or when a new major
  lands — never per upstream commit.

**Workshop title coupling:** each language's `UMW_SettingsCategory` Keyed value is the localized
Steam Workshop title and must equal the title line (line 1) of
`.steamworkshop/Description/<Language>.txt` — always change the two together (English keeps
`Unique Melee Weapons` in both).

**Optional-DLC content ships from LoadFolders-gated compat roots**, because MayRequire is honored
on defs but IGNORED on DefInjected entries, and textures have no node to carry one at all — so the
load root *is* the gate (`IfModActive`, a LoadFolders attribute unrelated to MayRequire). Every
well-known content folder is scanned once per active load root, so gating works for Textures
exactly as it does for Defs and Languages. There are **two** roots per optional mod, mirroring the
ungated `/` + `1.6` split:

- `Mods/<Name>/` — version-independent content: **art**, for the same reason the main `Textures/`
  tree is at the repo root (nesting it under `1.6/` would make a future `1.7/` duplicate the PNGs).
- `1.6/Mods/<Name>/` — version-specific content: `Defs`, and the `Languages` that must sit in the
  same load root as the defs they target.

Currently `Royalty` (the unique Axe/Warhammer, their art, and their Royalty-tech traits and
colours); `VanillaFactionsExpandedPirates` (the two non-stuffable warcasket uniques, warcasket-only
via VEF's inherited `HeavyWeapon` extension, their art, and the mix shader's ShaderTypeDefs and
bundles — the def headers cover the inherited VEF extensions and the dropped crate graphic); and
`VanillaTexturesExpanded`
(version root only: a Patches-only root that re-poses and re-scales the unique spear to match VTE's
redrawn vanilla spear, measurements in the patch header, with the drafted-idle grip nudge riding
`CarriedWeaponOffsetExtension`, our only def hook for a pose vanilla hard-codes). That root is
switchable from a Compatibility setting via `PatchOperation_UMWSetting`, which works because `Mod`
subclasses are created before XML patches apply — so **any setting that gates a patch is
restart-to-apply, and its row must say so**. Third-party compat uses the same shape as DLC compat;
gate on the packageId, never `PatchOperationFindMod` (matches by display name). **In C#, gate with
`ModLister.GetActiveModWithIdentifier(id, ignorePostfix: true)`, never `ModsConfig.IsActive`.**
When a local copy and a Workshop copy of a mod are both installed, the Workshop copy's `PackageId`
gets a `_steam` suffix; `IfModActive` and `MayRequire` ignore it, `IsActive` matches it literally,
so the two gates disagree exactly on a developer's machine and the C# side silently stays off.

`texPath` is **unaffected by which root the art lives in**: textures are keyed by their path
relative to `Textures/` in one flat per-mod dictionary merged across all roots, so a move needs no
def edit — only the matching `_ModFiles` glob in `StageMod` (a miss deploys nothing and shows pink
boxes in-game, with no build error).

Compat roots must sit beside the well-known folders, never inside one — anything under `1.6/Defs/**`
or `1.6/Languages/**` loads unconditionally at any depth. A compat root's language files must not
reuse a main-tree file's language-relative path: the game dedups per mod by that path and silently
skips one whole file (caught pre-release in 2026-08, when every language's main-tree injections
silently failed to load); compat-root files carry a `_Royalty` suffix. The checker validates key
parity, placeholders, DefInjected legality, load-root placement (an entry must live in the same
load root as the def it targets), cross-root file-path collisions, staleness, and file hygiene.

## Debugging

1. **Dev Mode:** Settings > Dev Mode > Logging.
2. **Log:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   (WSL: `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`).
3. **Logging convention:** `Log.Message("[Unique Melee Weapons] ...")`.
4. **Inspect the API:** `ilspycmd "/mnt/c/.../RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t "Namespace.ClassName"`.
5. **Startup smoke test (pre-release):** `python3 Scripts/integration-smoke-test.py` (game closed)
   boots UMW with its family siblings (UWU, PWU) on a pinned list, then classifies Player.log
   errors by origin and fails on anything attributed to UMW or a family seam. Run before every
   release (wired into the release skill); thin shim over the shared engine in `l10n/smoke/`.
