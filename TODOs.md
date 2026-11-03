# TODOs

- Re-upload the recompressed gallery images to the Workshop page
- Test earlier revisions of VFEP unique masks since 1.7 rolled DXT5->BC7
- Rewrite ComplexCutoutMix to GLSL source under 1.7
- Update/Add workshop page images with new VFEP uniques/katanas
- Test CE compat in-game (checklist: `Docs/Specs/Combat-Extended-Compat.md` §7)
- CE, non-English games: ship DefInjected for the tools CE relabels (spear head and shaft-1,
  warcasket broadsword edge) once the probe can boot official CE on 1.7; today they show in
  English (`1.6/Mods/CombatExtended/Patches/WeaponTraits_CE.xml` header, "Tool labels under CE")

- **Test the eltex trio in-game** (eltex-plated, eltex-fitted, lead-lined; implemented 2026-11-04,
  built and probe-booted, not eyeballed): the checklist is `Docs/Specs/Eltex-Plated-Trait.md` §7.
  Headline items: both card lines and the pawn's "Relevant gear" breakdown per trait, Plated's
  value moving with quality, a Plated + Fitted co-roll rendering pale body over shade fittings,
  Plated never beside Lead-lined, no eltex trait in war-merchant stock, the three rows under the
  Royalty settings header, and the colours against a VPE eltex sword.

## Features

- **Reply to the eltex Workshop commenter** (Simplified Chinese): trait(s) yes, eltex cost no (UWU's
  domain). English draft in the spec §8.
- Explore mace and axe trait roster depth
- Consider Mod integration
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally via steam (1814383360). Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.
