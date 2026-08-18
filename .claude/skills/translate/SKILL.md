---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla + Royalty RimWorld melee-weapon terminology for Unique Melee Weapons' weapon-trait / unique-weapon domain. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Unique Melee Weapons. English is
the source of truth; every other language derives from it.

**The family-wide process lives in the `l10n/` submodule — load these first,
and only these** (progressive disclosure; if `l10n/` is empty, run
`git submodule update --init`):

- `l10n/process.md` — non-negotiables, file/format conventions, terminology
  grounding method, and the generation / update / audit workflows. This is
  the workflow authority; follow it step by step.
- `l10n/languages/<Language>.md` — the target language's engine mechanics,
  style rules, and vanilla-grounded common vocabulary. Read ONLY the target
  language's file.
- `glossary/<Language>.md` (beside this file) — this mod's own coined-term
  table and `RulePackDef` naming-grammar findings for the target language.
  Read it in the same pass.
- `l10n/lessons.md` — cross-language lessons; read when generating a new
  language, skim otherwise. Its RulePackDef-specific lessons section is
  directly relevant here (this mod ships name-generation grammar).
- `l10n/workshop.md` — Steam Workshop description/title conventions;
  `.steamworkshop/README.md` names this mod's anchor term and title-coupling
  key (`UMW_SettingsCategory`).

**Where learnings land:** mod-independent findings (engine mechanics, a
language's grammar rule, corpus style facts, generic RulePackDef naming
techniques) go in the `l10n/` submodule — edit the canonical checkout at
`~/dev/rimworld-l10n`, commit there, then bump the pin here. Mod-specific
findings (coined terms, phrasing decisions, and worked `namerLabels`/
`traitAdjectives` applications tied to this mod's own defs) go in
`glossary/<Language>.md`.

## This mod's translation surface

- English Keyed source: `1.6/Languages/English/Keyed/UMW_UI.xml` (settings
  window), `UMW_Stats.xml` (info-card trait-effect lines), and
  `UMW_Combat.xml` (combat/battle-log strings). Every key is `UMW_`-prefixed.
- Most player-facing text lives in the defs themselves (`1.6/Defs/**`) —
  weapon/trait/hediff/thought/ability labels and descriptions, quest letter
  text, and `RulePackDef` name-grammar rulesStrings — and is translated per
  language via DefInjected, not Keyed. There is no English DefInjected tree
  at all (English is served by the def XML's own `<label>`/`<description>`),
  so **enumerate the DefInjected target key set from the
  `Scripts/expected-injections.json` sidecar, never from
  `1.6/Languages/English/`** — the sidecar is what catches vanilla-inherited
  fields (tool labels, `labelNounPretty`, `messageDefendersAttacking`) and
  C#-default comp strings (`chargeNoun`, `cooldownGerund`) that never appear
  in this repo's own XML.
- Def types currently in the sidecar: `AbilityDef`, `ColorDef`, `DamageDef`,
  `FactionDef`, `HediffDef`, `QuestScriptDef`, `RulePackDef`, `SitePartDef`,
  `SoundDef`, `ThingDef`, `ThoughtDef`, `WeaponCategoryDef`,
  `WeaponTraitDef` — all resolve bare (no namespace-prefixed DefInjected
  folders needed today; this mod defines no Def subclasses of its own).
- **Gated compat load root:** the Royalty-gated
  `UMW_Warhammer_Unique`/`UMW_Axe_Unique` (`ThingDef`),
  `UMW_ZeusHeaded`/`UMW_PlasmaCored`/`UMW_Monomolecular` (`WeaponTraitDef`),
  and `UMW_PlasmaOrange`/`UMW_MonoWhite` (`ColorDef`) entries live under
  `1.6/Mods/Royalty/Languages/<Language>/...` — MayRequire is ignored on
  DefInjected, so the folder is the gate. Route each gated def's
  translations to that root, never the main `1.6` tree; the checker enforces
  the placement both ways.

## This mod's grounding domain

Domain DLC: **Royalty** (plus Core) — the source for the ultratech melee
traits' vanilla weapons (monosword, plasmasword, zeushammer) and their
gated compat root. Terms that MUST be grounded before use: weapon trait,
unique weapon, the base melee weapon names this mod mirrors (longsword,
spear, mace, knife, gladius, axe, warhammer), quality tiers, material/stuff
names, Royalty's ultratech melee weapons for the ultratech-trait
descriptions, damage/condition terms (EMP, stun, burn, bleeding), and the
opportunity-site quest vocabulary (ancient mercenaries, bandit camp, item
stash) this mod's own warband quest models itself on. The vanilla-grounded
answers live in `l10n/languages/<Language>.md`; this mod's coined terms
(weapon-trait epithets, the WeaponCategoryDef labels, parry/warband/war-party
vocabulary, ...) live in `glossary/<Language>.md`. All eight shipped
languages have grounded tables; a ninth language starts from nothing and
gets its terms grounded and recorded per `l10n/process.md`.

**This mod ships `RulePackDef` name-generation grammar** (weapon naming via
`namerLabels`/`traitAdjectives` on top of Odyssey's `NamerUniqueWeapon`, plus
`NamerStuffAdjectives.xml`), so the per-language glossary's RulePackDef
section is load-bearing, not optional — read it before authoring or
translating either field. The generic naming-grammar *techniques* (German's
inline gender markers, Spanish's parallel symbol families, French's
rule-level constraints, Portuguese's literal hedge) live in
`l10n/languages/<Language>.md` and `l10n/lessons.md`; only the worked
application to this mod's own `namerLabels`/`traitAdjectives`/`stuff_adjective`
symbols lives in the glossary.

## Workflows

Follow `l10n/process.md`'s Initial generation / Update pass / Audit-only
workflows verbatim. This mod's specifics on top:

- The checker: `python3 Scripts/check-translations.py` (`--strict` for new
  languages). Sidecar regen: `python3
  Scripts/refresh-translation-expectations.py` (game must be closed; drives
  the deployed L10nProbe).
- Compat-root routing per the surface section above; the checker's
  missing-entry errors name the root a translation belongs under.
- `UMW_SettingsCategory` is that language's localized Workshop title and
  must stay in sync with the title line of
  `.steamworkshop/Description/<Language>.txt` — change both together.
- The public roster (and credits) is CONTRIBUTING.md's localization table —
  update it in the same commit as any language addition or native review.
