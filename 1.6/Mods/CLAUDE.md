# Compat roots (`1.6/Mods/<Name>/` and the root `Mods/<Name>/`)

Optional-DLC and third-party content ships from LoadFolders-gated compat roots because `MayRequire`
is honored on defs but IGNORED on DefInjected entries, and textures have no node to carry one at
all; so the load root *is* the gate (`IfModActive` by packageId, a LoadFolders attribute unrelated
to `MayRequire`). Every well-known content folder is scanned once per active load root, so gating
works for Textures exactly as for Defs and Languages. `LoadFolders.xml` lists the roots and why.

**Two roots per optional mod**, mirroring the ungated `/` + `1.6` split:

- `Mods/<Name>/` (repo root): version-independent content, i.e. **art**, for the same reason the
  main `Textures/` tree is at the repo root. `texPath` is unaffected by which root the art lives in
  (textures are keyed by their path relative to `Textures/` in one flat per-mod dictionary merged
  across roots), so a move needs no def edit, only the matching `_ModFiles` glob in `StageMod`.
- `1.6/Mods/<Name>/`: version-specific content: `Defs`, `Patches`, and the `Languages` that must sit
  in the same load root as the defs they target. Defs here follow `1.6/Defs/CLAUDE.md`.

Current roots: `Royalty` (the unique Axe/Warhammer, their art, their Royalty-tech traits and
colours); `VanillaFactionsExpandedPirates` (the two non-stuffable warcasket uniques, warcasket-only
via VEF's inherited `HeavyWeapon` extension, their art, and the mix shader's ShaderTypeDefs and
bundles; the def headers cover the inherited VEF extensions and the dropped crate graphic);
`VanillaTexturesExpanded` (version root only: a Patches-only root re-posing the unique spear to
match VTE's redrawn vanilla spear, with the drafted-idle grip nudge riding
`CarriedWeaponOffsetExtension`, our only def hook for a pose vanilla hard-codes);
`CombatExtended` (version root only, Patches-only: the armor-penetration traits retuned to CE's
penetration model, the Stab clones given CE's layer field, the ragged-wound clones given CE's
injury overhaul, the plasma-cored proc chance mirrored from CE's plasmasword and the envenomed
stab rerouted onto CE's wound-borne venom, with the audit of every other on-hit payload and of
the player-facing wording in those patch headers; the C# half is the reflection-only hooks under
`Source/1.6/Patches/CombatExtended/`, whose header is the full account of what CE's verb bypasses
and why base weapon stats need no work).

Rules:

- **Roots sit beside the well-known folders, never inside one.** Anything under `1.6/Defs/**` or
  `1.6/Languages/**` loads unconditionally at any depth.
- **A compat root's language files must not reuse a main-tree file's language-relative path.** The
  game dedups per mod by that path and silently skips one whole file (caught pre-release in
  2026-08, when every language's main-tree injections silently failed to load). Compat-root files
  carry a `_Royalty` (or mod-name) suffix; the checker validates this.
- **A setting that gates a patch is restart-to-apply, and its row must say so.** The VTE root is
  switchable from a Compatibility setting via `PatchOperation_UMWSetting`, which works because `Mod`
  subclasses are created before XML patches apply.
- **Package ids live in several places that must agree:** the `IfModActive` value in
  `LoadFolders.xml`, the C# gate constant beside the feature it enables (`VtePackageId` in
  `Settings_Compat.cs`, `VfepPackageId` in `ShaderDatabase_LoadShader_Fallback_Patch.cs`,
  `CePackageId` in `Patches/CombatExtended/CombatExtendedCompat.cs`), the
  `loadAfter` entry in `About.xml` when our defs inherit from that mod's, and the settings
  window's integration table (`Source/1.6/Core/ModIntegrations.cs`), which also lists by defName
  the weapons and traits each root ships so their settings rows can group under that mod's name
  while the mod is missing: a def added to a root is added there too, or its row lands ungrouped. Gate C# with
  `ignorePostfix: true` (root `CLAUDE.md`), or a Workshop copy beside a local one hides the setting
  row while the root still loads.
- A base weapon from a third-party mod needs the `Name=` add in `1.6/Patches/` and the `loadAfter`
  entry (`1.6/Defs/CLAUDE.md`).
- **A def that only a compat root consumes is still declared in the main tree when the probe
  cannot boot that root's mod.** The translation checker validates every def's injections against
  the sidecar the L10nProbe dumps on the local install, and a def declared under a root whose gate
  no probe boot activates is an error with no fix (official CE is 1.6-only; the install is 1.7).
  Declare the def in `1.6/Defs/`, keep it vanilla-legal there, and let the root's Patches add the
  foreign comp and point the consumer at it; `HediffDefs/VenomousStab.xml` is the worked case and
  says why in its header.
