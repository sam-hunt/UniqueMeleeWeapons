# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project Overview

**Unique Melee Weapons** is a RimWorld 1.6 and 1.7 mod adding individually-designed unique melee
weapons: stuffable variants of vanilla melee weapons that roll random traits, colours and names.
Requires Harmony and the Odyssey DLC (a few traits additionally `MayRequire` Royalty).
C# (.NET Framework 4.7.2), Harmony, XML defs.

### Where documentation lives

Three layers, read inward:

1. **This file:** cross-cutting rules only, plus the pointers below.
2. **Domain guidance**, autoloaded when you work under its folder:
   - `Source/1.6/CLAUDE.md` — C#: patching, startup and load, stat caches, the trait extension
     layer, settings.
   - `1.6/Defs/CLAUDE.md` — weapon and trait design rules and def-level traps. Also governs
     `1.6/Patches/` and the defs under compat roots.
   - `1.6/Mods/CLAUDE.md` — optional-DLC and third-party compat roots, including the art under the
     root `Mods/`.
   - `1.6/Languages/CLAUDE.md` — localization process, the checker and its sidecar. Also governs
     `Scripts/`.
   - `Textures/CLAUDE.md` — the double-mask art rules and the mix shader.
3. **File headers:** per-item values, tuning numbers, decompile-verified call paths and the history
   of a mechanism (what was tried and why it was dropped) live in the header comment of the file they
   describe: every `.cs`, every `WeaponCategoryDef`, every non-obvious trait/hediff/damage def. Put
   the *why* there; add a line to a guidance file only when it constrains work in other files, and
   never restate def values or call paths in guidance (they drift). Where a guidance bullet names a
   file, that file's header is the full story.

Research notes, specs and the vanilla reference defs live in the gitignored `Docs/`.

## Build Commands

```bash
# Build (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build UniqueMeleeWeapons.sln -c Release

# Stage the mod into an arbitrary folder (used by CI; same manifest as the local deploy)
dotnet build Source/1.6/UniqueMeleeWeapons.csproj -c Release \
  -t:StageMod -p:StageDir=/path/to/output/UniqueMeleeWeapons
```

The build compiles against the `Krafs.Rimworld.Ref` package pinned to the final 1.6 build,
locally and in CI; the local install is only the deploy target and the decompile target. The
install path (auto-detected, or `RIMWORLD_PATH`; under WSL point it at the Windows install in
`~/.bashrc`) decides only whether the build deploys, gated on the game's `Mods/` folder.

### Deployment

The repo lives outside the Mods folder; every local Release build redeploys atomically.

- **One manifest:** the `_ModFiles` ItemGroup in the `StageMod` target of
  `Source/1.6/UniqueMeleeWeapons.csproj` (its comments cover the globs and exclusions). Local deploy
  and CI release both call it. It is generic over folders; only a brand-new *file type* needs a
  line. Every staged `.xml` is rewritten without its comments: repo files are untouched, the checker
  and the probe read the repo, and the smoke test boots the stripped copy. A compat-root glob miss
  deploys nothing and fails silently in-game (pink boxes), never at build.
- **Asset bundles are committed binaries** (CI has no Unity): the mix shader's three OS bundles
  under the VFEP compat root. Rebuild them with `Source/UnityShaders/build.sh` whenever RimWorld
  moves Unity version; its header pins the version and modules. `Source/` never deploys.
- **Stop hook** (`.claude/hooks/sync-mod.sh`, wired by the tracked `.claude/settings.json`)
  rebuilds and redeploys after any turn that changed mod files, logging to
  `$TMPDIR/UniqueMeleeWeapons-build.log`. A failure exits 2 with the errors so the turn continues;
  a second failure in the same turn only warns. It is byte-identical across the mod family: change
  it in the template and copy it verbatim. Its `find` watch list must cover every content root
  `StageMod` ships, or edits under a missed root silently stop redeploying.
- **`.claude/` is only partly gitignored:** `skills/`, `hooks/` and `settings.json` are tracked and
  shared; `settings.local.json` stays local. A skill is a committed instruction coupled to what it
  automates: `/release` step 7 encodes the CHANGELOG layout and the version scheme (`X.Y.Z-rc.N`
  candidates are CHANGELOG-less and Workshop-less, with the suffix only in `modVersion` and
  `AssemblyInformationalVersion`; `release.yml` treats any suffixed tag as a prerelease), and
  `/translate`'s glossary encodes per-language terminology. Change the thing and the skill together.

