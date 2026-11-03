# TODOs

- Re-upload the recompressed gallery images to the Workshop page
- Test earlier revisions of VFEP unique masks since 1.7 rolled DXT5->BC7
- Rewrite ComplexCutoutMix to GLSL source under 1.7
- Update/Add workshop page images with new VFEP uniques/katanas
- Test CE compat in-game (checklist: `Docs/Specs/Combat-Extended-Compat.md` §7)
- CE, non-English games: ship DefInjected for the tools CE relabels (spear head and shaft-1,
  warcasket broadsword edge) once the probe can boot official CE on 1.7; today they show in
  English (`1.6/Mods/CombatExtended/Patches/WeaponTraits_CE.xml` header, "Tool labels under CE")

## Features

- **Ability mod settings subsection headers** showing the ability gizmo texture and description tooltip,
  our family warcasket mods' settings pages are the precedent
- **Eltex-plated trait** (Workshop request, 2026-10-20): implement per `Docs/Specs/Eltex-Plated-Trait.md`
  after its §6 open questions (ultratech veto, tokens, numbers, colour) are decided. Royalty root,
  zero C#: `statOffsets` on `PsychicSensitivityOffset` + `PsychicEntropyRecoveryRateOffset`, which
  vanilla routes from the wielded weapon (1.6/Defs/CLAUDE.md "Gear-routed pawn stats").
- **Reply to the eltex Workshop commenter** (Simplified Chinese): trait(s) yes, eltex cost no (UWU's
  domain). English draft in the spec §8.
- Review the other two weapon-routed psychic stats as trait vehicles: `PsychicEntropyMaxOffset`
  (a heat-sink trait) and a negative `PsychicSensitivityFactor` (the first wielder-side malus that
  is still a weapon stat). Scan results and sketches in the spec §9.
- Explore mace and axe trait roster depth
- Consider Mod integration
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally via steam (1814383360). Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.

## Settings window

The 2026-10-22 family audit's findings were settled on 2026-11-03 (family rules refined in the
template: a gated row hides only when it tunes a compat shim for the other mod's own content or is
derived from absent data; colour table added). Left over:

- **In-game check of the reworked window** (grouped Weapons and Traits sections with their two-step
  indent, the bare-name Royalty and VFE Pirates headers with and without those mods loaded and the
  "(not active)" suffix, the held-on last-trait row, click-and-drag painting across the def rows
  and past a group header, the def rows' tooltips showing the weapon or trait block in yellow, the
  coloured names in the earthshake and VTE tooltips; the 2026-11-03 grids: medium-font group
  headers with the box centred, the two-column weapon cells with their wide mask icons at the gold
  inlay over steel and dimmed when off, the three-column trait cells, a clamped held-on trait label
  and the suffix line at the end of its tooltip, every ungrouped section's rows and sliders sitting
  one step under their header with the box not clipped, and the warcasket pair's icon at the same
  footprint as the rest with VFE Pirates loaded): the rework was built and smoke-tested, not
  eyeballed.
