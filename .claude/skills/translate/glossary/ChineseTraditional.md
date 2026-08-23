# Traditional Chinese — Unique Melee Weapons glossary

From this repo's 2026-08-19 machine-assisted generation (no native review
yet). Family-shared mechanics (no Chinese LanguageWorker of either script;
「」 quoting; full-width ：in terse templates — both INVERTED from zh-Hans;
ASCII spaces around Latin acronyms; dash baseline; the zh-Hans/zh-Hant
term-inversion table) and vanilla-grounded common vocabulary live in the
`l10n/` submodule at `l10n/languages/ChineseTraditional.md` — read that file
first; this one holds only what is specific to Unique Melee Weapons.
RimWorld's language folder is `ChineseTraditional` (tar: `ChineseTraditional
(繁體中文).tar`). **Never derive a zh-Hant term from the zh-Hans glossary** —
several of its "Use" values are zh-Hant's "Never" (特化武器, 玻璃钢, 钉头锤,
匕首-as-knife) and vice versa.

## Weapon-domain vocabulary (vanilla-grounded)

| English | Use | Never | Why |
|---|---|---|---|
| unique weapon | 獨特武器 | 特化武器 | Odyssey `UniqueWeapon` |
| weapon trait (noun / stats title) | 特質 / 武器特質 | | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` (Keyed `WeaponTraits`=特性 exists but the stats slots are our nearer analog) |
| longsword / spear / mace / knife / gladius / axe / warhammer | 長劍 / 長矛 / 錘子 / 小刀 / 短劍 / 戰斧 / 戰錘 | 釘頭錘, 匕首 (as base labels) | Core/Odyssey/Royalty `MeleeWeapon_*` labels; 匕首 survives only as a knife namer synonym |
| breach axe | 破城斧 (tools: 握柄/斧頭) | | Core `MeleeWeapon_BreachAxe.label`/`tools.handle.label`/`tools.head.label`, verbatim |
| monosword / plasmasword / zeushammer | 單分子劍 / 等離子劍 / 宙斯錘 | | Royalty labels; plasma in prose = 電漿, EMP stays ASCII-spaced |
| wielder (stat context) / holder (flavour) | 使用者 / 持有者 | | Royalty `SpeedBoost` / `OnKill_*` descs |
| stun / stunned by EMP | 昏迷 (DamageDef), verb 擊昏 | 擊暈 | Core `Stun.label`, `StunnedByEMP` |
| "Traders will pay more/less for it." | 商人會為其支付更多錢。/ 商人付出的價格較低 | | Odyssey `GoldInlay` / `Ugly` descs, verbatim |

The universal Odyssey-ported traits reuse the official zh-Hant translations
verbatim where the English matches: 裝飾性 (ornamental), 醜陋 (ugly), 輕巧
(lightweight), 笨拙 (cumbersome), 鑲金 (gold inlay), and their official
traitAdjectives (黃金/金, 怪異/粗糙/醜陋, 笨拙/笨重, 輕盈/速擊).

## Mod-decided terms pending native review (2026-08-19)

格擋 (parry; register-matched to `TextMote_Dodge` 閃避), 戰團 (warband), 戰幫
(war party), 劍格 (quillons; also the Quilloned trait label), 十字護手
(crossguard, prose), 撼地 (earthshake), 鼓舞吶喊 (rallying cry), 士氣大振
(rallied), 傳世 (storied), 打樁頭 (piledriver), 鴉片塗層 (opiated label;
鴉片, never 阿片), 琺瑯 (enameled), 無回彈錘頭 (dead-blow), 鑲翡翠 (jade
inlay, extending official 鑲金; jade=翡翠 per Core), 遠古傭兵 (ancient
mercenaries — vanilla zh-Hant left the Odyssey quest untranslated; 遠古 wins
the corpus 416:38 over 古代), 鎮靜劑累積 (sedative buildup; buildup=累積 per
corpus), 毒刺 / 麻醉刺 (toxic/tranq stab DamageDefs, on vanilla's 毒抓/毒咬
compounding pattern), 撕裂割傷 / 撕裂刺傷 / 撕裂疤痕 (ragged wounds, from
vanilla `Crack`=撕裂傷), colour names 血紅 / 碳黑 / 琺瑯紫 / 電漿橙 / 單分子白
(drop-色 compound pattern per Odyssey 電磁藍/火焰橙; colour names use
prose-register 電漿 even though the trait label uses 等離子, mirroring
vanilla's own label/prose split). Ultratech trait labels: 單分子刃 /
等離子內芯 / 宙斯錘頭 (label register follows 等離子劍; descriptions use
電漿). "Thunderous" is deliberately split: 雷霆 (ZeusHeaded, Zeus's bolt) vs
雷鳴 (Piledriver, from the official Namer's badass list).
WeaponCategoryDef labels: 格鬥 (melee, Core skill label), 刃器 / 尖器 / 鈍器
(bladed / pointed / blunt), 沉重 (heavy), 護手 (guarded). Faction pawns:
戰士 (EN "warrior", not 部落民 — that renders EN "tribesfolk" only).
Localized Workshop title / `UMW_SettingsCategory`: 獨特近戰武器.
`UMW_BreachAxe_Unique` namer synonyms 破城者 (breacher) / 拆城者 (breaker):
both pair a verb with 城 (fortress/city, from the weapon's own vanilla label
破城斧) plus the agentive 者 suffix already used elsewhere in the corpus
(使用者, 持有者, 打撈者) — 破 (break/breach) vs 拆 (tear down/dismantle)
keeps the two synonyms distinct while both read as natural nouns that
compose with a material adjective ([stuff_adjective]破城者). `axe`
grounds to the existing `UMW_Axe_Unique` namer's own base term 戰斧; `head`
grounds to vanilla `MeleeWeapon_BreachAxe.tools.head.label` 斧頭 (not the
mace/warhammer's 錘頭, since the breach axe's head-tool is an axe head).

## `traitAdjectives` composition rules

Bare attributive words, no trailing 之 or 的 (official: GoldInlay 黃金/金,
Ugly 怪異/粗糙/醜陋, Cumbersome 笨拙/笨重) — each must read directly before a
weapon noun (黃金長劍) and standalone. Avoid weak single characters. The
namer's own `badass_adjective`-family epithets DO end in 之 (嚴酷之/雷鳴之) —
that suffix belongs to rulesStrings entries, never to traitAdjectives.

## Name grammar

No spaces around [symbols]; [weapon_adjective][weapon_noun] composes
directly; "The X of Y" → [Y]之[X]; person possessives take 的
([ANYPAWN_nameIndef]的[weapon_noun]); English "The" is dropped. Material
names compose directly: [stuff_adjective][weapon_noun] → 鋼鐵長劍,
[stuff_adjective]之[badass_noun] → 翡翠之獠牙. All per the official zh-Hant
`NamerUniqueWeapon` (see the l10n language file).

## Battle-log grammar

[skillAdv] entries end in 地 and slot before the verb
([RECIPIENT_definite][skillAdvMaybe]用劍格架開了…); [RECIPIENT_possessive] is
idiomatically dropped, as in vanilla zh.
