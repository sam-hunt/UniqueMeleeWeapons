# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-08-19

### Added

- Traditional Chinese translation (machine-assisted; corrections welcome).

### Fixed

- Weapon, trait and colour names stayed English in every non-English language.
- Possible startup errors for players without Royalty.
- Changing language mid-session blanked trait effect lines, kept the old language's
  weapon names in the settings, and reverted def-overriding settings.

## [1.0.2] - 2026-08-06

### Changed

- Needle point now costs a slower swing (+5% melee cooldown) instead of a wielder
  hit-chance penalty, and shows on the weapon's own stat list.

### Fixed

- Trait effect lines no longer count as untranslated, which had made every language
  report 18 missing keys.

### Performance

- Dropped a Harmony patch that ran on every wielder stat lookup to serve one trait.

## [1.0.1] - 2026-08-03

### Changed

- Hardened Savage Warband spawning against missing pawns or an absent site map,
  logging a warning when a spawn is skipped.

## [1.0.0] - 2026-08-03

Initial release.

### Added

- Seven stuffable unique melee weapons: knife, gladius, longsword, spear and mace,
  plus axe and warhammer with Royalty. Each rolls its own material, art variant,
  colours, name and traits.
- 28 melee-built traits across six weapon categories, including on-hit effects,
  per-tool damage and armour penetration, a defender-side parry, forced body colours,
  and two granted abilities (Earthshake, Rallying Cry).
- Savage Warband quest, a tribal sibling of Odyssey's Ancient Mercenaries, as a
  dedicated acquisition route. Melee uniques roll from their own reward pool.
- Mod settings: per-weapon toggles, Royalty-trait toggle, wood exclusion, quest
  commonality, and ability cooldown/radius sliders.
- Translations for Simplified Chinese, French, German, Japanese, Korean, Brazilian
  Portuguese, Russian and Spanish.

[1.1.0]: https://github.com/sam-hunt/UniqueMeleeWeapons/releases/tag/v1.1.0
[1.0.2]: https://github.com/sam-hunt/UniqueMeleeWeapons/releases/tag/v1.0.2
[1.0.1]: https://github.com/sam-hunt/UniqueMeleeWeapons/releases/tag/v1.0.1
[1.0.0]: https://github.com/sam-hunt/UniqueMeleeWeapons/releases/tag/v1.0.0
