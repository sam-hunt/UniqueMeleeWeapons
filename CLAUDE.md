# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project Overview

**Unique Melee Weapons** is a RimWorld 1.6 mod adding individually-designed unique melee
weapons — stuffable variants of vanilla melee weapons that roll random traits, colours and
names. Requires Harmony and the Odyssey DLC (a few traits additionally `MayRequire` Royalty).

**Key technologies:** C# (.NET Framework 4.7.2), Harmony, RimWorld modding API, XML defs.

### Where documentation lives

**This file holds only cross-cutting rules and rationale.** Per-item values, tuning numbers and
decompile-verified call paths live in the header comment of the file they describe — every `.cs`
file, every `WeaponCategoryDef`, and every non-obvious trait/hediff/damage def carries one. When
adding or changing something, put the *why* there and only add a line here if it constrains work
in other files. Do not restate def values or call paths here; they drift.

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
  `Source/1.6/UniqueMeleeWeapons.csproj` — see that target's comments for how it globs and what it
  excludes. It is generic over folders, so a new `1.7/` or `Sounds/` needs no build change; only a
  brand-new *file type* does. Local deploy and CI release both call it, so they can't drift.
- **Stop hook (`.claude/hooks/sync-mod.sh`):** rebuilds+redeploys after a turn only when
  mod-relevant files changed, logs to `$TMPDIR/umw-build.log`, warns on failure. It is local-only
  (see below) — if it is ever promoted to committed config, move the helper somewhere
  version-controlled.

**`.claude/` is only partly gitignored.** `.gitignore` carries `.claude/*` followed by
`!.claude/skills/`, so the skills are tracked and shared while hooks and settings are local
per-machine. Editing a skill is therefore a committed, team-visible change and must keep in step
with whatever it automates: `/release`'s step 4 encodes this repo's CHANGELOG layout, and
`/translate`'s glossary encodes per-language terminology decisions. Changing the thing without
changing the skill leaves an instruction pointing at something that no longer exists, and nothing
fails until the next release run.

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
  RimWorld type-name conflicts. All patches are applied by `PatchAll()` in
  `UniqueMeleeWeaponsMod`, so a `[HarmonyPatch]` class anywhere in the assembly is picked up.
