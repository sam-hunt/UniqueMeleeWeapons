# TODOs

## Fix

- VFEP uniques shouldn't have quality
- Review player-facing strings

## Features

- Ship the `UMW_CutoutComplexMix` shader (spike verified in game 2026-09; everything needed is in the gitignored `Docs/shader-spike/`, start with `HANDOVER.md` section 3A). It renders masks as a weighted mix instead of vanilla's stacked multiply, so feathered or anti-aliased red|green mask edges draw the blend the artist painted. Pure texels are bit-identical to vanilla. Steps: build the win/linux/mac bundles from the Windows editor (Mac mono module now installed) and commit them as binaries under `1.6/AssetBundles/`; restore the csproj extensionless `AssetBundles/*` glob (excluding `.manifest`); add the two ShaderTypeDefs and the `shaderType` swap on all 10 uniques; add one Harmony prefix on the 4-arg `ShaderDatabase.LoadShader` that evicts our stale cache entries on reload, probes bundle presence and `isSupported` for both shaders, and on any failure rewrites BOTH paths to vanilla `CutoutComplex`/`CutoutComplexUI` (never `Cutout`); flip the CLAUDE.md "tints stack" sentence, document the bundle content type and the fallback/reload trap; smoke test plus the language-switch reload on Windows; one info-card screenshot each from a Mac and a Linux tester. Third-party weapons and UWU stay on `CutoutComplex`, contracts unchanged.
- New katana texture for longsword? Explore ideo's stylable axis
- Explore mace and axe trait roster depth
- Consider Mod integration
  - VFE Pirates: consider an option to stock our higher-tech-level (Industrial+) uniques, such as the warcasket broadsword and gravity hammer, at a higher-tech trader, e.g. the orbital or caravan combat supplier (VFEP already puts its exotic warcasket weapons there). The tribal traders and Warband are capped at `UniqueWeaponDefs.TribalTechCap`, so these uniques currently reach players only through rewards and loot. A second `StockGenerator_UMWUniqueMelee` instance with a tech floor would do it. See `Docs/Specs/VFEP-Warcasket-Uniques.md`.
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.
