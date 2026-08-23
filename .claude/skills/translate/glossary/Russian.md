# Russian — Unique Melee Weapons glossary

From UWU PR #6's native review, mirrored into this repo. Family-shared
mechanics (LanguageWorker behavior, style/corpus rules, vanilla-grounded
common vocabulary, and the reviewed `Cancel`/inspect-string rows) live in
the `l10n/` submodule at `l10n/languages/Russian.md` — this file holds only
what is specific to Unique Melee Weapons' weapon domain.

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | свойство | черта | vanilla `WeaponTraits`=Свойства; черта = pawn personality traits |
| charge (weapons) | энерг- root | заряд- | vanilla `Gun_ChargeRifle`=энерговинтовка; заряд reads as ammo |
| breach axe | штурмовой топор | взломной топор | Core `MeleeWeapon_BreachAxe.label`, verbatim — the vanilla ru pack's own coinage, so reuse rather than re-derive |

## Mod-decided WeaponCategoryDef labels (pending native review, 2026-07-30)

ближний бой (melee, Core skill label), рубящее / колющее / дробящее (bladed /
pointed / blunt, the ToolCapacityDef adjective family), тяжёлое (heavy),
с гардой (guarded — prepositional, matching the reviewed с крестовиной).

## Mod-decided terms pending native review

- **`UMW_BreachAxe_Unique` namerLabels.0/2 (2026-08-23):** `breacher` →
  проломщик, `breaker` → крушитель. Both are coined agent/instrument nouns
  (English source itself coins them for the name generator) rather than
  vanilla-grounded terms. проломщик is a natural Russian agent noun built on
  пролом (a breach/gap made by force) + the -щик agentive suffix, distinct
  from namerLabels.1's топор (axe, grounded to the existing Royalty axe
  entry) and namerLabels.3's навершие (head, grounded to the mace entry's
  head-namer term). крушитель ("breaker/destroyer/crusher") is an existing
  Russian word rather than a further coinage, chosen to read as a distinct
  near-synonym alongside проломщик without overlapping it semantically.
  Note `tools.head.label` for this weapon renders as лезвие (matching the
  official vanilla Core string for the breach axe's head tool) — a
  deliberate divergence from the namerLabels.3 "head" pool item, which
  instead grounds to the file's existing head-namer convention (навершие).
