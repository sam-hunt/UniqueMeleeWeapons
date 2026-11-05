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

## Royalty eltex-theme traits (coined, machine-assisted)

eltex 灵质 / psychic sensitivity 心灵敏感度 / neural heat 心灵熵 (limit 心灵熵阈值)
are Royalty-verbatim. Labels follow the 单分子刃 compound-noun precedent:
eltex-plated 灵质镀层, eltex-fitted 灵质配件, lead-lined 铅内衬 (lead 铅,
ordinary word). Descriptions (2026-11 rewrite) reuse Royalty: psychic focusing material
心灵聚合物 (`Apparel_PsyfocusRobe`/`Shirt`/`Vest`), enhance psychic sensitivity
增加心灵敏感度, dissipate neural heat 驱散心灵熵 (`Apparel_EltexSkullcap`),
suppresses 抑制 (`PsychicSensitivityDownMinor`). Parts: grip 握柄 (Core),
fittings 配件 (ordinary word). Colours: eltex mint 灵质薄荷绿, eltex green
灵质绿 (绿 from Core `Structure_Green`; 薄荷绿 the ordinary word, unattested
in vanilla).

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

2026-10-02 (VFE Pirates compat root, `Weapons_Unique_VFEP.xml`): warcasket =
战棺, the family term shared with the sibling Warcasket mods, taken from the
community pack `q847633684/Vanilla-Expanded-ZH` (the Workshop page's 无畏战棺
prefix deliberately not used). Labels follow that pack's word order with 特化
in front: 特化战棺巨剑 / 特化战棺重力锤. `namerLabels` (no gender or marker
conventions in zh): broadsword = 巨剑 / 利刃 / 剑 / 剑锋; gravity hammer = 锤 /
巨锤 / 锤头 / 重力锤. Tool labels reuse this file's grounded rows for the other
uniques (剑刃 / 剑尖, 锤头 / 锤柄) rather than the pack's 握柄.

2026-11 (Combat Extended compat): venomous stab (the `UMW_VenomousStab` wound) = 淬毒刺伤, the trait's 淬毒 plus Core `Stab` 刺伤, kept distinct from the damage label 毒刺伤; its scar reuses Core `Stab` permanentLabel 刺伤疤痕. Info-card joiners: applies {1} = ，附带{1} (hediff 中毒 etc.), leaving {1} = ，造成{1}.

## 2026-11-06 player-text pass (register rewrite of 47 entries; machine-assisted, pending native review)

- Unique weapon description frame is Odyssey's verbatim: 一把配有定制部件的X。 (spear keeps 一杆; warhammer moved from 一柄 to 一把 after vanilla Zeushammer). Cumbersome: 该武器操作笨重，挥击速度较慢。
- Stagger = 抑止状态 (Core `StaggerDurationFactor` 从被攻击而速度减慢的抑止状态下恢复), not 打乱脚步 or 踉跄. Braced wielder = 稳住架势的使用者. Wood as a material noun = 原木 (Core `WoodLog`), adjective 木制; weapon labels injected bare (木制{0}, no quotes). Tech bands 中世纪及以下科技等级 / 工业时代及以上科技等级 (及以上 from `NormalQualityOrBetter`).
- UI section names take corner brackets 「武器」; a cited quest name takes curly quotes “古代雇佣兵”. Radius and reach sliders open on a noun phrase (冲击波的波及范围, 呐喊的传播范围), never a "How far" clause. Trader caravans stay 商队（{0}）; 远行队 is the player's own caravan.