- **Patch-timing hazard (other mods' methods):** that `PatchAll()` runs from the `Mod` subclass
  constructor — BEFORE any defs are loaded. Applying a detour JIT-compiles the target and runs its
  declaring type's static ctor, so a patch targeting ANOTHER MOD's method can permanently break
  that mod when its cctor resolves defs (the BetterTradersGuild v1.1.0 CWTL incident). All current
  targets are vanilla (safe); before ever adding a foreign-target patch, defer its application
  until after defs load — worked example: BetterTradersGuild's `Core/DeferredModPatches.cs`.
- **Settings:** every user-facing string is localized — through `.Translate()` against `UMW_UI.xml`,
  except where vanilla already localizes the exact string (reuse the vanilla Keyed key or def label
  rather than duplicating it), and strings that name game content inject the def label as a
  placeholder rather than restating it. `UniqueMeleeWeaponsSettings` is one partial class split per
  UI section: `Core/UniqueMeleeWeaponsSettings.cs` holds only the window frame, the
  `ExposeData`/`ResetToDefaults` fan-out and the shared row helpers, while each section owns its
  fields, scribe entries, defaults, def-writes and draw method in a `Core/Settings/Settings_*.cs`
  file — so adding a setting is a one-file edit. The step-by-step recipe — including the pattern for
  a setting that *overrides a def field* (written onto the live def on every play-data load and on
  window close; XML holds only the shipped default) — is in the header of
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
  the bar is that each trait be mechanically meaningful — a `UMW_Reach` category was considered for
  the spear and rejected, since melee has no reach mechanic and its traits would be flavour-only.
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
  never use them. (A patch routing `equippedStatOffsets` live shipped in 1.0.x as NeedlePoint's
  hit-chance malus and was deliberately retired: it rode vanilla's stat pipeline tens of thousands
  of calls per frame for that one trait, and a weapon-side stat expresses the same fiction — the
  NeedlePoint header carries the swap rationale and tuning equivalence.)
  What reaches melee natively: `statOffsets`/`statFactors`, `equippedHediffs`, `abilityProps`,
  `forcedColor`. **Wielder-side effects are expressed as weapon stats first**: canonically a trait
  is a physical property of the weapon, and Odyssey prices every trait buff/malus as a weapon-thing
  stat (for ranged uniques even accuracy lives on the weapon), so `equippedHediffs` — the one
  vanilla-applied wielder-side vehicle (`WeaponTraitWorker`) — stays an unused escape hatch, not a
  precedented tool. A wielder effect that is a combat *outcome* rather than a stat gets its own
  mechanic instead (Quilloned's parry — `MeleeParryExtension`). Market value rides
  `statOffsets`/`statFactors → MarketValue`.
- **Trait stat mods reach any stat of the weapon *thing***, not just combat ones — item-condition
  stats (`MaxHitPoints`, `DeteriorationRate`, `Flammability`) are fair game. Note melee damage and
  armor pen share the single `MeleeWeapon_DamageMultiplier` stat: there is **no** melee AP stat, so
  raising AP via stats always raises damage. Use `MeleeToolModExtension` for independent per-tool
  changes.
- **Anything a weapon needs beyond those four fields goes through our own extension layer** — a
  `DefModExtension` on the trait plus a Harmony postfix, so the trait stays an ordinary def and
  vanilla generation/naming/stats keep working. Six exist, each documented in
  `Source/1.6/Traits/`: `MeleeTraitEffectExtension` (on-hit effects — extra damage, stun, stagger,
  mental state), `MeleeDamageConversionExtension` (reroute the *base* hit's `DamageDef`),
  `MeleeToolModExtension` (per-tool damage/AP), `MeleeParryExtension` (defender-side chance to
  negate an incoming melee blow, with its own battle-log outcome), `ForcedColorTwoExtension`
  (forced body colour), `ForcedArtExtension` (guaranteed art inscription regardless of quality).
  Prefer extending one of these over a new mechanism.
- **An effect outside `statOffsets`/`statFactors` is invisible until you describe it — as *data on
  the def*, never as text in a renderer.** Vanilla only ever displays those two lists (plus
  ranged-only fields), so every extension-borne effect, `equippedHediffs` and `abilityProps` would
  otherwise show a description and a market value with no stated effect. The split is strict:
  `Traits/TraitEffectSummary.cs` derives one short **unstyled** line per effect and attaches them on
  every play-data load as a `TraitEffectLinesExtension`; renderers add their own bullets and layout
  (`Patches/CompUniqueWeapon_TraitStats_Patch.cs` for the info card). **A new on-hit effect subclass,
  extension or trait mechanism must gain a case in `TraitEffectSummary`**, or it ships undocumented
  in-game. Lines are *derived from the def, never authored prose*, so a retuned number can't drift
  from its own summary — keep it that way. Strings live in `Keyed/UMW_Stats.xml`.
  Two shapes were tried and rejected: patching only the info card fixes one screen and leaves every
  other consumer to patch its own; appending the lines to `Def.description` reaches all of them but
  hands each a pre-styled blob, which dumped them into the middle of Unbound's prose paragraph
  instead of its bulleted effects list.
- **`TraitEffectLinesExtension` is a published cross-mod contract**, like the `stuff_adjective`
  symbol. Unique Weapons Unbound's trait-picker tooltip finds it by duck-typing — scanning
  `modExtensions` for a type whose **simple name** is `TraitEffectLinesExtension` and reflecting its
  public `List<string> lines` field — so neither assembly references the other. Renaming the type or
  the field compiles clean here and silently empties that tooltip. Its reader is covered by
  `TraitEffectLinesIntegrationTests` in that repo, which can't see a rename on this side.
- **On-hit `DamageDef` payloads work for free.** `DamageDef.additionalHediffs` and the damage
  workers (`Flame`'s ignition, `EMP`'s stun) are applied source-agnostically by every
  `Thing.TakeDamage`, so an extra-damage effect carrying the right `DamageDef` needs no new C#.
  Reuse a Core `DamageDef` where one fits; clone only when a field must change.
- **Every weapon def must carry a `CompEquippable`-derived ability comp.** `CompUniqueWeapon.Setup`
  dereferences `CompEquippableAbilityReloadable` with no null check whenever a rolled trait carries
  `abilityProps`. Only one such comp is allowed per thing, so all 8 unique defs replace their
  inherited comps wholesale (`<comps Inherit="False">`, uniform across the 8 even where no ability
  can currently roll). **If base-game weapon comps change in a vanilla update, replicate the change
  in all 8 files.**
- **An AoE ability's radius lives in two places and must agree, at `X.9`.** The gizmo-hover preview
  reads `verbProperties.range` (via `VerbProperties.DrawRadiusRing`) and *never* a comp field, so a
  mismatch draws a ring that lies about the effect. Use `X.9`, not `X.0`: the ring outlines the edge
  of the discrete cell set inside the radius, so a radius whose set exactly fills its own bounding
  box draws a **square** (2.9 did), and an exact integer admits a sparse diamond. Both ability defs
  carry the worked arithmetic; `AbilityDefs/Earthshake.xml` has the fullest version.
- **Stuffable uniques are double-masked** (`Things/UniqueMeleeWeapon.cs`): mask **red** → colour
  one (the unique accent, supplied by vanilla), mask **green** → colour two (the material tint, or
  a trait-forced body colour). This is the load-bearing trick of the mod — Odyssey's ranged uniques
  are not stuffable and don't need it. **Art rule:** the weapon silhouette must be all red/green
  with **no black** (black means "not painted" and would ignore the material entirely), and the
  diffuse must stay light/neutral so the multiply yields a clean tint. There are only two channels,
  so a forced body colour *replaces* the material tint — one body-colour trait per weapon, gated by
  its exclusion token; it can still co-occur with a colour-one inlay.
- **Vanilla melee weapons have no `Name=`, so they can't be `ParentName` targets.**
  `Patches/AddNameToBaseMeleeWeapons.xml` adds one per base weapon we mirror (patches run before
  inheritance resolution; `AttributeAdd` is add-if-missing, so it stacks safely with other mods).
  Add an Operation there for each new base weapon, DLC-gated by node existence if it isn't Core.
  Unique defs then override only their deltas and inherit tools/stats/stuff.
- **Back-reference the base weapon via `<descriptionHyperlinks>`.** Our `UMW_` prefix means the
  base def isn't derivable from the unique's defName, so the explicit link is required.
- **Nullified *situational* thoughts still render as a grey "0" row** (only memories are dropped at
  `MoodOffset()==0`). So a trait-flipped mood must be **one multi-stage def with a stage-routing
  worker**, not a penalty def plus a `requiredTraits` buff def — the latter shows a duplicate row.
  Personality exemptions stay declarative (`nullifyingTraits`/`nullifyingGenes`, with `MayRequire`
  on mod-specific entries) *except* a trait that must flip the sign, which has to route in the
  worker because nullification zeroes the whole def. See `Traits/ThoughtWorker_BloodStainedWeapon.cs`.
- **Reward pools are split in def space, not by Harmony.** Odyssey's `ThingSetMaker_UniqueWeapon`
  makes things with no stuff, which both errors on our stuffable weapons and dilutes the ranged pool.
  Every `*_Unique` weapon carries a `UMW_UniqueMelee` tag; an XPath patch repoints the two
  class-based vanilla consumers onto `ThingSetMaker_UMWUnique`, and our own pool filters on the tag.
  Tag-based makers (crates, fishing, map-gen loot) pass a stuff already and keep our weapons.
  The tag is also how C# asks "is this def one of ours?" (`UniqueWeaponDefs`, which owns the constant
  and the test — don't re-derive it from a defName prefix); changing it means changing the weapon defs,
  our pool def and that constant in lockstep. It is additionally a **published opt-in cross-mod
  contract** (like `stuff_adjective`): a third-party melee unique carrying the tag joins our pool,
  settings and exclusion machinery wholesale — semantics on `UniqueWeaponDefs.Tag`.
- **A def is kept out of the pools by filtering `ThingSetMakerUtility.CanGenerate`, never by removing
  the def.** That is the one choke point every `ThingSetMaker` funnels through, so the per-weapon
  settings toggles need a single postfix
  (`Patches/ThingSetMakerUtility_CanGenerate_Patch.cs`) to cover our pool and every tag-based maker at
  once (the repointed vanilla consumers exclude our weapons by construction and never route through the
  utility) — and estimates, "can this maker generate?" checks and saves that already contain the weapon
  all stay consistent.
- **Material must be surfaced explicitly**, because a unique name hides the stuff an ordinary label
  shows. `UniqueMeleeWeapon` adds an inspect-pane line and injects a `stuff_adjective` grammar
  symbol into name generation. That symbol is also a **dependency-free integration contract** with
  the companion mod (Unique Weapons Unbound): it publishes the material as that well-known symbol,
  we supply the grammar; neither mod references the other's code. Don't rename it. See
  `Patches/NameGenerator_StuffAdjective_Patch.cs`.
- **Startup def-writes and def caches re-run on every play-data load, not once per process.** A
  mid-session language change reloads all play data in-process and replaces every def instance;
  `[StaticConstructorOnStartup]` never re-runs, so anything it wrote onto defs goes stale. All such
  work therefore lives in `UMW_Startup.Run` (which must stay idempotent), invoked from
  `Patches/StaticConstructorOnStartupUtility_CallAll_Patch.cs` — its header carries the verified
  load ordering and the traps in full.

### Notable features

- **Warband quest** (`Source/1.6/Quests/`) — a low-tech tribal sibling of Odyssey's
  `AncientMercenaries` handing out our uniques, using a temporary hidden faction, our reward pool,
  reused vanilla tribal pawnkinds (no new ones) and a ruined tribal site. Rationale per difference
  is in `QuestNode_Root_Warband.cs`.
- **Wood-free material rolls** (`Patches/GenStuff_ExcludeWoodStuff_Patch.cs`) — setting-gated,
  filtering the single choke point every generation path funnels through, def-gated to our weapons.
- **Tribal trader stock** (`Traders/StockGenerator_UMWUniqueMelee.cs`,
  `Core/Settings/Settings_Traders.cs`) — two default-off toggles put uniques in the tribal war
  merchant's and shaman's stock at Royalty's bladelink rarity. Entirely runtime def-writes (a
  generator instance on the TraderKindDef plus a Sellable→All tradeability flip while on); nothing
  trader-related ships in XML. The two file headers carry the rationale, including why the war
  merchant's stock scope-bans the ultratech traits and the shaman's doesn't.

## Localization

English (Keyed files + def fields) is the source of truth; other languages derive from it via the
`/translate` skill (`.claude/skills/translate/SKILL.md` — this mod's translation surface, grounding
domain, and glossary; family-wide process lives in the `l10n/` submodule, see below) and are
validated deterministically by `python3 Scripts/check-translations.py` (also a CI release gate).
The DefInjected expected set is the checked-in sidecar `Scripts/expected-injections.json`: a dump
of every injection point the *live* game sees for this mod — including vanilla-inherited fields
(tool labels, `labelNounPretty`, `messageDefendersAttacking`) and C#-default comp strings
(`chargeNoun`, `cooldownGerund`) that never appear in this repo's XML — produced by
`Scripts/refresh-translation-expectations.py` driving the L10nProbe dev mod (source lives at
`l10n/probe/`; build/deploy it only from the canonical `~/dev/rimworld-l10n` checkout) through the
game's own walker. The checker refuses to run against stale expectations (any defName in `Defs/`
the sidecar has never seen, or label/description text that drifted), so new content forces a regen
and the regen sees everything the game sees; the release skill regenerates every release, which
also covers vanilla updates changing inherited text under unchanged defNames. The public language
roster lives in CONTRIBUTING.md and must move in the same commit as any language change.

- **Shared l10n toolkit (`l10n/` submodule):** the family-wide translation process, per-language
  mechanics references, cross-language lessons, Workshop conventions, and the checker/refresh
  script engines live in the `rimworld-l10n` repo, consumed here as the `l10n/` git submodule
  (canonical working checkout: `~/dev/rimworld-l10n`). `Scripts/check-translations.py` and
  `Scripts/refresh-translation-expectations.py` are thin per-repo config shims over its engines. If
  `l10n/` is empty, run `git submodule update --init`. Never edit `l10n/` in place here:
  mod-independent learnings go upstream in the canonical checkout, then the pin is bumped in each
  consuming repo; mod-specific learnings (this mod's coined weapon-trait/name-grammar vocabulary)
  go in this repo's skill/glossary.

**Workshop title coupling:** each language's `UMW_SettingsCategory` Keyed value is the localized
Steam Workshop title and must equal the title line (line 1) of
`.steamworkshop/Description/<Language>.txt` — always change the two together (English keeps
`Unique Melee Weapons` in both).

**Optional-DLC content ships from LoadFolders-gated compat roots**, because MayRequire is honored
on defs but IGNORED on DefInjected entries, and textures have no node to carry one at all — so the
load root *is* the gate (`IfModActive`, which is a LoadFolders attribute and unrelated to
MayRequire). Every well-known content folder is scanned once per active load root
(`ModContentPack.GetAllFilesForMod` loops `foldersToLoadDescendingOrder`), so gating works for
Textures exactly as it does for Defs and Languages. There are **two** roots per optional DLC,
mirroring the ungated `/` + `1.6` split:

- `Mods/<Name>/` — version-independent content: **art**. Same reasoning that puts the main
  `Textures/` tree at the repo root; nesting it under `1.6/` would make a future `1.7/` either
  duplicate the PNGs or point a version block back at `1.6/`.
- `1.6/Mods/<Name>/` — version-specific content: `Defs`, and the `Languages` that must sit in the
  same load root as the defs they target.

Currently only `Royalty`, for the unique Axe/Warhammer ThingDefs, their textures, and their
Royalty-tech WeaponTraitDefs/ColorDefs. `texPath` is **unaffected by which root the art lives in**:
textures are keyed by their path relative to `Textures/` in one flat per-mod dictionary merged
across all roots, so both the def's `texPath` and `Graphic_RandomComplex`'s folder enumeration
(`ContentFinder.GetAllInFolder`) resolve the same either way. A move therefore needs no def edit —
only the matching `_ModFiles` glob in `StageMod` (a miss deploys nothing and shows pink boxes
in-game, with no build error).

Compat roots must sit beside the well-known folders, never inside one — anything under `1.6/Defs/**`
or `1.6/Languages/**` loads unconditionally at any depth. A compat root's language files must not
reuse a main-tree file's language-relative path (the game dedups per mod by that path and silently
skips one whole file — caught pre-release in 2026-08 when all 9 languages' main-tree
weapon/trait/colour injections silently failed to load in-game); compat-root files carry a
`_Royalty` suffix. The checker validates key parity, placeholders, DefInjected legality, load-root placement
(an entry must live in the same load root as the def it targets), cross-root file-path collisions,
staleness, and file hygiene.

## Debugging

1. **Dev Mode:** Settings > Dev Mode > Logging.
2. **Log:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   (WSL: `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`).
3. **Logging convention:** `Log.Message("[Unique Melee Weapons] ...")`.
4. **Inspect the API:** `ilspycmd "/mnt/c/.../RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t "Namespace.ClassName"`.
5. **Startup smoke test (pre-release):** `python3 Scripts/integration-smoke-test.py` (game closed) boots UMW with its family siblings (UWU, PWU) on a pinned list, then classifies Player.log errors by origin and fails on anything attributed to UMW or a family seam. Run before every release (wired into the release skill); thin shim over the shared engine in `l10n/smoke/` (born from the BetterTradersGuild v1.1.0 CWTL incident).
