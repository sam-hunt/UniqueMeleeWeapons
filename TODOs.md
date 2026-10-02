# TODOs

## Before release

- cut v1.4.0-rc.1 and eyeball the gh release/changelog
- Test v1.4.0-rc.1 on linux/mac with VFEP
- Test the PostMake fixes in rc.1: with UWU off, dev-spawn a Carbonized unique (starts 80/80, not 100/80), check a few names show a material adjective, and open a warcasket unique's Art tab (inscription present); with UMW above VFEP in the mod list, confirm the red load-order error and that no warcasket rows appear in settings
- Check the compare link on the v1.4.0 after v1.4.0-rc.n
  it could not be confirmed from GitHub's docs whether auto-generated notes use the previous prerelease or the previous stable as their base. The workflow comment says prereleases count, from experience. If the first promotion shows v1.3.0...v1.4.0 instead of rc.N...v1.4.0, soften that comment.
- replicate rc release/ci support across sibling repos

## Features

- New katana texture for longsword? Explore ideo's stylable axis
- Explore mace and axe trait roster depth
- Consider Mod integration
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.
