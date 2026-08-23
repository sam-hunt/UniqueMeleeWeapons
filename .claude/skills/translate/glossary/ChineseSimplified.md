# Simplified Chinese — Unique Melee Weapons glossary

From this repo's 2026-07 machine-assisted generation. Family-shared mechanics
(no `LanguageWorker_ChineseSimplified` authoring requirements; full-width
punctuation and quoting rules; terse label templates; job report strings;
em dash handling; unit spacing) and vanilla-grounded common vocabulary
(trade/settlement/faction terms, quality tiers, materials, tech levels,
ideoligion/relic, general UI) live in the `l10n/` submodule at
`l10n/languages/ChineseSimplified.md` — this file holds only what is specific
to Unique Melee Weapons' weapon domain. RimWorld's language folder is
`ChineseSimplified` (tar: `ChineseSimplified (简体中文).tar`).

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器特性) | — | Odyssey `WeaponTraits` / `StatsReport_WeaponTraits` |
| unique weapon | 特化武器 | 独特武器 | Odyssey `UniqueWeapon` |
| monosword / plasmasword / zeushammer | 单分子剑 / 等离子剑 / 宙斯锤 | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | 长剑 / 长矛 / 钉头锤 / 匕首 / 短剑 / 战斧 / 战锤 | | Core/Odyssey/Royalty labels |
| breach axe | 破墙斧 (handle 握柄, head 斧头) | | Core `MeleeWeapon_BreachAxe` — official zh, verbatim |
| wielder (stat context) / bearer (flavour prose) | 使用者 / 持有者 | | Royalty `SpeedBoost`, Odyssey `EMPPulser` descs |
| stun / EMP | 击晕 / 电磁脉冲 (prose may keep "EMP") | | Core damage defs; zeushammer desc uses EMP冲击 |

## `traitAdjectives` composition rules

`traitAdjectives` (this mod's own WeaponTraitDef field) are bare attributive
words with no trailing 的: the zh Odyssey namer composes both
[weapon_adjective]的[weapon_noun] and [weapon_adjective][weapon_type], so
each must read both ways. Avoid weak single characters (快 → 迅疾).

## Name grammar

No spaces around [symbols]; zh links with 的 and 之 and drops English "The"
("The X of Y" → Y之X). Material names compose directly:
[stuff_adjective][weapon_noun] → 钢铁长剑, [stuff_adjective]之[badass_noun]
→ 翡翠之獠牙.

## Battle-log grammar

zh [skillAdv] entries end in 地, so an optional [skillAdvMaybe] slots cleanly
before the verb; [RECIPIENT_possessive] is idiomatically dropped (vanilla zh
does the same).

## Mod-decided terms pending native review (from the 2026-07 commit)

格挡 (parry, register-matched to `TextMote_Dodge` 闪避), 战团 (warband), 战帮
(war party), 剑格 / 十字护手 (quillons / crossguard), 撼地 (earthshake),
鼓舞呐喊 (rallying cry), 士气大振 (rallied), 传世 (storied), 打桩头
(piledriver), 阿片 (opiated), 珐琅 (enameled), 无回弹 (dead-blow), 破拆者 /
破坏者 (the breach axe's coined `namerLabels` nouns for "breacher" /
"breaker" — 破拆者 evokes a person who breaches/forces entry, 破坏者 a more
general destroyer/wrecker; distinct nouns chosen so the two compose
differently in generated names, both grounded in the def's own
demolition-tool fiction rather than combat). The
2026-07-30 WeaponCategoryDef labels are likewise mod-decided: 格斗 (melee,
Core skill label), 刃器 / 尖器 / 钝器 (bladed / pointed / blunt — 刃器/钝器
are established weapon-class terms, 尖器 a coined parallel), 沉重 (heavy),
护手 (guarded).
