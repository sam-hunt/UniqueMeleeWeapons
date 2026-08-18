# Japanese — Unique Melee Weapons glossary

From UWU's 2026-07 machine-assisted generation, extended by this repo's own
2026-07 melee/quest pass. Family-shared mechanics (no `LanguageWorker_Japanese`
exists; ASCII punctuation; the corner-bracket-vs-ASCII-quote split; battle-log
grammar; name-generation grammar; `stuffProps.stuffAdjective`'s `〜製` suffix;
register-by-def-type; DLC names staying Latin) live in the `l10n/` submodule
at `l10n/languages/Japanese.md` — this file holds only what is specific to
Unique Melee Weapons' weapon domain. RimWorld's language folder is `Japanese`
(tar: `Japanese (日本語).tar`).

**`traitAdjectives` (this mod's own WeaponTraitDef field) must be attributive
forms** ending in の / な / い or a plain attributive verb (Odyssey ships 探知の,
正確な, 灼熱の) — the JP namer concatenates with no space, so a bare noun reads
broken. This is the worked application of the generic "attributive slot"
finding (upstream) to this mod's own field.

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器の特性) | 特性・特徴 | `WeaponTraits` / `StatsReport_WeaponTraits` / Odyssey `Stat_ThingUniqueWeaponTrait_Label`; 特性・特徴 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`) and belongs to PWU's domain, not ours |
| unique weapon | ユニークな武器 | | vanilla `UniqueWeapon`, Odyssey `*_Unique` labels |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| monosword / plasmasword / zeushammer | モノソード / プラズマソード / ゼウスハンマー | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | ロングソード / スピア / メイス / ナイフ / グラディウス / 戦斧 / ウォーハンマー | | Core/Odyssey/Royalty labels (mostly katakana, not 長剣/槍) |
| plasteel / jade / wood (stuff adjectives) | プラスチール製 / ヒスイ製 / 木製 | 塑鋼, 翡翠 | Core `stuffProps.stuffAdjective` |
| mechanite / mechanoid | メカナイト / メカノイド | | Royalty, Odyssey descs |
| wielder / bearer | 使用者 / 持ち主 | | Odyssey `EMPPulser` desc |
| stun / EMP / stagger | スタン / EMP / よろめき | | `StunnedByEMP`, `StaggerDurationFactor` |
| armor penetration / bleed rate / move speed | アーマー貫通力 / 出血量 / 移動速度 | | Core Keyed + StatDefs |
| cut / stab (DamageDef) | 斬る / 刺す | 切創, 刺し傷 (those are the *hediff* labels) | Core DamageDefs vs HediffDefs differ |
| bandaged / sutured / set / cut off / cut out | 包帯 / 縫合 / セット / 切り落とされた / 切り取られた | | Core `Cut`/`Stab` injury comps |
| toxic buildup | 毒物が蓄積 | | Core `ToxicBuildup` |
| item stash / bandit camp / ancient mercenaries / sealed crate | 埋蔵品 / 盗賊の野営地 / 古代の傭兵 / 密封されたクレート | | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement / tribesfolk / chief | 放棄された集落 / 蛮族 / 族長 | | Core `AbandonedSettlement`, `TribeRough` |

The six Odyssey trait ports (`Lightweight`, `Cumbersome`, `Ornamental`,
`Ugly`, `GoldInlay`, `JadeInlay`) have official JP labels, adjectives and — for
four of them — descriptions that our English matches word for word; copy them
rather than retranslating.

## Mod-decided terms pending native review (from the 2026-07 commit)

受け流し (parry, register-matched to `TextMote_Dodge` 回避), 戦士団 (warband,
parallel to vanilla 傭兵団), 襲撃団 (war party), 頭目 (warlord), 鍔 / クロスガード
(quillons / crossguard), 地響き (earthshake), 鼓舞の叫び (rallying cry),
士気高揚 (rallied), 由緒ある (storied), 杭打ちヘッド (piledriver), アヘン塗布
(opiated), 琺瑯 (enameled), 無反発 (dead-blow, from the real tool term
無反発ハンマー), 稜付き (flanged), 鋲打ち (studded), 徹甲スパイク (armor
spike), 先重心 (head-weighted), 素早い (quickdraw — vanilla's 早撃ちの is
ranged-specific and wrong on melee). The 2026-07-30 WeaponCategoryDef labels
are likewise mod-decided: 格闘 (melee, Core skill label), 斬る / 刺す / 殴る
(bladed / pointed / blunt, the Core DamageDef labels), 重量 (heavy), 鍔付き
(guarded).
