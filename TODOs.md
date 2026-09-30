# TODOs

## Fix

- Review player-facing strings

## Features

- Mix shader follow-up: confirm the mid-session language-switch reload keeps the unique weapons' masks on Windows (before the fallback patch they dropped to plain Cutout with a `Could not load shader UMW/CutoutComplexMix` warning); get one info-card screenshot of a stuffable unique from a Mac and a Linux tester (their bundles are built and verified but untested in game; a failure falls back to vanilla CutoutComplex and logs a `[Unique Melee Weapons] Shader ...` warning). Spike notes in the gitignored `Docs/shader-spike/HANDOVER.md`.
- New katana texture for longsword? Explore ideo's stylable axis
- Explore mace and axe trait roster depth
- Consider Mod integration
  - VFE Pirates: consider an option to stock our higher-tech-level (Industrial+) uniques, such as the warcasket broadsword and gravity hammer, at a higher-tech trader, e.g. the orbital or caravan combat supplier (VFEP already puts its exotic warcasket weapons there). The tribal traders and Warband are capped at `UniqueWeaponDefs.TribalTechCap`, so these uniques currently reach players only through rewards and loot. A second `StockGenerator_UMWUniqueMelee` instance with a tech floor would do it. See `Docs/Specs/VFEP-Warcasket-Uniques.md`.
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.