## Conventions

- **Naming:** every def carries the `UMW_` prefix; a unique variant of a vanilla weapon is the base
  name plus `_Unique` (`UMW_LongSword_Unique`), mirroring Odyssey. Root C# namespace
  `UniqueMeleeWeapons`, patch classes under `.Patches`. Art lives at the repo root (never under a
  version folder) in Odyssey's per-weapon folder layout.
- **Warnings are build errors** (`TreatWarningsAsErrors`, locally, in the Stop hook and in CI).
  Severities are pinned in `.editorconfig`: `warning` blocks, `suggestion` is IDE-only. Fix the
  code, not the severity, unless the rule is wrong for this domain.
- **Every user-facing string is localized** through `.Translate()` against the Keyed files, except
  where vanilla already localizes the exact string (reuse the vanilla key or def label); a string
  that names game content injects the def label as a placeholder. Keyed files split by purpose:
  `UMW_UI.xml` settings, `UMW_Combat.xml` floating combat text, `UMW_Stats.xml` info-card lines.
- **No em dashes in player-facing text** (def labels and descriptions, `Keyed/`, `About.xml`);
  reflow the sentence. Guidance files, code comments and def comments are unaffected.
- **Gate on packageId, never on display name.** XML: `IfModActive` / `MayRequire`, never
  `PatchOperationFindMod`. C#: `ModLister.GetActiveModWithIdentifier(id, ignorePostfix: true)`,
  never `ModsConfig.IsActive`. A Workshop copy installed beside a local one gets a `_steam` suffix
  that `IsActive` matches literally while the XML gates ignore it, so the two disagree exactly on a
  developer's machine and the C# side silently stays off.
- **Logging:** `Log.Message("[Unique Melee Weapons] ...")`.

## Published cross-mod contracts

Three names are consumed by Unique Weapons Unbound or by third-party weapons with no assembly
reference either way, so a rename compiles clean here and silently breaks them there. Don't rename;
each owner's header has the details.

- **`UMW_UniqueMelee` thingSetMakerTag** (`Core/UniqueWeaponDefs.cs` owns the constant and the
  "is this def ours?" test; never re-derive it from a defName prefix). A third-party melee unique
  carrying it joins our pools, settings and exclusion machinery wholesale. It moves with the weapon
  defs and our pool def in lockstep.
- **`stuff_adjective` grammar symbol** (`Patches/NameGenerator_StuffAdjective_Patch.cs`): UWU
  publishes the material, we supply the grammar.
- **`TraitEffectLinesExtension`** (`Traits/TraitEffectLinesExtension.cs`): duck-typed by simple
  type name and its public `List<string> lines` field.

## Feature map

- **Warband quest** (`Source/1.6/Quests/`): a low-tech tribal sibling of Odyssey's
  `AncientMercenaries`, handing out our uniques via a temporary hidden faction. Rationale per
  difference in `QuestNode_Root_Warband.cs`.
- **Trader stock** (`Traders/StockGenerator_UMWUniqueMelee.cs`, `Core/Settings/Settings_Traders.cs`):
  default-off toggles, entirely runtime def-writes; two tech bands partition the roster.
- **Wood-free material rolls** (`Patches/GenStuff_ExcludeWoodStuff_Patch.cs`): setting-gated.
- **Optional-content compat roots** for Royalty, VFE Pirates and Vanilla Textures Expanded:
  `1.6/Mods/CLAUDE.md`.

## Debugging

1. **Dev Mode:** Settings > Dev Mode > Logging.
2. **Log:** under `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`
   (WSL: `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/`). On 1.7 it
   is `Logs/Player.log` with timestamps and levels; on 1.6 it is `Player.log` one level up, which a
   1.7 install leaves in place, stale.
3. **Inspect the API:** `ilspycmd "$RIMWORLD_PATH/Assembly-CSharp.dll" -t "Namespace.ClassName"`
   (flat in the install root on 1.7, under `RimWorldWin64_Data/Managed/` on 1.6). The local install
   is now 1.7, so a call path verified on 1.6 should say so if re-verified on 1.7 and different.
   The `rimworld-logs` skill covers both.
4. **Startup smoke test (pre-release):** `python3 Scripts/integration-smoke-test.py` (game closed)
   boots UMW with its family siblings on a pinned list and fails on any Player.log error attributed
   to UMW or a family seam. Wired into the release skill; thin shim over `l10n/smoke/`.
