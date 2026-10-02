# TODOs

## Before release

- Test the generation fixes (rc.1 + follow-up), UWU off: spawn a unique and dev-add Carbonized ("Add trait to unique weapon"), expect HP to drop to 80/80 at once, not 100/80; roll a few Carbonized by spawning repeatedly and check they start 80/80; a warcasket unique without Storied shows NO Art tab, one with Storied (rolled or dev-added) does; with UMW above VFEP in the mod list, confirm the red load-order error and that no warcasket rows appear in settings
  (material adjectives in names: confirmed 2026-10-02)
- Check the compare link on the v1.4.0 after v1.4.0-rc.n
  it could not be confirmed from GitHub's docs whether auto-generated notes use the previous prerelease or the previous stable as their base. The workflow comment says prereleases count, from experience. If the first promotion shows v1.3.0...v1.4.0 instead of rc.N...v1.4.0, soften that comment.
- replicate rc release/ci support across sibling repos

## Features

- New katana texture for longsword? Explore ideo's stylable axis
- Explore mace and axe trait roster depth
- Consider Mod integration
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.
