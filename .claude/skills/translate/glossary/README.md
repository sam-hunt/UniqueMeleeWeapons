# Glossary — UMW-specific terminology

These per-language files (`Russian.md`, `Japanese.md`, `ChineseSimplified.md`,
`Korean.md`, `German.md`, `Spanish.md`, `French.md`,
`PortugueseBrazilian.md`) hold everything about a language's translation
that is specific to Unique Melee Weapons: weapon-trait and unique-weapon
vocabulary, the base melee weapon names, tool-part labels, DamageDef/
HediffDef splits for this mod's weapon domain, the mod-decided
WeaponCategoryDef labels, the `RulePackDef` naming-grammar requirements
tied to this mod's own `namerLabels`/`traitAdjectives` fields (worked
against Odyssey's `NamerUniqueWeapon` and this mod's own weapon roster),
battle-log tense/register findings for this mod's combat content, and the
mod-decided terms pending native review (parry, warband, war party,
piledriver, quilloned, and the rest).

Family-shared, mod-independent findings — LanguageWorker mechanics, style
and corpus rules, and vanilla-grounded common vocabulary (trader,
settlement, goodwill, quest, quality tiers, tech levels, and so on) — live
upstream in the `l10n/` submodule at `l10n/languages/<Language>.md`
(canonical checkout: `~/dev/rimworld-l10n`), since they apply to any mod in
the family, not just this one. The same is true of the generic RulePackDef
naming-grammar *techniques* (German's inline `|M|`/`|F|`/`|N|` markers,
Spanish's parallel symbol families, French's rule-level constraints,
Portuguese's literal `(a)` hedge) — only the worked application of a
technique to this mod's own def fields belongs here.

When a future translation pass coins a new UMW-specific term, record it
here. If a pass instead surfaces a correction to shared mechanics or
vocabulary, send that fix upstream to the l10n repo rather than duplicating
it here.
