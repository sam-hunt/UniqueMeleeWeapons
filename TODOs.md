# TODOs

- Re-upload the recompressed gallery images to the Workshop page
- Test earlier revisions of VFEP unique masks since 1.7 rolled DXT5->BC7
- Rewrite ComplexCutoutMix to GLSL source
- Update/Add workshop page images with new VFEP uniques/katanas
- Test CE compat in-game (checklist: `Docs/Specs/Combat-Extended-Compat.md` §7)
- CE, non-English games: ship DefInjected for the tools CE relabels (spear head and shaft-1,
  warcasket broadsword edge) once the probe can boot official CE on 1.7; today they show in
  English (`1.6/Mods/CombatExtended/Patches/WeaponTraits_CE.xml` header, "Tool labels under CE")
- fix ragged seams in `1PreviewWorkshop.png`, notes in `Docs/Research/preview-halo-seams/NOTES.md`

## Features

- Eltex-plated trait (Workshop request, 2026-10-20): implement per `Docs/Specs/Eltex-Plated-Trait.md`
  after its §6 open questions (ultratech veto, tokens, numbers, colour) are decided. Royalty root,
  zero C#: `statOffsets` on `PsychicSensitivityOffset` + `PsychicEntropyRecoveryRateOffset`, which
  vanilla routes from the wielded weapon (1.6/Defs/CLAUDE.md "Gear-routed pawn stats").
- Reply to the eltex Workshop commenter (Simplified Chinese): trait yes, eltex cost no (UWU's
  domain). English draft in the spec §8; translate at reply time like the Russian precedent in
  `Docs/workshop-comment-translations/`.
- Review the other two weapon-routed psychic stats as trait vehicles: `PsychicEntropyMaxOffset`
  (a heat-sink trait) and a negative `PsychicSensitivityFactor` (the first wielder-side malus that
  is still a weapon stat). Scan results and sketches in the spec §9.
- Explore mace and axe trait roster depth
- Consider Mod integration
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.

## Settings window

Findings against the family's settings window rules (`~/dev/RimworldModTemplate`, `Source/<version>/Settings/CLAUDE.md`).

- **VTE spear compat section hidden without Vanilla Textures Expanded** (`Source/1.6/Core/Settings/Settings_Compat.cs:50`): show it disabled, forced off, requirement stated.
- **Ultratech traits toggle hidden without Royalty** (`Settings_Generation.cs:41`): show it disabled, forced off, "(requires Royalty)".
- **Royalty weapons' rows absent without Royalty** (`Settings_Weapons.cs:72`): the rows come from defs, so there is nothing to label one with; decide whether this is the exception.
- **Header comment prescribes hiding DLC rows** (`UniqueMeleeWeaponsSettings.cs:24-27`): rewrite it to the disable-and-force pattern.
- **VTE's name uncoloured** in a label and tooltip (`Settings_Compat.cs:60`, `:62`): pick a colour.
- **"Royalty's" and "Odyssey's" uncoloured** in tooltips (`UMW_UI.xml:37`, `:73`): decide whether passing mentions count.
