# TODOs

## Before release

- Test the generation fixes (rc.1 + follow-ups), UWU off: spawn uniques until one rolls Carbonized and check it starts at the lowered max (80/80 for steel; plasteel 224/224 vs 280/280 plain), and that the inspect-pane max agrees with the info card; dev-add Carbonized to a plain unique ("Add trait to unique weapon") and expect HP to drop to the new max at once; load a pre-fix save holding a Carbonized unique and expect it clamped to its max; a warcasket unique without Storied shows NO Art tab, one with Storied (rolled or dev-added) does; with UMW above VFEP in the mod list, confirm the red load-order error and that no warcasket rows appear in settings
  (material adjectives in names: confirmed 2026-10-02; 100/100 on Carbonized was the immutable MaxHitPoints cache, fixed 2026-10-02)
- Check the compare link on the v1.4.0 after v1.4.0-rc.n
  it could not be confirmed from GitHub's docs whether auto-generated notes use the previous prerelease or the previous stable as their base. The workflow comment says prereleases count, from experience. If the first promotion shows v1.3.0...v1.4.0 instead of rc.N...v1.4.0, soften that comment.
- replicate rc release/ci support across sibling repos

## Features

- New katana texture for longsword? Explore ideo's stylable axis
- Explore mace and axe trait roster depth
- Consider Mod integration
  - Alpha Armory? Installed locally
  - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.
