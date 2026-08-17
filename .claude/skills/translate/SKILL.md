---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla RimWorld terminology. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Unique Melee Weapons. English is
the source of truth; every other language derives from it.

## Non-negotiables

- **Run the checker first and last.** `python3 Scripts/check-translations.py`
  validates key sets, placeholders, DefInjected paths, staleness, and file
  hygiene deterministically. Never hand-derive anything it reports; never
  finish with it failing.
- **Community translations are owned by their contributors.** Update
  stale/missing keys in an existing language when asked, but do not rewrite a
  contributor's phrasing wholesale without the user's explicit direction.
- **Machine-assisted output is a first pass.** PRs and commits containing
  generated translations must say so and invite native-speaker review.
- **Keep the public roster current.** CONTRIBUTING.md's localization table
  (Planned / Machine-assisted / Native, plus credit) must be updated in the
  same commit whenever a language is added or a native review lands. The
  target roster lives there — consult it before proposing new languages.

## File map and conventions

- English Keyed source: `1.6/Languages/English/Keyed/UMW_UI.xml` (settings
  window) and `UMW_Stats.xml` (info-card trait-effect lines).
- Most player-facing text lives in the defs themselves
  (`1.6/Defs/**`) — weapon/trait/hediff/thought/ability labels and
  descriptions, quest letter text, name-grammar rulesStrings — and is
  translated per language via DefInjected, not Keyed.
- Target layout: `1.6/Languages/<Language>/Keyed/*.xml` and
  `1.6/Languages/<Language>/DefInjected/<DefTypeFolder>/*.xml`
- Gated compat load roots are additional language roots: the Royalty-gated
  `UMW_Warhammer_Unique`/`UMW_Axe_Unique` (`ThingDef`),
  `UMW_ZeusHeaded`/`UMW_PlasmaCored`/`UMW_Monomolecular` (`WeaponTraitDef`),
  and `UMW_PlasmaOrange`/`UMW_MonoWhite` (`ColorDef`) entries live under
  `1.6/Mods/Royalty/Languages/<Language>/...` — a folder LoadFolders.xml
  loads only when Royalty is active, because MayRequire is ignored on
  DefInjected entries, so the gate must be the folder. Each gated def's
  translations mirror its own root, never the main `1.6` tree (that would be
  a startup error whenever Royalty is inactive); the checker enforces the
  placement in both directions.
- `<DefTypeFolder>` must be the def's resolvable type name: bare for vanilla
  types (`ThingDef`, `WeaponTraitDef`, `HediffDef`, `AbilityDef`,
  `QuestScriptDef`, ...). This mod currently defines no Def subclasses of its
  own (audited 2026-07); if one is ever added, its folder must be
  **namespace-qualified** (`UniqueMeleeWeapons.<DefClass>`) — a bare custom
  name silently drops every translation in the folder.
- **The type folder is load-bearing, not organizational** (decompile-verified,
  `Verse.LoadedLanguage`): RimWorld enumerates only the top-level directories
  under `DefInjected/` and resolves each directory *name* to the def type its
  files target. An `.xml` placed directly in `DefInjected/` is never loaded,
  and the checker likewise iterates only directories — a misplaced file fails
  silently on both sides, so never flatten the tree. *Inside* a type folder
  everything is free: file names are arbitrary and files are found recursively,
  so one bundled file per type vs one-def-per-file is pure preference — this
  repo bundles per type, since reviewers work in whole-language passes and
  entries are found by their defName-prefixed keys, not by file. (The loader
  even tolerates a pluralized folder name by retrying with the last character
  stripped — `ThingDefs` → `ThingDef` — but the checker does not; use exact
  type names.)
- DefInjected keys are `DefName.field` paths (`UMW_LongSword_Unique.label`,
  `UMW_Earthshake.description`). Translate `label`, `description`, and the
  long tail of secondary fields this mod actually uses: `traitAdjectives`
  (all WeaponTraitDefs) and `namerLabels` (all weapon ThingDefs' comps),
  which feed generated unique names; hediff `labelNoun`, injury-comp labels
  (`labelTendedWell`, `permanentLabel`, `destroyedLabel`, ...) and stage
  labels; thought `stages` labels/descriptions; DamageDef `deathMessage`;
  FactionDef `pawnSingular`/`pawnsPlural`/`leaderTitle`; and quest
  `rulesStrings` grammar. The checker errors on any uncovered expected key
  (the `required` subset of the `Scripts/expected-injections.json` sidecar —
  see the next bullet) and on cross-language drift (a key translated in one
  language but missing in another); everything else it validates
  structurally once present.
- **Some translatable fields never appear in this repo's XML** and cannot be
  found by reading `Defs/`: tool labels (`UMW_*_Unique.tools.<tool>.label`)
  inherited from the vanilla base weapon defs;
  `comps.CompEquippableAbilityReloadable.chargeNoun`/`.cooldownGerund`
  (C# defaults "charge"/"on cooldown", reached via weapons' comps and via
  traits' `abilityProps`); `labelNounPretty` from vanilla `InjuryBase`; and
  `messageDefendersAttacking` from vanilla `FactionBase`. All of them — with
  exact keys and current English — are in the
  `Scripts/expected-injections.json` sidecar, a dump of every injection
  point the live game sees, regenerated by
  `Scripts/refresh-translation-expectations.py` (launches the game with the
  `../L10nProbe` dev mod). The checker enforces the sidecar's `required`
  subset per language and fails on stale expectations, so new content of
  *any* shape forces a regen rather than a hand-maintained manifest row.
  Ground these keys by copying the official translation of the matching
  vanilla def verbatim: base weapons' `tools.*.label` (Core/Royalty tars),
  Odyssey's `*_Unique` weapons for the two comp strings, Core `Cut`/`Stab`
  for `labelNounPretty` (keep its `{lookup: ...}` case grammar), Core
  `TribeRough` for the defenders-attacking message. `WeaponCategoryDef`
  labels have no vanilla translations (vanilla ships its own untranslated) —
  they are mod-decided terms; flag them for native review.
- The name-generation grammar (`RulePackDefs/`, and the `stuff_adjective`
  symbol it consumes) is translatable content, not fixed data: its
  rulesStrings carry English adjectives/nouns that each language rewrites to
  produce natural names in that language.
- **EN comment convention (required):** every translated entry carries the
  current English source directly above it:
  `<!-- EN: Reset to defaults -->` — this is how the checker detects
  staleness.
- Formatting: UTF-8 without BOM, LF endings, 2-space indent, final newline,
  root element `<LanguageData>`.
- Placeholders (`{0}`, `{1}`, named args) must match English exactly per key.
  Translator comments above placeholdered English keys explain what gets
  injected — injected values are lowercase def labels; phrase around them
  accordingly.

## Terminology grounding (do not skip)

Every game term must match the official localization, not a plausible
translation. Sources, in order:

1. Vanilla language data:
   `"$RIMWORLD_PATH"/Data/<Expansion>/Languages/<Language> (<Native>).tar`
   (read entries with `tar -xOf`). Check Core plus Odyssey (this mod's DLC),
   and Royalty (the `MayRequire`-gated ultratech traits borrow its melee
   kit).
2. This file's glossary below (lessons already learned — apply them).
3. If a term appears nowhere official, flag it in the PR for native review
   rather than inventing silently.

Terms that MUST be grounded before use: weapon trait, unique weapon, the
base melee weapon names we mirror (longsword, spear, mace, knife, and the
Royalty pair), quality tiers, material/stuff names (wood, plasteel, uranium,
jade, ...), Royalty's ultratech melee weapons for the ultratech trait
descriptions (English labels are fused lowercase words: "monosword",
"plasmasword", "zeushammer" — ground each language's forms from the Royalty
tar), damage/condition terms (EMP,
stun, burn, bleeding), and the opportunity-site quest vocabulary
(ancient mercenaries, bandit camp, item stash).

### Glossary — shared across the mod family

The RU and JP rows were learned in the companion mod (UWU) from native review
(RU) and vanilla-data study (JP); the Simplified Chinese, Korean, German, Spanish,
French and Brazilian Portuguese sections were learned in this repo's 2026-07
generations. Lessons
propagate across all three repos
(here, ../UniqueWeaponsUnbound, ../PersonaWeaponsUnbound): when a row is added
or corrected in one skill, mirror it into the siblings, adjusting
domain-specific rows. Add rows whenever a native review lands corrections.

#### Russian (from UWU PR #6 native review)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | свойство | черта | vanilla `WeaponTraits`=Свойства; черта = pawn personality traits |
| charge (weapons) | энерг- root | заряд- | vanilla `Gun_ChargeRifle`=энерговинтовка; заряд reads as ammo |
| Cancel (button) | Отменить | Отмена | vanilla `Cancel`; buttons use infinitive verbs |
| report/inspect strings | noun phrases | finite verbs | matches inspect-pane convention |

Mod-decided WeaponCategoryDef labels pending native review (2026-07-30):
ближний бой (melee, Core skill label), рубящее / колющее / дробящее (bladed /
pointed / blunt, the ToolCapacityDef adjective family), тяжёлое (heavy),
с гардой (guarded — prepositional, matching the reviewed с крестовиной).

#### Japanese (from UWU machine-assisted generation, 2026-07, extended by this
repo's melee/quest pass, 2026-07)

RimWorld's language folder is `Japanese` (tar: `Japanese (日本語).tar`).

Style rules discovered from the vanilla JP data (mandatory):

- Vanilla JP uses ASCII punctuation: `,` and `.` — never `、` or `。`.
- Descriptions/tooltips: polite です/ます form ending `.`; labels/buttons no
  period. Thought (`ThoughtDef` stage) descriptions are the exception — plain
  first-person form, no です/ます.
- Quote injected def labels and cross-referenced UI labels with 「」. Suffixes
  and parentheticals take no leading space and use ASCII parens.
- `traitAdjectives` are **attributive** forms ending in の / な / い / a verb
  (Odyssey ships 探知の, 正確な, 灼熱の). The JP namer concatenates with no
  space, so a bare noun reads broken.
- Name grammar: no spaces around [symbols]; "The X of Y" → `[Y]の[X]`; vanilla
  keeps `[RECIPIENT_possessive]` (unlike zh, which drops it).
- `stuffProps.stuffAdjective` is `〜製` (鉄製, プラスチール製, 木製, ヒスイ製),
  so `[stuff_adjective]の[noun]` composes cleanly — supply the の in our rules,
  matching vanilla's の-terminated trait adjectives.
- Battle-log entries end in plain past tense (よけた, 受け流した) and JP
  `[skillAdv]` values are adverbials (巧みに, ゆっくりと), so `[skillAdvMaybe]`
  slots directly before the verb.
- `deathMessage` keeps vanilla's space after the pawn token: `{0}は 斬られて…`.
- DLC names stay in Latin script (Odyssey, Royalty), as does MOD.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器の特性) | 特性・特徴 | `WeaponTraits` / `StatsReport_WeaponTraits` / Odyssey `Stat_ThingUniqueWeaponTrait_Label`; 特性・特徴 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`) and belongs to PWU's domain, not ours |
| unique weapon | ユニークな武器 | | vanilla `UniqueWeapon`, Odyssey `*_Unique` labels |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| Cancel / Reset / Reset to defaults | キャンセル / リセット / デフォルトに戻す | | vanilla Keyed buttons |
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
| humanlike / ability / quest / cooldown / cells | 人型 / 能力 / クエスト / クールダウン / セル | | Core Keyed |
| quality tiers | 壊れかけ/低品質/標準品/良品/秀品/名品/幻の一品 | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | 貿易商は高値で/低い価格でこれを買い取ります. | | Odyssey `GoldInlay`/`Ugly` descs — reuse verbatim |

The six Odyssey trait ports (`Lightweight`, `Cumbersome`, `Ornamental`,
`Ugly`, `GoldInlay`, `JadeInlay`) have official JP labels, adjectives and — for
four of them — descriptions that our English matches word for word; copy them
rather than retranslating.

Mod-decided terms pending native review (from the 2026-07 commit): 受け流し
(parry, register-matched to `TextMote_Dodge` 回避), 戦士団 (warband, parallel
to vanilla 傭兵団), 襲撃団 (war party), 頭目 (warlord), 鍔 / クロスガード
(quillons / crossguard), 地響き (earthshake), 鼓舞の叫び (rallying cry),
士気高揚 (rallied), 由緒ある (storied), 杭打ちヘッド (piledriver), アヘン塗布
(opiated), 琺瑯 (enameled), 無反発 (dead-blow, from the real tool term
無反発ハンマー), 稜付き (flanged), 鋲打ち (studded), 徹甲スパイク (armor
spike), 先重心 (head-weighted), 素早い (quickdraw — vanilla's 早撃ちの is
ranged-specific and wrong on melee). The 2026-07-30 WeaponCategoryDef labels
are likewise mod-decided: 格闘 (melee, Core skill label), 斬る / 刺す / 殴る
(bladed / pointed / blunt, the Core DamageDef labels), 重量 (heavy), 鍔付き
(guarded).

#### Simplified Chinese (from this repo's machine-assisted generation, 2026-07)

RimWorld's language folder is `ChineseSimplified` (tar: `ChineseSimplified
(简体中文).tar`) — the mod's folder must match it exactly, whatever the
public roster calls the language.

Style rules discovered from the vanilla zh data (mandatory):

- Full-width punctuation in prose (，。、；：（）……); descriptions end with 。;
  labels and buttons carry no trailing period. Placeholders, digits and units
  stay ASCII. Vanilla labels use full-width parens: 锻造台（燃料）.
- Quote cited names in prose with full-width curly quotes — vanilla writes
  任务“{0}”. Terse stat templates take no quotes ({0}伤害).
- `traitAdjectives` are bare attributive words with no trailing 的: the zh
  Odyssey namer composes both [weapon_adjective]的[weapon_noun] and
  [weapon_adjective][weapon_type], so each must read both ways. Avoid weak
  single characters (快 → 迅疾).
- Name grammar: no spaces around [symbols]; zh links with 的 and 之 and drops
  English "The" ("The X of Y" → Y之X). Material names compose directly:
  [stuff_adjective][weapon_noun] → 钢铁长剑, [stuff_adjective]之[badass_noun]
  → 翡翠之獠牙.
- Battle-log grammar: zh [skillAdv] entries end in 地, so an optional
  [skillAdvMaybe] slots cleanly before the verb; [RECIPIENT_possessive] is
  idiomatically dropped (vanilla zh does the same).
- Vanilla zh files can contain untranslated English values (Odyssey's
  ancient-mercenaries name symbols) — vanilla incompleteness is not style
  guidance. Some vanilla zh files carry a BOM; ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器特性) | — | Odyssey `WeaponTraits` / `StatsReport_WeaponTraits` |
| unique weapon | 特化武器 | 独特武器 | Odyssey `UniqueWeapon` |
| ultratech (attributive) | 极致科技 | 超科技 | `TechLevel_Ultra`=极致时代; `BodyPartsUltra`=极致科技 |
| monosword / plasmasword / zeushammer | 单分子剑 / 等离子剑 / 宙斯锤 | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | 长剑 / 长矛 / 钉头锤 / 匕首 / 短剑 / 战斧 / 战锤 | | Core/Odyssey/Royalty labels |
| plasteel | 玻璃钢 | 塑钢 | Core `Plasteel` — counterintuitive, always check |
| wood (material adjective) | 木 | | `WoodLog.stuffProps.stuffAdjective` |
| wielder (stat context) / bearer (flavour prose) | 使用者 / 持有者 | | Royalty `SpeedBoost`, Odyssey `EMPPulser` descs |
| stun / EMP | 击晕 / 电磁脉冲 (prose may keep "EMP") | | Core damage defs; zeushammer desc uses EMP冲击 |
| mechanoid | 机械族 | 机械体 | Core |
| item stash / bandit camp / ancient mercenaries | 物品藏匿点 / 匪徒营地 / 古代雇佣兵 | | Core sites, Odyssey quest |
| ancient (sealed) crate | 密封储物箱 | | Odyssey `AncientSealedCrate` |
| tribesfolk / tribal chief | 部众 / 酋长 | | Core `TribeRough` |
| quality tiers | 极差/较差/一般/良好/极佳/大师级/传奇级 | | Core `QualityCategory_*` |

Mod-decided terms pending native review (from the 2026-07 commit): 格挡
(parry, register-matched to `TextMote_Dodge` 闪避), 战团 (warband), 战帮
(war party), 剑格 / 十字护手 (quillons / crossguard), 撼地 (earthshake),
鼓舞呐喊 (rallying cry), 士气大振 (rallied), 传世 (storied), 打桩头
(piledriver), 阿片 (opiated), 珐琅 (enameled), 无回弹 (dead-blow). The
2026-07-30 WeaponCategoryDef labels are likewise mod-decided: 格斗 (melee,
Core skill label), 刃器 / 尖器 / 钝器 (bladed / pointed / blunt — 刃器/钝器
are established weapon-class terms, 尖器 a coined parallel), 沉重 (heavy),
护手 (guarded).

#### Korean (from this repo's machine-assisted generation, 2026-07)

RimWorld's language folder is `Korean` (tar: `Korean (한국어).tar`). Decompile-
verified why the paren-stripped name works: `LoadedLanguage` derives
`legacyFolderName` by cutting at `(`, and mod language dirs match on *either*
`folderName` or `legacyFolderName` — the same mechanism behind `Japanese`.

**Josa (particle) markers are the one hard mechanical rule Korean adds, and
nothing else in this skill has an equivalent.** Korean particles are
allomorphic: the correct form depends on whether the previous syllable ends in
a consonant, which is unknowable when the preceding text is an injected value.
`Verse.LanguageWorker_Korean.ReplaceJosa` (decompile-verified) resolves exactly
eight tokens, and no others:

```
(이)가   (와)과   (을)를   (은)는   (아)야   (이)어   (으)로   (이)
```

- Every *allomorphic* particle following `{0}`, `[symbol]` or `[TOKEN_x]` MUST use
  a marker. `{0}(을)를 생성` is correct; `{0}를 생성` breaks on consonant-final
  labels. Only five distinctions inflect (은/는, 이/가, 을/를, 와/과, 으로/로);
  **`에`, `에서` and `의` are invariant** — write those bare after a placeholder.
- Never hand-roll `{0}을(를)` — the worker does not recognize it.
- **Spelling is exact, and `(와)과` is asymmetric.** For every token the paren
  holds the post-*consonant* form — except `(와)과`, where `JosaPatternPaired`
  maps to `("과","와")`, so the paren holds the post-*vowel* form. `(과)와` matches
  nothing and ships literally.
- **A marker resolving off a digit is always wrong.** `HasJong()` falls back to
  `AlphabetEndPattern` = `{b,c,k,l,m,n,p,q,t}` for non-Korean chars, which has no
  digits, so a number always yields the vowel form — right for 2/4/5/9
  (이·사·오·구), wrong for 1(일) 3(삼) 6(육) 7(칠) 8(팔) 0(영). Phrase around it
  (`{1} x{2} 예약에 실패했습니다`), never mark it. Same list means a Latin tail is
  consonant-final only for those nine letters, so `Odyssey` → `y` → vowel form.
- **Quoting interacts with resolution.** `FindLastChar` skips a preceding `"`,
  `'` or `)` (walking back past the matching `(` and any spaces) to reach the real
  final character, so `"{0}"(을)를` resolves correctly. Curly `“ ”` and corner
  `「 」` are **not** skipped: they fall through to `default`, and
  `char.IsLetterOrDigit('”')` is false, so the token is returned unresolved and
  the raw `(은)는` shows on screen. Korean therefore needs no defensive quoting at
  all — josa does the job quoting does in ja/ru/zh.
- The one safe unmarked case, which vanilla ko itself uses: a symbol that always
  resolves the same way, e.g. `[refugee_pronoun]는` (Korean pronouns are always
  vowel-final). Def labels, pawn names, material words and numbers are never safe.
- A lint for this lives outside the repo checker (which is language-agnostic).
  It was calibrated to zero false positives against the vanilla ko Keyed corpus,
  Odyssey's WeaponTraitDefs and Core's DamageDefs. Four patterns fooled earlier
  drafts and must stay excluded: `(와)과의` (valid token plus ordinary trailing
  `의`), `기간 (일)` (a parenthetical unit, not a marker), bare `0으로` (no marker,
  so untouched and already correct), and `{2}(으)로` (correct authoring; only a
  *literal* digit before a marker is provably wrong).

Other style rules discovered from the vanilla ko data (mandatory):

- ASCII punctuation (`.` `,`), never `。`. Descriptions/tooltips take polite
  formal `-습니다.`/`-입니다.`; labels, buttons and stat fragments take no
  trailing period.
- `ThoughtDef` stage descriptions are the exception: casual first-person
  (`-어`, `-지`, `-군`, `-거야`), e.g. vanilla `이제 거의 깼어.`
- Battle-log rulesStrings end in the nominalized `-함.`/`-임.` form, not polite
  form (`Combat_Dodge`: `… [implement](을)를 [skillAdvMaybe] 피함.`).
- Korean **uses spaces**, unlike JP/zh: the ko namer composes
  `[weapon_adjective] [weapon_noun]` with a space, so `traitAdjectives` may be
  attributive verb forms (`가벼운`, `저주받은`) *or* bare noun modifiers
  (`황금`, `신속`, `특제`). Genitive epithets carry their own `의` (`죽음의`).
- Korean drops English "The" in name grammar and links with `의`
  (`[badass_concept]의 [weapon_type]`). Material composes bare:
  `[stuff_adjective] [weapon_noun]` → 강철 장검.
- Vanilla ko **drops `[RECIPIENT_possessive]`** in the combat packs — 12
  textual occurrences, all in EN comments, zero in Korean values. Korean omits
  possessive pronouns, so follow suit rather than rendering 그의.
- Units attach with no space: `{0}시간`, `{0}일`, `{0}칸`. Some vanilla ko
  files carry a BOM; ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 특성 (stats-entry title 무기 특성) | 개성 | Odyssey `WeaponTraits` / `Stat_ThingUniqueWeaponTrait_Label`; 개성 is Royalty's *persona* word (`Stat_Thing_PersonaWeaponTrait_Label`), PWU's domain |
| unique weapon | 고유 무기 | | Odyssey `UniqueWeapon` |
| **unique \<weapon\>** (label) | **특제 \<weapon\>** | | Odyssey's ranged uniques: 특제 장궁, 특제 돌격소총 |
| longsword / spear / mace / knife / gladius / axe / warhammer | 장검 / 창 / 철퇴 / 단검 / 검 / 도끼 / 전투망치 | | Core/Odyssey/Royalty labels |
| monosword / plasmasword / zeushammer | 단분자검 / 플라즈마검 / 제우스망치 | | Royalty labels |
| **mechanite(s)** | **기계입자** | 나노머신 | Core, 36/36 (근섬유질 기계입자); 나노머신 renders English *nanomachines* — a different word. Easy trap: they look interchangeable and are not |
| mechanoid | 메카노이드 | | Core |
| ultratech | 미래 (`TechLevel_Ultra`); 최첨단 attributively in prose | | monosword desc 최첨단 금속 검입니다 |
| plasteel / jade / wood / steel | 플라스틸 / 비취옥 (Odyssey inlay uses 옥) / 나무 · 목재 / 강철 | | Core labels + `stuffAdjective` |
| cut / stab (DamageDef) | 잘림 / 찔림 | 베임 (that is the *hediff* label) | Core DamageDefs vs HediffDefs differ |
| toxic \<damage\> label | `찔림 (독성)` shape | | Core `ScratchToxic`=찢김 (독성), `ToxicBite`=물림 (독성) |
| bandaged / sutured / set | 붕대 감음 / 봉합됨 / 접합됨 | | Core Cut/Stab injury comps |
| cut off / cut out | 끊어짐 / 잘림 | | Core `injuryProps` |
| toxic buildup / anesthetic | 중독 / 마취 | | Core |
| woozy / sedated | 혼미함 / 안정됨 | | Core `Anesthetic` stages; `-됨` is the hediff-stage family |
| point (tool) / edge (tool) | 칼끝 / 칼날 | 첨단 for "point" | Core tool labels; 첨단 reads "cutting-edge" (첨단 기술) in modern ko |
| armor penetration / move speed / stagger multiplier / bleeding | 방어 관통력 (melee: 근접 방어 관통력) / 이동속도 / 비틀거림 배수 / 출혈 | | Core StatDefs |
| Dodge (TextMote) | 회피 | | Core `TextMote_Dodge` |
| radius / cells / cooldown / ability / quest | 범위 / 칸 / 대기시간 / 능력 / 임무 | | Core Keyed |
| Cancel / Reset / Reset all | 취소 / 초기화 / 모두 초기화 | | Core Keyed |
| quality tiers | 끔찍/빈약/평범/상급/완벽/걸작/전설적 | | Core `QualityCategory_*` |
| item stash / bandit camp / ancient mercenaries / sealed crate | 귀중품 은닉처 / 도적 캠프 / 고대 용병들 / 밀봉된 상자 | | Core sites, Odyssey quest + `AncientSealedCrate` |
| tribesman / tribespeople / chief / fierce | 부족민 / 부족민들 / 족장 / 호전적인 | | Core `TribeRough` |
| wielder / bearer | 사용자 / 주인 | | Odyssey `EMPPulser`, Royalty descs |
| Traders will pay more/less for it. | 상인들이 더 높은 값을 쳐줍니다. / 상인들은 더 적은 돈을 쳐줍니다. | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim |

**Cross-checked against PWU's own ko pass (landed the same day, independently
grounded).** It reached the same josa conclusion, and adds two findings worth
reusing: `LanguageWorker_Korean.FindLastChar` skips a preceding `'`/`"`, so
quoting an injected label and *then* attaching a josa resolves correctly; and
`AlphabetEndPattern` contains no digits, so a josa directly after a number
always picks the no-batchim form and is wrong for 1/3/6/7/8/0 — phrase around
it. Two rows genuinely diverge, and neither repo should silently "fix" the other:

- **PWU's `mechanite` → 나노머신 row is wrong; use 기계입자.** PWU grounded on
  Royalty/Biotech only and concluded ko has no term for it. Core in fact has one
  in 7 files (`Hediffs_Local_Infections`, `Luciferium`, `Items_Exotic`, ...):
  all 36 English "mechanite" occurrences render 기계입자. 나노머신 is Core/Biotech's
  word for English *nanomachines*. Correct this when mirroring.
- **armor penetration: 방어 관통력 here, 관통력 in PWU — both correct.** Core
  Keyed `ArmorPenetration` is 관통력, but the StatDef this mod's info-card lines
  sit under is `MeleeWeapon_AverageArmorPenetration` = 근접 방어 관통력. Match
  whichever anchor the surrounding screen shows.

The six Odyssey trait ports have official ko labels/adjectives, and descriptions
that match our English verbatim for four of them (장식용, 난잡한 외형, 금 상감,
옥 상감); `Lightweight` 경량 and `Cumbersome` 불편 differ only in aim-vs-swing,
so adapt that clause alone. Note Odyssey's `Ugly` adjective *indices* differ
from ours: re-map by meaning (crude=조잡한, ugly=난잡한, monstrous=끔찍한).

Mod-decided terms pending native review (from the 2026-07 commit): 받아넘김
(parry, register-matched to `TextMote_Dodge` 회피), 전사단 (warband, parallel to
vanilla 용병단), 습격단 (war party), 두목 (warlord, distinct from Pirate 대장),
날받이 / 십자 가드 (quillons / crossguard), 지진 강타 (earthshake), 결집의 외침
(rallying cry), 결집됨 (rallied), 유서 있는 (storied), 항타기 (piledriver),
무반동 (dead-blow), 아편 도포 (opiated), 독 도포 (envenomed), 법랑 (enameled),
날개 돌기 (flanged), 징 박음 (studded), 관통 스파이크 (armor spike), 선단 편중
(head-weighted), 균형추 (counterweighted), 종 주조 (bell-cast), 바늘 끝 (needle
point), 미늘 (barbed, keeping 갈고리 for its "hooked" adjective), 탄화
(carbonized), 혈흔 (blood-stained), 톱니 (serrated), 면도날 (razored), 단분자 /
플라즈마 코어 / 제우스 헤드 (the ultratech trio), 진정제 축적 (sedative
buildup), 투여됨 (dosed), 찢긴 (ragged), 명장이 벼린 (master-forged), 도살도
(cleaver), 쇠메 (maul), 쇠뭉치 (mace head), 혈홍색 / 탄흑색 (colours, patterned
on Odyssey's 염홍색 / 전청색). The 2026-07-30 WeaponCategoryDef labels are
likewise mod-decided: 근접 (melee, from Core 근접 무기), 잘림 / 찔림 / 맞음
(bladed / pointed / blunt, the Core DamageDef labels), 중량 (heavy), 가드
(guarded, matching the mod's 십자 가드).

#### German (preseeded from PersonaWeaponsUnbound's 2026-07-28 generation,
generated and extended here 2026-07-28)

Base rows were ground against the de Core/Royalty/Ideology/Odyssey tars during
PWU's run; this repo's full melee/quest/naming pass confirmed them and resolved
the two questions PWU had left open (stuff naming, and whether `namerLabels`
need markers — both answered by vanilla data, see below). Language folder is
`German` (tar: `German (Deutsch).tar`).

Style rules from the vanilla de data (mandatory):

- **ASCII single quotes** for cited def labels and UI labels — vanilla writes
  `Forschungsprojekt '{0}'`. Core+Royalty Keyed ship 140 single-quoted
  placeholders and **zero** German `„…"`. Never use `„ "`, `» «`, or curly
  quotes. Pawn names are not quoted.
- **En dash `–`, never em dash `—`** (20 vs 0). English source uses `—`, so every
  dash needs converting; `<!-- EN: -->` comments keep the English form verbatim.
- Ellipsis is ASCII `...` (74 in Core Keyed, `…` zero).
- Descriptions end with `.`; labels and buttons take none. Player-facing prose is
  informal **du** with imperatives, never Sie.
- `JobDef.reportString` and `RecipeDef.jobString` are third-person **with** a
  terminal period (`wendet TargetB an.`, `Stellt Hightech-Bauteil her.`) —
  unlike ja/ko, which take none. `RecipeDef.label` is `X herstellen`, no article.
- Research labels are lowercase noun phrases (Hightech-Fabrikation, lange
  Klingen, mehrläufige Waffen) or verb-final phrases (Bier brauen).

**The trait row collapses in German.** This repo's RU glossary hangs on
свойство-not-черта, because RU uses a different word for pawn traits. German has
no such split: Odyssey's `Stat_ThingUniqueWeaponTrait_Label`, Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` **and** Core's pawn-trait `<Traits>` are
all **Merkmale**. The disambiguating form, when no weapon context is present, is
vanilla's own `StatsReport_WeaponTraits` = **Waffenmerkmale**. Run the lookup
anyway — just expect it to come back the same.

**Case is the German landmine, not gender** (decompile-verified:
`Verse.GrammarResolverSimple`, `LanguageWorker_German`, `LanguageWordInfo`).
`"key".Translate(args)` reaches `GrammarResolverSimple`, not the rulepack
resolver. Its `obj is string` branch *does* support `{0_gender ? m : f : n}`,
`{0_definite}`, `{0_indefinite}`, `{0_plural}` on a plain string, resolving
gender from the word itself via `WordInfo/Gender/{Male,Female,Neuter,Other}.txt`
(~2450 nouns in Core). But it implements **no `lookup` function**, so
`{lookup: {0}; decline; N}` — the only route to the 2457-row `decline.txt` case
forms — silently fails, and de's article helpers are nominative-only. So gender
is solvable, case is not: restructure any oblique slot rather than guessing an
article. Two live cases in this repo, both plain-string injections:
`UMW_ExcludeWoodStuffDesc` (`MeleeWeapon_LongSword.label` = Langschwert, neuter)
and `UMW_WeaponEnabledDesc` (`weapon.label` — a **mod-coined** label, absent from
the Gender tables, so `ResolveGender` falls back to its `defaultGender` of
**Male** and `{0_gender ? …}` becomes a silent coin-flip). Reserve the gender
symbols for vanilla nouns in nominative slots.

**`RulePackDef` naming grammar works completely differently in German, and this
is the finding that matters most here.** Odyssey's de
`RulePacks_Namers_UniqueWeapons.xml` tags every noun with its gender inline and
strips the marker with `{replace:}` to emit the right article and adjective
ending:

```
<li>badass_noun_gender->|M|Mäher</li>
<li>badass_noun_gender->|F|Witwe</li>
<li>badass_noun_gender->|N|Ende</li>
<li>weapon_adjective_weapon_noun->{replace: [weapon_noun]; "|M|"-"[weapon_adjective]er "; "|F|"-"[weapon_adjective]e "; "|N|"-"[weapon_adjective]es "}</li>
<li>the_badass_concept->{replace: [badass_concept]; "|M|"-"Der "; "|F|"-"Die "; "|N|"-"Das "}</li>
<li>of_the_badass_concept->{replace: [badass_concept_gender_gen]; "|M|"-"des "; "|F|"-"der "; "|N|"-"des "}</li>
<li>weapon_noun_ungendered->{replace: [weapon_noun]; "|M|"-""; "|F|"-""; "|N|"-""}</li>
```

Adopt that shape for any de naming grammar here: a `*_gender` symbol carrying
`|M|`/`|F|`/`|N|`-prefixed nouns, plus `{replace:}` wrappers per syntactic slot
(bare, definite, genitive, adjective-agreeing), and an `_ungendered` variant that
strips the marker where no agreement is needed. A bare noun list with no markers
cannot be inflected at all. **Caveat:** in that same vanilla file the
*scaffolding* is German but several leaf lists (`badass_adjective`,
`badass_concept`) are still English (grim, eternal, justice, revenge) — vanilla
incompleteness, exactly as noted for zh. Copy the technique, never the vocabulary.

**Two def fields this mod owns feed straight into that machinery, so German
constrains their *form*, not just their wording:**

- **`namerLabels` must each carry a `|M|`/`|F|`/`|N|` prefix**, marker then noun,
  no space (`|N|Langschwert`). Odyssey's own de namerLabels do: `|M|Großbogen`,
  `|N|Sturmgewehr`, `|F|Büchse`, `|F|schwere MP`. Odyssey's `{replace:}` slots are
  what emit the article and adjective ending, so an unmarked label leaves the strip
  with nothing to match and generates a broken name. Nothing in the checker sees
  this — it validates the key path, not the value's shape.
- **`traitAdjectives` must be uninflected adjective stems** that read correctly with
  `-er`/`-e`/`-es` appended (strong) and `-e`/`-en` after a definite article (weak),
  because `weapon_adjective_weapon_noun` concatenates the ending. Odyssey de ships
  `leicht`, `schnellziehbar`, `unhandlich`, `sperrig`, `klobig`, `schön`, `elegant`,
  `verziert`, `golden`, `vergoldet`, `jadeverziert`, `grässlich`, `primitiv`,
  `hässlich`, `zielsuchend`, `treffsicher`, `präzis`, `lahmlegend`, `EMP-verstärkt`.
  A **noun** is never valid here (`Panzerdorn` + `es`), nor is a stem ending in
  `-e`/`-er`, nor one containing a space. This is the inverse of ja/ko/zh, where the
  same field wants an attributive *phrase* — do not port those rules to German.

Two more German mechanics from `LanguageWorker_German`, neither visible to the
checker:

- `PostProcessed` rewrites a trailing English `'s` to `s` (or a bare `'` after
  s/ß/z/x/ce). A closing ASCII single quote immediately followed by lowercase `s`
  is silently mangled — never write `'{0}'s`.
- `PostProcessThingLabelForRelic` truncates a weapon label to its bare weapon
  noun via `EndsWith` against a hardcoded 26-noun list: Horn, Lanze, Pulser,
  Werfer, Axt, Flinte, Bogen, Revolver, Gewehr, Stoßzahn, Stab, Hammer, Schwert,
  Pistole, Dolch, Büchse, Kanone, Granaten, Granate, Keule, Säbel, Messer,
  Rapier, Klinge, Sense, Speer; on no match it keeps only the substring after the
  last space or hyphen. **Directly relevant to this repo's `ThingDef` weapon
  labels** — a de label ending outside those 26 nouns yields a poor relic name.
  Note Waffe is *not* on the list; Schwert, Hammer, Klinge, Messer, Speer,
  Keule, Axt and Stab are.

**Stuff naming inverts in German, which directly affects this repo's
`stuff_adjective` symbol.** Core de's `ThingMadeOfStuffLabel` is `{1} aus {0}`
where English is `{0} {1}` — "wooden longsword" is "Langschwert aus Holz", not
"hölzernes Langschwert". Correspondingly de Core defines
`stuffProps.stuffAdjective` for only 9 defs, because the prepositional frame
replaces the adjective — **and every value is a bare noun built for that dative
frame**: `Holz`, `Gold`, `Granit`, `Marmor`, `Kalkstein`, `Schiefer`,
`Sandstein`, `Vakuumstein`, and decisively `Leather_Heavy` = **`dickem Fell`**,
already dative-inflected. Our `stuff_adjective` symbol falls back to the stuff's
`label` when `stuffAdjective` is absent, which in German is likewise always a
noun (`Stahl`, `Plastahl`, `Uran`, `Jade`, `Silber`).

**So this is settled by vanilla data, not a native call: use the `aus
[stuff_adjective]` frame.** It composes correctly for every material including
the pre-inflected one ("aus dickem Fell"), and it needs no gender agreement on the
material at all. Concretely, in `UMW_NamerStuffAdjectives`, **drop English's
`weapon_adjective->[stuff_adjective]` rule** — Odyssey's namer would append
`-er/-e/-es` to it and yield "Stahler" — and build `r_weapon_name` patterns on the
`aus` frame instead, reusing Odyssey's own `[weapon_noun_ungendered]` to strip the
gender marker (and noting its `badass_noun` list is unmarked, so it is safe bare).
Dropping a rule and adding others is fine: the checker enforces no `<li>`-count
parity on list-valued entries.

**Inside a rulepack the FULL resolver runs, so `lookup` *is* available** — the
opposite of §"Case is the German landmine", which applies only to `.Translate()`.
Vanilla de rulepacks use `{lookup: [INITIATOR_label]; decline; 2}` freely.
`decline.txt`'s column order is
`NOM;1_GEN;2_DAT;3_ACC;4_NOM_DEF;5_GEN_DEF;6_DAT_DEF;7_ACC_DEF`. It is a table of
**nouns**, so it resolves a pawn-kind label but not a proper name — prefer
restructuring over relying on it.

**Never *print* a `[X_definite]'s` genitive in German.** English name-grammar and
battle-log source is full of it; German cannot form a genitive by suffixing a
nominative definite phrase ("der Pirat" + s → "der Pirats"). Vanilla de contains 63
occurrences — **all in `<!-- EN: -->` comments** — and only 4 in actual German
values, every one of them inside a `{replace: …; " [INITIATOR_label]'s [WEAPON_label]"-""}`
that *deletes* the English construction before appending a `{lookup: …; decline; …}`
form. So keep the attacker a **nominative subject** and restructure the clause
(`[INITIATOR_definite] holte aus, doch [RECIPIENT_definite] parierte den …`). Note
this is the exact mirror of the ko lesson, where `[RECIPIENT_possessive]` was
comment-only: check comments-vs-values before copying a symbol usage from the tar.

**German keeps `[RECIPIENT_possessive]` and inflects it inline** by appending the
ending — `von [RECIPIENT_possessive]er Panzerung`, `gegen [RECIPIENT_possessive]e
Panzerung`, `mit [RECIPIENT_possessive]em Handschutz` (55 uses in Core combat packs).
Unlike ko, do **not** drop it.

**Battle-log `rulesStrings` are Präteritum** (`wich … aus`, `verfehlte`, `prallte …
ab`, `sprang zur Seite`) — not the nominalized ko form and not polite ja form. De's
`[skillAdv]` values are adverbs/adjective stems (`ungeschickt`, `geschickt`,
`meisterhaft`, `kunstvoll`), so an optional `[skillAdvMaybe]` composes cleanly as
`[skillAdv] geführten` before a masculine accusative noun.

**Quest descriptions must strip `[discoveryMethod]`'s case markers.** In German that
symbol resolves to a sentence frame containing `|thing_nom|` / `|thing_gen|` /
`|thing_dat|` / `|thing_acc|` plus four `_embedded` variants (see Core
`Keyed/Letters.xml` → `LetterNewQuest`). Every consumer `{replace:}`s all eight and
supplies its own noun phrase declined four ways — Odyssey's
`Script_AncientMercenaries.xml` is the worked example and the direct template for
this repo's warband quest, which reuses the same symbol. Miss this and a raw
`|thing_dat|` ships to screen. Odyssey's de file also supplies the reusable
renderings `eine einzigartige Waffe von [WEAPON_quality]er Qualität` and
`{LEADER_gender ? den Anführer : die Anführerin} einzufangen oder zu töten`.

**`questSubjectRules` needs extra case families in German:** alongside
`questMapFeature`, Odyssey de adds `questMapFeatureGenIndef` and
`questMapFeatureDatIndef` (`einer Militärgarnison`), because `Description_Map`
consumes the oblique forms. Supply all three families.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | Merkmal / Merkmale (standalone: Waffenmerkmale) | Eigenschaft, Attribut | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` |
| unique weapon | einzigartige Waffe | Unikat, besondere Waffe | Odyssey `UniqueWeapon` |
| **unique \<weapon\>** (ThingDef label) | **einzigartige/-r/-s \<Waffe\>** — lowercase adj, gender-agreeing | | Odyssey `einzigartiger Großbogen`, `einzigartiges Sturmgewehr`, `einzigartige Vollautomatikflinte` |
| longsword / spear / mace / knife / gladius | Langschwert / Speer / Streitkolben / Messer / Gladius | Schwert alone, Keule for mace | Core labels |
| axe / warhammer | Axt / Kriegshammer | Kriegsaxt, Streithammer | Core `MeleeWeapon_Axe`, `MeleeWeapon_Warhammer` |
| tool: handle / point / edge / head | Griff (bladed) or Stiel (hafted) / Spitze / Klinge / Kopf | | Royalty `MeleeWeapon_*.tools.*.label`; axe's own edge tool is `Schneide` |
| stagger | Taumeln; stat Taumelzeit-Faktor | Stolpern | Core `StaggerDurationFactor` |
| move speed / cells | Laufgeschwindigkeit / Zellen | Bewegungsgeschwindigkeit, Felder | Core `MoveSpeed` |
| melee armor penetration | Nahkampfrüstungsdurchdringung | | Core `MeleeWeapon_AverageArmorPenetration` — match whichever anchor the screen shows |
| toxic buildup | **Vergiftung** | Toxinaufbau | Core `ToxicBuildup` |
| toxic \<damage\> label | `Gift-` prefix: Giftkratzer, Giftbiss → Giftstich | | Core `ScratchToxic`, `ToxicBite` |
| woozy / sedated | benommen / bewusstlos | | Core `Anesthetic.stages.*` |
| injury `labelNoun` | **carries the indefinite article**: `ein Schnitt`, `eine Verbrennung` | bare noun | Core `Cut`/`Burn.labelNoun` — a shape ja/ko/zh don't have |
| bandaged / sutured / set | bandagiert / vernäht / geschient | | Core `HediffComp_TendDuration` |
| Cut off / Cut out | Abgeschnitten / Herausgeschnitten (capitalized) | | Core `Cut.injuryProps` |
| \<x\> scar | …narbe (Schnittnarbe, Brandnarbe) | | Core `HediffComp_GetsPermanent` |
| Dodge (TextMote) | **Ausgewichen** (past participle — match this register for a parry mote) | | Core `TextMote_Dodge` |
| stun | **betäuben** for flesh; **lahmlegen** ONLY for electronics/mechanoids | | Core `StunnedByEMP`, `ParalyticArrows` (`Betäubt Ziele`) vs Odyssey `EMPPulser` (`lahmlegt`); Odyssey ships both as adjectives but always with an electronic subject |
| quest | **Quest** | **Auftrag** (that is de's word for bills/recipes) | Core `Quest`, MainButton `Quests.label` |
| cooldown | Abklingzeit; "on cooldown" → `klingt gerade ab` | | Odyssey `abilityProps.cooldownGerund` |
| tribesman/tribespeople / chief | Ureinwohner (same sing+plural) / Häuptling | Stammesangehöriger | Core `TribeRough` |
| abandoned settlement / ancient sealed crate | verlassene Siedlung / versiegelte Kiste | | Core+Odyssey SitePartDefs |
| warlord | **Kriegsherr** (vanilla-attested, not a coinage) | | Core `BackstoryDef Warlordess56.title` |
| mod (the noun) | **feminine** — `die Mod`, `dieser Mod` | der/das Mod | Core Keyed `Die Mod muss nach {1} geladen werden.` |
| monosword / plasmasword / zeushammer | Monoschwert / Plasmaschwert / Zeushammer | | Royalty labels (persona forms prefix Persona-) |
| wood / plasteel / uranium / jade / steel / silver / gold | Holz / Plastahl / Uran / Jade / Stahl / Silber / Gold | Plasteel, Plastik | Core labels — Plastahl is translated |
| quality / tiers | Qualität / übel·schlecht·normal·gut·exzellent·meisterlich·legendär | | Core `Quality`, `QualityCategory_*` |
| "{0} quality or better" | `Qualität {0} oder besser` | | reshaped from Core `NormalQualityOrBetter` (pre-inflected, untemplatable) |
| tech levels | neolithisch / mittelalterlich / industriell / Raumfahrt / Ultra / Archotech | Weltraum, Ultratech | Core `TechLevel_*`; "tech level" = Techstufe |
| cut / stab / blunt / burn (DamageDef) | Schnitt / Stich / Wucht / Verbrennung | Schnittwunde, Stichwunde (hediff labels) | Core DamageDefs |
| blood loss / bleed rate | Blutverlust / Blutung | Blutung for the hediff | Core `BloodLoss.label`, `BleedingRate` |
| EMP stun | Betäubt durch EMP | | Core `StunnedByEMP` |
| armor penetration / damage / accuracy | Rüstungsdurchdringung / Schaden / Genauigkeit | Panzerung, Treffsicherheit | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| stopping power / burst count / burst speed | Mannstoppwirkung / Schüsse pro Feuerstoß / Feuerrate | Durchschlagskraft | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| ability / mood / colour / faction | Fähigkeit / Stimmung / Farbe / Fraktion | | Core `Abilities`, `Mood`, `Color`, `Faction` |
| bandit camp / item stash / ancient mercenaries | Banditenlager / Versteck mit Waren / antike Söldner | Räuberlager, Warenlager | Core `BanditCamp.label`, `ItemStash.label`, PawnGroup label |
| mechanite | Mechaniten | Mechanite | Royalty monosword desc |
| wielder | Träger | Anwender, Nutzer | Royalty weapon-trait descs |
| relic | Reliquie | Relikt | Ideology `Relic` (reliquary = Reliquienschrein) |
| ideoligion / reform | Ideologie / Ideologie reformieren | Ideoligion | Ideology `IdeoligionOf`, `ReformIdeoligion` — de uses the plain word, no portmanteau |
| Crafting (the skill) | Handwerk | Herstellung, Basteln | Core `Crafting.label` |
| bill / recipe (both) | Auftrag | Rezept, Rechnung | Core `TabBills`, `AddBill`, every `Stat_Recipe_*_Desc` — de collapses the two |
| Cancel / Reset / Confirm / Randomize | Abbrechen / Zurücksetzen / Bestätigen / Zufällig | | Core buttons |
| Reset to defaults / default | Auf Standard zurücksetzen / Standard | | Core `ResetBinding`, `Default` |
| None | Nichts | Keine | Core `None` |

The six Odyssey trait ports have official de labels and adjectives, and four
have descriptions matching our English word for word — copy those verbatim
(`verziert`, `hässlich`, `vergoldet`, `jadeverziert`). `Lightweight` (`leicht`)
and `Cumbersome` (`unhandlich`) differ only in aim-vs-swing, so adapt that clause
alone. As in ko, Odyssey's `Ugly` adjective *indices* differ from ours: re-map by
meaning (crude=`primitiv`, ugly=`hässlich`, monstrous=`grässlich`). `Lightweight`'s
"nimble" has a Core anchor — the Gladius description's `leicht und wendig`.

Mod-decided terms pending native review (from the 2026-07-28 commit), all
uninflected stems where they are trait adjectives: `Panzerdorn` (armor spike),
`Widerhaken` (barbed), `glockengegossen` (bell-cast), `blutbefleckt`
(blood-stained), `karbonisiert` (carbonized), `Gegengewicht` (counterweighted),
`rückschlagfrei` (dead-blow), `emailliert` (enameled), `vergiftet` (envenomed),
`gerippt` (flanged, flanges = `Schlagrippen`), `kopfschwer` (head-weighted),
`monomolekular`, `Nadelspitze` (needle point), `opiatbeschichtet` (opiated),
`Rammkopf` (piledriver), `plasmaumhüllt` (plasma-cored), `Parierstangen`
(quilloned; quillon/crossguard = `Parierstange`, third synonym `Handschutz`),
`rasierscharf` (razored), `gezahnt` (serrated), `geschichtsträchtig` (storied),
`genietet` (studded, studs = `Nieten`), `Zeuskopf` (zeus-headed, capacitor =
`Zeus-Kondensator`); `parieren`/`Pariert` (parry, register-matched to
`Ausgewichen`), `Erdstoß` (earthshake), `Schlachtruf` (rallying cry), `angespornt`
(rallied), `Sedierung` (sedative buildup), `ausgefranst` (ragged),
`meistergeschmiedet` (master-forged), `Kriegerbande` (warband),
`Kriegerbandenlager` (warband camp), `Kriegszug` (war party), `Stammeskrieger` /
`Stammesraider` (tribal warrior/raider, on Core's dominant `Raider`),
`Spalter` (cleaver), and the colours `blutrot` / `karbonschwarz` / `emailviolett`
/ `monomolekularweiß` / `plasmaorange` (patterned on Odyssey's
`eisblau`/`feuerorange`). The 2026-07-30 WeaponCategoryDef labels are likewise
mod-decided: `Nahkampf` (melee, Core skill label), `Schnitt` / `Stich` / `Wucht`
(bladed / pointed / blunt, the Core DamageDef labels), `schwer` (heavy),
`bewehrt` (guarded, matching the `-bewehrt` pattern of the Quilloned family).

#### Spanish (Castellano) (from this repo's machine-assisted generation, 2026-07-29)

RimWorld ships **two** Spanish languages: `Spanish (Español(Castellano)).tar` and
`SpanishLatin (Español(Latinoamérica)).tar`. The roster's "Spanish" means the
Castilian one, so the mod folder is `Spanish` (the parenthetical is stripped by
`legacyFolderName`, same mechanism as `Japanese`/`Korean`). A LatAm pass would be a
separate `SpanishLatin` folder, not an edit to this one.

`Verse.LanguageWorker_Spanish` is decompiled and **imposes no hidden authoring
requirements** — no `PostProcessed` override (unlike German), no particle system
(unlike Korean). It prepends `el/la/los/las` and `un/una/unos/unas` from the word's
gender, returns names unchanged, has full `Pluralize` rules plus a `plural.txt`
lookup, and renders ordinals `N.º`. Notably it does **not** contract `de el`/`a el`
— see below, that is the author's job.

Style rules from the vanilla es data (mandatory):

- **ASCII straight double quotes** for cited def labels: vanilla writes
  `La misión se llama "{0}".` — 7689 ASCII `"` against **7** curly `“` and **zero**
  guillemets `«»`. Do not port ja's 「」, ru's «», or zh's “”.
- **Inverted opening marks are required**: `¿…?`, `¡…!` (168 / 433 in Core).
- **Zero dashes.** Core+DLC contain **no** em dashes and **no** en dashes, so an
  English `—` must be **reflowed**, not converted. This is the opposite of German,
  which mandates `–`.
- Ellipsis is ASCII `...`. Descriptions end `.`; labels, buttons and stat fragments
  take none, and labels are lowercase noun phrases.
- **Informal tú with imperatives**, decisively: Explora 12 / Explore 0, Asegúrate
  41 / Asegúrese 0, `tu colonia` 61 / `su colonia` 3.
- **Adjectives postpose and agree in gender + number.** So `unique <weapon>` is
  `<arma> único/única` — Odyssey ships `arco grande único`, `escopeta automática
  única`, `minigun única`.
- Two different gender hedges, and the right one depends on the field: a
  `deathMessage` takes the inline resolver form (`{0} ha muerto quemad{0_gender ?
  o : a}.`), while a bare-participle `injuryProps.destroyedLabel` takes a
  capitalized `(a)` (`Lacerado(a)`, `Seccionado(a)`, `Quemado(a)`).
- `labelNoun` **carries the indefinite article** (`un corte`, `una puñalada`, `una
  quemadura`) — the same shape German has and ja/ko/zh don't.

**Spanish solves name-grammar gender by SPLITTING SYMBOLS, not by tagging nouns.**
Odyssey's es `NamerUniqueWeapon` keeps parallel families — `badass_concept` (M) vs
`badass_conceptF` (F), `concept` vs `conceptF` — and writes one rule per gender
(`[weapon_type] del [badass_concept]` / `… de la [badass_conceptF]`). There are no
inline `|M|`-style markers and no `{replace:}` gender stripping. Consequences:

- **`namerLabels` are bare lowercase nouns with NO markers** — the exact inverse of
  German. Odyssey's es namer never puts an article or an agreeing adjective beside
  `[weapon_type]`, precisely because its gender is unknowable there, so nothing
  needs marking.
- **`traitAdjectives` must be GENDER-INVARIANT**, because they postpose straight onto
  a weapon noun of either gender (`[weapon_type] [trait_adjective]` → `espada larga`
  F or `martillo de guerra` M). Two legal shapes, both used throughout Odyssey's own
  es trait file: an invariant adjective (`-e`, `-al`, `-ar`, `-z`, `-ista`, `-ble`,
  `-il` — `torpe`, `elegante`, `ornamental`, `ágil`, `veloz`, `brillante`,
  `horripilante`), or a **prepositional phrase** (`de oro`, `de jade`, `de adorno`,
  `de gran tamaño`, `de manejo torpe`, `con buscador`). A bare `-o`/`-a` adjective is
  silently broken on half the weapons. Note the trait's own `label` is a different
  field and *may* inflect — Odyssey uses default masculine (`ligero`, `feo`).
  Also keep such an adjective **material-neutral**: a universal trait rolls on wood,
  jade and plasteel too, so `de acero carbonizado` is wrong where
  `de superficie carbonizada` is right.
- **Weapon names carry no definite article at all** in Odyssey's es patterns. Drop
  English's "The" rather than trying to supply `el`/`la`.
- **es redefines `weapon_adjective` as a prepositional phrase**, not an adjective
  (`weapon_adjective->del [concept]` / `de la [conceptF]`). Its `badass_adjective`
  list survives but is referenced by no rule — dead weight in es.

**`de el` → `del` and `a el` → `al` must be contracted by hand.** `[X_definite]`
emits `el …` already wrapped in a colour tag, so Core es fixes this 89 times with the
colour code baked into the search pattern:

```
{replace: de [RECIPIENT_definite]; "de &lt;color=#D09B61FF>el "-"&lt;color=#D09B61FF>del "}
{replace: a [RECIPIENT_definite]; "a &lt;color=#D09B61FF>el "-"&lt;color=#D09B61FF>al "}
```

Feminine (`de la pirata`) and named pawns (`de Roberto`, no article) simply don't
match and pass through untouched, which is correct. **Core es also ships a shorter
variant, `{replace: de [X]; ">el "-">del "}` (20 uses in `RulePacks_CombatRanged`),
and that one is buggy** — it leaves the literal `de ` outside the match and renders
"de del proyectil". Copy the full form only. The alternative is to restructure so no
`de`/`a` precedes a `_definite` symbol.

**`[RECIPIENT_possessive]` resolves to `su` and has NO plural form** — Core
`Keyed/Grammar.xml` sets `Prohis`/`Proher`/`Proits` all to `su`. Since Spanish `su`
agrees in number with the *possessed* noun, the symbol is only safe before a
**singular** noun; `[RECIPIENT_possessive] gavilanes` would ship "su gavilanes" every
roll. Use the definite article for plurals (`los gavilanes`), which is also the more
idiomatic Spanish for one's own equipment. This is a third distinct answer to the
possessive question: ko drops the symbol, de keeps and inflects it, es keeps it only
in the singular.

**Battle-log `rulesStrings` are preterite** (`evitó`, `falló`, `vaciló`, `se tropezó`,
`se tambaleó`, `se resbaló`, `saltó`) — not the perfect. es `[skillAdv]` values are
adverbs (`incompetentemente`, `ineptamente`), and Core places `[skillAdvMaybe]`
*before* the verb.

**The stuff frame inverts, and here the `weapon_adjective` rule SURVIVES** (unlike
German, where it had to be dropped). Core es `ThingMadeOfStuffLabel` is `{1} de {0}`,
and es `stuffProps.stuffAdjective` values are bare nouns (`acero`, `plastiacero`,
`madera`, `jade`, `oro`, plus pre-framed `piel gruesa` / `cuero ligero`). Because es
`weapon_adjective` is *already* prepositional, `weapon_adjective->de
[stuff_adjective]` composes correctly with every Odyssey pattern (`espada larga de
acero`, `filo de plastiacero`). Build the `r_weapon_name` patterns on the same `de`
frame, article-free.

**Quest grammar is markedly simpler than German's.** `[discoveryMethod]` carries no
case markers in es — Odyssey uses it bare (`[discoveryMethod] la ubicación de una
infame compañía de mercenarios.`) — so there is nothing to `{replace:}` away, and
`questSubjectRules` needs only the plain `subject` / `questMapFeature` /
`questMapText` families, with no genitive/dative variants. Two Odyssey es renderings
are worth reusing verbatim: `un arma única: [WEAPON_quality]` (a colon sidesteps
quality-adjective agreement) and `Si logras capturar o matar al líder, puedes tomar
su arma.`

**The trait row collapses in Spanish, exactly as it does in German.** Odyssey's
`Stat_ThingUniqueWeaponTrait_Label`, `WeaponTraits` **and** Core's pawn-trait
`Traits` are all **Rasgos**; the disambiguated form is vanilla's own
`StatsReport_WeaponTraits` = **Rasgos del arma**. Royalty's *persona* word is
`Características` (PWU's domain, not ours). Run the lookup anyway; expect a collision.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | `rasgo` / `rasgos`; standalone `Rasgos del arma` | `propiedad`, `característica` | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits`; `Características` is Royalty's persona word |
| unique weapon | `arma única` | | Odyssey `UniqueWeapon` |
| longsword / spear / mace / knife / gladius | `espada larga` (F) / `lanza` (F) / `maza` (F) / `cuchillo` (M) / `gladius` (M) | | Core labels; genders matter for the `único/única` suffix |
| axe / warhammer / club | `hacha` (F, takes *el/un hacha*) / `martillo de guerra` (M) / `porra` (F) | | Core |
| monosword / plasmasword / zeushammer | `mono-espada` / `espada de plasma` / `martillo de Zeus` | | Royalty labels |
| **monomolecular (adjective)** | **`mono-molecular`** — hyphenated | `monomolecular` | Royalty renders the *adjective* hyphenated 4/4, though its *noun* varies (`mono-espada` 2 / `monoespada` 3) |
| tool: handle / point / edge / blade / head / shaft | `mango` (bladed, blunt) or `empuñadura` (axe, warhammer, ultratech) / `punta` / `filo` / `hoja` / `cabeza` / `ástil` | | Core+Royalty `tools.*.label` |
| **cut / stab (DamageDef)** | **`corte` / `apuñalamiento`** | `puñalada` (that is the *hediff* label) | Core splits them: DamageDef `Stab`=`apuñalamiento`, HediffDef `Stab`=`puñalada`; both `Cut`=`corte`. Same trap as ko/de |
| blunt / burn / flame (DamageDef) | `contusión` / `quemadura` / `llama` | | Core |
| toxic \<damage\> label | postposed agreeing adjective: `arañazo tóxico`, so a toxic stab is `apuñalamiento tóxico` | a prefix | Core `ScratchToxic` |
| bandaged / tended / sutured | `vendada` / `atendida` / `suturada` — **agree with their own wound noun's gender** | | Core `HediffComp_TendDuration`; `corte` (M) and `puñalada` (F) therefore differ |
| Cut off / Cut out | `Lacerado(a)` / `Seccionado(a)`; a stab uses `Perforado(a)` | | Core `Cut`/`Stab.injuryProps` — Core itself differentiates by wound |
| \<x\> scar | `cicatriz de <noun>` (Core converts adjectival forms: "shredded scar" → `cicatriz de desgarramiento`) | `cicatriz <adj>` | Core `HediffComp_GetsPermanent` |
| woozy / sedated | `atontado` / `sedado` | | Core `Anesthetic.stages.*` — **don't spend `sedado` on another stage**; "dosed" needed a fresh word (`medicado`) |
| blood loss / bleeding | `pérdida de sangre` / `Hemorragia` | `sangrado` | Core `BloodLoss.label`, `BleedingRate` |
| toxic buildup / anesthetic | `acumulación tóxica` / `anestesia` | | Core |
| **Dodge (TextMote)** | **`Esquivado`** (past participle — match this register for a parry mote) | | Core `TextMote_Dodge` |
| stun / EMP / stagger | `aturdir`/`aturdido` / **`PEM`** / `tambaleo` | `EMP` | Core `Stun`, `EMP.label`, `StunnedByEMP`=`Aturdido por PEM`, `StaggerDurationFactor` |
| melee armor penetration / melee damage multiplier | `penetración de armadura CaC` / `multi. de daño cuerpo a cuerpo` | | Core StatDefs |
| move speed / max hit points / deterioration / flammability / market value | `velocidad de movimiento` / `puntos de impacto máximos` / `índice de deterioro` / `inflamabilidad` / `valor de mercado` | | Core StatDefs |
| **quest** | **`misión`** | `búsqueda` | Core `Quest`, MainButton `Quests.label`=`misiones` |
| cooldown / ability / radius / cells | `enfriamiento` / `habilidad` / `radio` / `casillas` | | Core |
| quality tiers | `horrible·mediocre·normal·bueno·excelente·obra maestra·legendaria` | | Core `QualityCategory_*` |
| wood / plasteel / uranium / jade / steel / silver / gold | `madera` / **`plastiacero`** / `uranio` / `jade` / `acero` / `plata` / `oro` | `plasacero` | Core labels + `stuffAdjective` |
| **purple (weapon colour)** | **`púrpura`** | `morado` | Core's generic ColorDefs say `morado`, but **Odyssey's own `UniqueWeapon_*` colour defs — the exact analog — say `púrpura`** (`púrpura apagado`). Match the nearer file |
| mechanite / mechanoid | `mecanita`/`mecanitas` (F) / `mecanoide` | `nanomáquina` | Royalty monosword desc |
| wielder / bearer | `usuario` / `portador` | | Odyssey `EMPPulser` (`pulso PEM`, `centrado en el usuario`), Royalty trait descs |
| item stash / bandit camp / ancient mercenaries / sealed crate | `Alijo de objetos` / `campamento de bandidos` / `mercenarios antiguos` / `caja sellada` | | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement | `asentamiento abandonado` (Odyssey) or `colonia abandonada` (Core) | | both attested; prefer Odyssey's for a site part |
| tribesman / tribespeople / chief / fierce tribe | `tribal` / `tribales` / `jefe` / `tribu agresiva` | | Core `TribeRough` |
| **warlord** | **`señor de la guerra`**; short `caudillo` | | Core `Warlordess56.title`/`.titleShort` — vanilla-attested, not a coinage |
| relic / ultratech (tech level) | `reliquia` / `ultra` | | Ideology `Relic`, Core `TechLevel_Ultra`. The *adjective* `ultratecnológico` is Core-attested (6+) and safe in prose |
| Cancel / Reset / Confirm / Default / None | `Cancelar` / `Restablecer` / `Confirmar` / `Por defecto` / `Ninguno` | | Core buttons |
| Traders will pay more/less for it. | `Los comerciantes pagarán más por ella.` / `… menos por ella.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim |

The six Odyssey ports have official es labels, adjectives and — for four of them —
descriptions matching our English word for word (`ornamental`, `feo`, `incrustación
de oro`, `incrustación de jade`); copy those verbatim. `Lightweight` (`ligero`) and
`Cumbersome` (`torpe`) need only their aim-vs-swing clause adapted. As in ko and de,
Odyssey's `Ugly` adjective *indices* differ from ours: re-map by meaning
(crude=`de aspecto horrible`, ugly=`horripilante`, monstrous=`horrible`).

Mod-decided terms pending native review (from the 2026-07-29 commit), every trait
adjective among them gender-invariant by construction: `pico perforante` (armor
spike), `con lengüetas` (barbed), `fundido en campana` (bell-cast), `manchado de
sangre` (blood-stained), `carbonizado`, `contrapesado`, `sin rebote` (dead-blow),
`esmaltado`, `envenenado`, `de aletas` (flanged; flanges = `aletas`), `de cabeza
pesada` (head-weighted), `punta de aguja`, `opiáceo`, `martinete` (piledriver),
`núcleo de plasma`, `con gavilanes` (quilloned; quillons = `gavilanes`, crossguard =
`cruz`, guard = `guarda` — **not** `guarnición`, which vanilla uses for "military
garrison"), `filo de navaja` (razored), `serrado`, `de renombre` (storied), `con
tachuelas` (studded), `cabeza de Zeus`; `Desviado` (the parry mote, register-matched
to `Esquivado`; `Parada` is the fencing term and the likeliest reviewer alternative)
with `desviar`/`paró`/`detuvo` in the log lines, `sacudida telúrica` (earthshake),
`arenga` (rallying cry) / `arengado` (rallied), `acumulación de sedante` with the
stage ladder `medicado`/`atontado`/`sedado`, `corte desgarrado` / `puñalada
desgarrada` (ragged), `forjado por un maestro` (master-forged), `banda de guerra`
(warband), `campamento de la banda de guerra`, `partida de guerra` (war party),
`guerrero tribal` / `saqueador tribal`, `machete` (cleaver, vanilla-attested),
`mazo` (maul), `lanzón` (lance), `pica` (pike), `garrote` (bludgeon), and the colours
`rojo sangre` / `negro carbón` / `púrpura esmalte` / `blanco mono-molecular` /
`naranja plasma` (patterned on Odyssey's `azul hielo` / `naranja fuego`). The
2026-07-30 WeaponCategoryDef labels are likewise mod-decided: `cuerpo a cuerpo`
(melee, Core skill label), `cortante` / `punzante` / `contundente` (bladed /
pointed / blunt), `pesado` (heavy), `con guarda` (guarded, matching the
`de guarda` construction already used for Quilloned).

#### French (from this repo's machine-assisted generation, 2026-07-29)

Language folder is `French` (tar: `French (Français).tar`).

**`LanguageWorker_French` rewrites every string, and this is the finding that shapes
everything else** (decompile-verified). Its `PostProcessed` runs five regexes in order:

```
ElisionE   \b(ce|de|je|le|me|ne|se|te|que|quoique|lorsque) + vowel   → c' d' j' l' m' n' s' t' qu' ...
ElisionLa  \bla + vowel                                             → l'
ElisionSi  \bsi il(s)                                               → s'il(s)
DeLe       \bde le(s)                                               → de / des
ALe        \bà le(s)                                                → au / aux
```

**So French is the inverse of Spanish: never hand-contract.** Where Core es needs
`{replace: de [X_definite]; "de &lt;color=…>el "-"…del "}`, French writes `de` / `le` /
`la` plainly and the worker fixes it — including inside rulepacks and `.Translate()`
output. Vanilla fr relies on this: `le [attack_noun]` renders "l'assaut", `dégâts de
{1}` renders "dégâts d'immolation", `en or`/`en argent` need nothing. Two traps in it:

- **`de le` becomes `de`, not `du`.** Group 2 captures only `e`/`es`, so `de les X`
  correctly yields "des X" but `de le X` yields "de X". Core fr ships this bug (`a
  [destroyed_past] [destroyed_targets] de [RECIPIENT_definite]` → "la jambe de pirate").
  Never write `de [X_definite]`; restructure so the entity is a subject, or use an
  agent phrase — **`par [X_definite]` never contracts** and is the clean escape.
- **`IsVowel` includes `h`**, so the worker cannot tell *h muet* from *h aspiré* and
  elides both: `la hache` → "l'hache", `de hampe` → "d'hampe". Never place an
  elidable word directly before an h-initial noun. (This mod's axe is `hache`, so it
  matters here.)

`WithDefiniteArticle`/`WithIndefiniteArticle` are **overridden**, handling `l'` before a
vowel and `le`/`la` by gender directly — so the Keyed `DefiniteForm`/`IndefiniteForm`
templates are dead code in French, and `[X_definite]` is reliable for pawns. `Pluralize`
knows `-al`→`-aux`, `-au`/`-eu`→`+x`, and leaves `s`/`x`/`z` alone. `OrdinalNumber` gives
`1er`, `2e`.

Style rules from the vanilla fr data (mandatory):

- **Formality is `vous`, decisively** — 564 `vous` against **zero** `tu`/`Tu` in
  Core+DLC Keyed (`Assurez-vous` 15 / `Assure-toi` 0, `votre colonie` 30). This is the
  opposite of German and Spanish, both of which are informal. Imperatives are the
  vous form (`Explorez`, `Faites attention`).
- **ASCII straight double quotes** for cited def labels: `La quête s'appelle "{0}".` —
  356 ASCII `"` against 14 guillemets `«»` (inconsistently spaced) and **zero** curly
  `“`. Do not port the ja/zh/ru quote marks.
- **ASCII apostrophe `'`**, not `’` (1991 vs 65) — and this is load-bearing, not
  cosmetic: the elision worker emits ASCII `'`, so a curly one would not match.
- **A space before `:` `;` `!` `?`**, per French typography — 727 ` :` against 30 tight,
  plus 239 ` ?` and 91 ` !`. It is a **plain ASCII space**, not a no-break or narrow
  space (only 13 of those exist in the whole corpus). Vanilla fr itself slips here
  (Odyssey's `Le groupe contient:`); write the space.
- **Zero dashes.** Core+DLC hold 1 em dash and 6 en dashes, i.e. none — an English `—`
  must be **reflowed**, as in Spanish and unlike German, which mandates `–`. Ellipsis
  is ASCII `...`.
- Descriptions end `.`; labels, buttons and stat fragments take none, and labels are
  lowercase noun phrases.
- **`ThoughtDef` stage descriptions are the register exception**: first-person present
  and informal (`Je suis à la limite de vaciller.`, `J'ai l'impression d'avoir…`), never
  vous-form.
- Gender hedging inside a string uses **inline word-splitting**, the French idiom:
  `{0} a été taillad{PAWN_gender ? é : ée : é(e)} à mort.`, `{PAWN_gender ? un : une :
  un(e)}`. Note both arities occur — the 3-arg form when a genderless subject is
  possible, the 2-arg `détendu{PAWN_gender ? : e}` where it is not. A bare-participle
  `injuryProps` label instead takes a capitalized `(e)`: `Déchiqueté(e)`, `Perforé(e)`.
- `labelNoun` **carries the indefinite article** (`une taillade`, `un coup de lame`,
  `une brûlure`) — the shape de and es share and ja/ko/zh lack.

**French solves rulepack gender with RULE-LEVEL CONSTRAINTS — a fourth technique,
distinct from German's inline `|M|` markers and Spanish's parallel symbol families.**
Core fr writes one rule per agreement class and lets the resolver pick:

```
<li>staggered(p=3,SUBJECT_gender==Male)->est stupéfait</li>
<li>staggered(SUBJECT_gender==Female)->est stupéfaite</li>
<li>staggered(SUBJECT_gender==None)->est stupéfait</li>
<li>verb_genericattack(INITIATOR_gender!=Female)->s'est rué</li>   <!-- shorthand for Male+None -->
```

**Always cover `None`** (or use `!=Female`): a missing branch fails to resolve for
genderless pawns, i.e. mechanoids. But reach for this only when you must, because:

**Battle-log `rulesStrings` are passé composé with *avoir*** (`a esquivé`, `a dévié`, `a
évité`, `a échoué`, `a titubé`) — not es's preterite and not de's Präteritum. A participle
with *avoir* does **not** agree with the subject, which is why most Core fr combat lines
need no gender handling at all. It *does* agree with a **preceding direct object**, so if
a line uses an object pronoun (`l'a reçu`), every noun that pronoun can stand for must be
the same gender — pick the symbol's values accordingly rather than adding gender rules.
fr `[skillAdv]` values are adverbials (`maladroitement`, `avec habileté`, `de manière
incompétente`) and Core places `[skillAdvMaybe]` **after** the verb.

**`[X_possessive]` is structurally wrong in French, and this is a fourth distinct answer
to the possessive question.** Core `Keyed/Grammar.xml` sets `Prohis`=`son`,
`Proher`=`sa`, `Proits`=`son/sa`, so the symbol resolves from the **possessor's** gender
— but French `son`/`sa` agrees with the **possessed noun**. The symbol therefore keys off
the wrong entity no matter what. Counting values rather than comments proves vanilla
agrees: **1471 occurrences in `<!-- EN: -->` comments, 24 in actual values, and all 24 are
broken** (Anomaly's `[RECIPIENT_possessive]de son travail` renders "sonde son travail";
Odyssey's `de [PAWN_possessive]` renders "le visage de son"). Core's combat packs write the
possessive literally instead — `[deflecting] son armure` — and so should you. So: ko drops
the symbol, de keeps and inflects it, es keeps it only before a singular noun, **fr
replaces it with a literal possessive agreeing with the possessed noun.**

**Odyssey's French `NamerUniqueWeapon` is not merely incomplete, it is broken, and none of
it may be copied.** It defines four parallel symbol families —
`badass_adjective_feminine`, `badass_noun_feminin` (note the inconsistent spelling),
`badass_noun_vowel`, `badass_adjective_indef` — that **no rule references**, so every one
is dead; its rules hardcode a masculine `Le [weapon_type]`; it inverts the English
possessives (`[ANYPAWN_nameIndef] du [weapon_noun]` for "X's reaper"); and the translator
left their own unresolved question in the file as `<!-- WeaponType feminine/masculine? -->`.
The practical consequence is that vanilla fr generates "Le lance" and (via h-aspiré
elision) "L'hache" for unique weapons whatever a mod does, because that rule lives in
Odyssey's def and mods can only *add* alternatives to it. Keep your own
`r_weapon_name` patterns **article-free** so they are at least correct, and don't try to
repair Odyssey's.

Two def fields this mod owns, both constrained by that namer:

- **`traitAdjectives` must be GENDER-INVARIANT** — the same requirement Spanish has, for
  the same reason (they postpose onto a `[weapon_noun]` of unknowable gender), and it
  bites harder here: this mod's roster is four feminine weapons (`hache`, `épée longue`,
  `masse`, `lance`) against three masculine (`glaive`, `couteau`, `marteau de guerre`), so
  a masculine-default adjective is wrong more often than right. **Odyssey's own fr file
  violates this throughout** (`léger`, `légère`, `lourde`, `gênante`, `perçante`, `laid`,
  `exacte`, plus plurals like `surdimensionnées`), which mostly survives on its almost
  entirely masculine gun roster — do not copy the adjectives even for the six ports whose
  labels and descriptions you do copy. Two legal shapes, both attested in that same file:
  a **prepositional phrase** (`à …`, `de …`, `en …`, `au …`, `sans …`, `d'…` — vanilla ships
  `sur mesure`, `à percussion`, `de choc`, `à sabot`, `haute capacité`), or an adjective
  already invariant in gender, i.e. one whose masculine form ends in `-e` (`agile`,
  `féroce`, `magnifique`, `malcommode`, `infâme`, `mono-moléculaire`). An invariant colour
  compound (`rouge sang`, `noir carbone`) also works.
- **`namerLabels` are bare lowercase nouns with NO marker** — as in Spanish, the inverse of
  German. Odyssey's fr namer never places an agreeing adjective or article beside
  `[weapon_type]`, precisely because its gender is unknowable there.

**The stuff frame is `en`, and it needs no elision work at all.** Core fr
`ThingMadeOfStuffLabel` is **`{1} en {0}`** ("épée longue en acier"), and fr
`stuffProps.stuffAdjective` values are bare nouns. So build the `stuff_adjective` rules on
`en [stuff_adjective]`: it composes with Odyssey's postposing `[weapon_noun]
[weapon_adjective]` pattern, and unlike a `de` frame it cannot trip the `de le` bug.
Keep English's `weapon_adjective->[stuff_adjective]` rule but make it prepositional (as in
es; de had to drop it). **Trap: `Steel.stuffProps.stuffAdjective` is `métal`, not
`acier`** — the label and the stuff adjective differ, so a steel weapon reads "épée longue
en métal". Verify per material rather than assuming the label.

**Quest grammar is the simple kind, like Spanish's.** `[discoveryMethod]` carries no case
markers and is used bare (`[discoveryMethod] l'emplacement d'une infâme compagnie de
mercenaires.`), so there is nothing to `{replace:}` away, and `questSubjectRules` needs
only the plain `subject` / `questMapFeature` / `questMapText` families — no oblique
variants. Two Odyssey fr renderings are worth reusing verbatim: `une arme unique
[WEAPON_quality]` and `Si vous parvenez à capturer ou à tuer le chef, vous pouvez prendre
l'arme unique.`

**`unique <weapon>` is the easy case here:** `unique` is invariant in gender and
postposes, so one form serves every weapon — Odyssey ships `arc long unique`, `fusil
d'assaut unique`, `minigun unique`. No es-style `único/única` or de-style ending needed.

**The trait row does NOT collapse in French.** Odyssey's `WeaponTraits` is `Traits d'arme`
and `Stat_ThingUniqueWeaponTrait_Label` is `Traits`, while Core's pawn-trait section header
is `Éléments marquants :` — a different word entirely, unlike de (`Merkmale`) and es
(`Rasgos`), where weapon and pawn traits collide. Royalty's *persona* label is also
`Traits`, but that is PWU's domain.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | `Traits`; standalone `Traits d'arme` | | Odyssey `WeaponTraits`, `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` (which says `Traits d'armes` — vanilla is inconsistent on the plural) |
| unique weapon | `arme unique` | | Odyssey `UniqueWeapon` |
| longsword / spear / mace / knife | `épée longue` (F) / `lance` (F) / `masse` (F) / `couteau` (M) | `épée large`, `massue` for mace | Core labels |
| **gladius** | **`glaive`** | `gladius` | Core `MeleeWeapon_Gladius` — French translates it rather than borrowing |
| axe / warhammer / club | `hache` (F) / `marteau de guerre` (M) / `massue` (F) | `hache de guerre` | Core+Royalty. `hache` is h-aspiré: never put `la`/`de` straight before it |
| tool: handle / point / edge / blade / head / shaft | `manche` (or `poignée` on the longsword) / `pointe` / `tranchant` / `lame` / `tête` / `hampe` | `fil` for edge | Core+Royalty `tools.*.label` |
| tool capacity: cut / stab / blunt | `coupant` / `perçant` / `contondant` | | Core `ToolCapacityDef` — adjectives, so they cannot simply precede a noun like "dégâts" |
| **cut / stab (DamageDef)** | **`taillade` / `blessure par lame`** | `perforation` (that is the *hediff* label) | Core splits them: HediffDef `Stab`=`perforation`, DamageDef `Stab`=`blessure par lame`; `Cut`=`taillade` in both. Same trap as ko/de/es |
| blunt / burn / flame (DamageDef) | `passage à tabac` / `brûlure` / `immolation` | | Core |
| toxic \<damage\> label | postposed agreeing adjective: `lacération empoisonnée`, `morsure venimeuse` | a prefix | Core `ScratchToxic`, `ToxicBite` |
| bandaged / sutured / set | `bandée` / `suturée` / `plâtrée` — **agree with their own wound noun** | | Core `HediffComp_TendDuration` |
| Cut off / Cut out | `Déchiqueté(e)` / `Sectionné(e)`; a stab uses `Perforé(e)` | | Core `Cut`/`Stab.injuryProps` — Core itself differentiates by wound |
| \<x\> scar | `cicatrice de <noun>` (`cicatrice de taillade`, `cicatrice de brûlure`) | | Core `HediffComp_GetsPermanent` |
| woozy / sedated | `vaseux` / `sous sédatif` | | Core `Anesthetic.stages.*` |
| blood loss / bleed rate | `perte de sang` / `saignement` | `hémorragie` (that is the ITab header) | Core `BloodLoss.label`, `Stat_Hediff_TotalBleedFactor_Name` |
| toxic buildup / anesthetic | `accumulation toxique` / `anesthésie` | | Core |
| **Dodge (TextMote)** | **`Esquive`** — a NOUN, so match a parry mote to it as a noun (`Parade`) | a participle | Core `TextMote_Dodge`; de and es both use participles here, French does not |
| stun / EMP | `étourdir`/`étourdi` / **`IEM`** | `EMP`, `IEM` spelled out | Core `EMP.label`=`IEM`, `StunnedByEMP`=`Étourdi par une IEM` |
| **stagger** | **`faire tituber`** (verb) | the StatDef label | Core glosses it in `StoppingPowerExplanation` ("feront tituber les cibles") and `failtype->a titubé`; `StaggerDurationFactor.label` is `facteur de progression du temps`, a vanilla mistranslation — do not propagate it |
| melee armor penetration / melee damage multiplier | `pénétration d'armure en mêlée` / `multiplicateur de dégâts en mêlée` | | Core StatDefs |
| move speed / max hit points / deterioration / flammability / market value | `vitesse de déplacement` / `point de santé maximale` (sic, vanilla singular) / `taux de dégradation` / `inflammabilité` / `valeur marchande` | | Core StatDefs |
| **cooldown** | **`Temps de recharge`** | `Délai de refroidissement` | `StatsReport_Cooldown`, `ITabs.Cooldown`, `PsychicRitualCooldownLabel`, `CommandOnCooldown` all agree; `Dialogs_Various.CooldownTime` is the lone outlier |
| quest / ability / radius / cells | `quête` / `capacité` / `rayon` / `cases` | `cellules` for cells | Core `Quest`, `Abilities`, `Ability_EffectRadius`, "dans un rayon de 5 cases" |
| quality tiers | `horrible·médiocre·normal·bon·excellent·merveille·légendaire` | | Core `QualityCategory_*` |
| wood / plasteel / uranium / jade / steel / silver / gold | `bois` / **`plastacier`** / `uranium` / `jade` / `acier` (stuffAdjective **`métal`**) / `argent` / `or` | `plastacier` as `plastique`, `acier` as the stuff adjective | Core labels + `stuffAdjective` |
| monosword / plasmasword / zeushammer | `épée mono-moléculaire` / `épée plasmique` / `marteau de Zeus` | | Royalty labels; the adjective is hyphenated `mono-moléculaire` |
| mechanite / mechanoid | `mécanites` (F) / `mécanoïde` | `nanomachine` | Core `FibrousMechanites`, Royalty monosword desc |
| **ultratech** | **`ultratechnologie`** (noun), `ultratechnologique` attributively | `ultra-tech` | Royalty `BroadshieldCore` ("Une pièce d'ultratechnologie"); Core `TechLevel_Ultra` is just `ultra` |
| wielder / bearer | `utilisateur` / `porteur` | | Odyssey `EMPPulser` ("centrée sur l'utilisateur"), Core gene descs |
| item stash / bandit camp / ancient mercenaries / sealed crate | `planque` / `camp de bandits` / `mercenaires anciens` / `caisse scellée` | | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement | `colonie abandonnée` | | Odyssey `AbandonedSettlement` (its own label is oddly plural); Core's WorldObjectDef says `base de faction abandonnée` |
| tribesman / tribespeople / chief / fierce tribe | `indigène` / `indigènes` / `chef` / `tribu indigène féroce` | | Core `TribeRough` |
| **raider** | **`pillards`** | `assaillants` | Core `RaiderKing38.title`=`roi des pillards`; Ideology's MemeDef `Raider`=`pilleur` is the outlier |
| quest / mod UI: Cancel / Reset / Reset to defaults / Default / None | `Annuler` / `Réinitialiser` / `Réinitialiser les valeurs par défaut` / `Par défaut` / `Aucune` | | Core buttons |
| Traders will pay more/less for it. | `Les commerçants en paieront un prix plus élevé.` / `Les commerçants en paieront moins cher.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim (its `JadeInlay` uses a third variant, `paieront plus cher pour cela`) |

The six Odyssey ports have official fr labels, and descriptions matching our English word
for word for four of them (`ornemental`, `laid`, `incrusté d'or`, `incrusté de jade`);
copy those verbatim. `Lightweight` (`léger`) and `Cumbersome` (`encombrant`) need only
their aim-vs-swing clause adapted. As in ko, de and es, Odyssey's `Ugly` adjective
*indices* differ from ours, so re-map by meaning — but replace all six ports' adjectives
with invariant forms per the rule above.

Mod-decided terms pending native review (from the 2026-07-29 commit), every trait
adjective among them gender-invariant by construction: `pointe perforante` (armor spike,
with `perce-armure` / `brise-plaque`), `barbelé` (barbed; barbs = `barbelures`), `fondu en
cloche` (bell-cast), `taché de sang` (blood-stained), `carbonisé`, `à contrepoids`
(counterweighted), `sans rebond` (dead-blow, from the real tool term *marteau sans
rebond*), `émaillé`, `empoisonné` (envenomed), `à ailettes` (flanged; flanges =
`ailettes`), `à tête lourde` (head-weighted), `pointe d'aiguille`, `opiacé`,
**`marteau-pilon`** (piledriver — the literal *sonnette de battage* is too obscure),
`à cœur de plasma`, `à quillons` (quilloned — `quillon` is itself the French source word;
crossguard = `garde en croix`, guard = `garde`), `fil de rasoir` (razored), `à dents de
scie` (serrated), `de renom` (storied), `clouté` (studded; studs = `clous`), `tête de
Zeus`; **`Parade`** (the parry mote, register-matched to the noun `Esquive`) with
`parer`/`détourner` in the log lines, `secousse tellurique` (earthshake), `cri de
ralliement` (rallying cry) / **`galvanisé`** (rallied — `rallié` reads as "joined a cause"
in French, so the pair is deliberately loosened and is the likeliest reviewer question),
`accumulation de sédatif` with the stage ladder `dosé`/`vaseux`/`sous sédatif`, `taillade
déchiquetée` / `perforation déchiquetée` (ragged), `forgé par un maître` (master-forged),
`bande de guerre` (warband), `camp de la bande de guerre`, `troupe de guerre` (war party),
**`chef de guerre`** (warlord — Core's `Warlordess56.title` is `machine de guerre`, a loose
rendering that does not mean warlord, so this one is a coinage), `couperet` (cleaver),
`mailloche` (maul), `taillant` (bit), `épieu` (lance), `pique` (pike), `gourdin`
(bludgeon), and the colours `rouge sang` / `noir carbone` / `violet émail` / `blanc
mono-moléculaire` / `orange plasma` (patterned on Odyssey's `bleu glacier` / `orange feu`).
The 2026-07-30 WeaponCategoryDef labels are likewise mod-decided: `mêlée` (melee, Core
skill label), `coupant` / `perçant` / `contondant` (bladed / pointed / blunt, the
ToolCapacityDef adjectives), `lourd` (heavy), `à garde` (guarded).

#### Brazilian Portuguese (from this repo's machine-assisted generation, 2026-07-29)

Language folder is **`PortugueseBrazilian`** (tar: `PortugueseBrazilian (Português
Brasileiro).tar`). RimWorld ships European `Portuguese` as a *separate* language; a pt-PT pass
would be its own folder, not edits to this one. `LanguageInfo.xml` declares
`languageWorkerClass` **`LanguageWorker_Portuguese`** — the two languages share one worker.

**The worker does almost nothing, and that is the finding that shapes everything else**
(decompile-verified). `LanguageWorker_Portuguese` overrides **only** `WithIndefiniteArticle` and
`WithDefiniteArticle` (prepending `o `/`a `/`os `/`as `, `um `/`uma `/`uns `/`umas ` by gender).
It has **no `PostProcessed` override**, so the base `LanguageWorker.PostProcessed` runs — and that
only calls `MergeMultipleSpaces()`. No elision, no contraction, no `'s` rewriting, no particles.

**So Portuguese is the hard case: its contractions are orthographically mandatory and nothing
supplies them.** `de`+`o`=`do`, `de`+`a`=`da`, `em`+`o`=`no`, `em`+`a`=`na`, `a`+`o`=`ao`,
`a`+`a`=`à`, `por`+`o`=`pelo` (plus every plural). Consequences:

- **Never write `de` / `em` / `a` / `por` directly before a `[X_definite]` symbol.** `_definite`
  prepends a bare `o `, nothing fuses it, and the literal **"de o pirata"** ships. Simulating the
  worker in four lines of Python confirms it, and **vanilla pt-BR ships exactly this bug**: Core
  `RulePacks_CombatMelee` has `o [destroyed_targets] de [RECIPIENT_definite]` and `esquivou de
  [INITIATOR_definite]`; `Combat_FailIncludes` has `balançou seu(ua) [WEAPON_label] em
  [RECIPIENT_definite]`. Frequency is not correctness.
- **The clean escapes are `com`, `para`, `contra`, `sem`, `sobre`, `entre`** — none contract with
  the article, so `com [X_definite]` is safe. Otherwise restructure so the entity is a subject.
- **The idiomatic vanilla technique is to use the bare `[X_label]` and write the contracted article
  yourself, hedged**: Core's ranged pack writes `do(a) [INITIATOR_label]`, `pelo(a) [projectile]`.
- There are **zero `{replace:}` blocks** anywhere in pt-BR's rulepacks — vanilla never even
  attempted Spanish's contraction scaffolding. Don't invent it; restructure.
- `PostProcessThingLabelForRelic` is the base version (returns `null` for any label containing a
  space), so unlike German there is no hardcoded noun list constraining `ThingDef` labels.

Style rules from the vanilla pt-BR data (mandatory; counts are values with `<!-- EN: -->`
comments stripped):

- **ASCII straight double quotes** for cited def labels — `{1} "{0}" não pode mais...`,
  `O decreto foi intitulado de "{0}".` 70 ASCII `"` against 2 curly `“` and **zero** guillemets.
- **Zero em dashes and zero en dashes** in the entire corpus, so an English `—` must be
  **reflowed** (as in es and fr; the opposite of de, which mandates `–`). Ellipsis is ASCII `...`
  (47, vs 0 `…`); apostrophe is ASCII `'` (90, vs 0 `’`).
- **No space before `:` `;` `!` `?`** — tight `x:` 444 against ` :` 44. The exact opposite of
  French, and the two languages are otherwise close enough that this is an easy cross-contamination.
- No `¿`/`¡` — that is Spanish only.
- **Formality is `você`, decisively**: 428 `você`, 36 `sua colônia`, and **zero** `tu`/`teu`.
  Imperatives take the você form (`Clique`, `Selecione`, `Escolha`, `Certifique-se`, `Faça`).
- Descriptions end `.`; labels, buttons and stat fragments take none, and labels are lowercase.
- `ThoughtDef` stage descriptions are the register exception: first-person, informal, present.
- `labelNoun` **carries the indefinite article** (`um corte`, `uma facada`, `uma queimadura`) —
  the shape de/es/fr share and ja/ko/zh lack.

**Gender hedging is a FIFTH distinct technique, and pt-BR applies it to the surface text itself.**
Where de tags nouns inline, es keeps parallel symbol families and fr constrains the rule, pt-BR
writes a literal **`(a)`** into the string and moves on — pervasively, in articles, participles,
contractions and possessives alike: `O(a)`, `um(a)`, `do(a)`, `pelo(a)`, `danificado(a)`,
`destinado(a)`. Two shapes by field, as in es:

- A `.Translate()` / `deathMessage` string takes the **inline resolver split**: Core
  `Cut.deathMessage` = `{0} foi cortad{PAWN_gender ? o : a} até a morte.` (2-arg form; Core ships
  no 3-arg genderless fallback for these, so don't invent one).
- Rulepack and def surface text takes the literal `(a)`. Vanilla also writes a sloppy `seu(ua)`;
  write `seu(sua)` if you need it.
- **Exception worth knowing:** Core's `injuryProps` labels are **bare masculine with no hedge**
  (`Cortado fora` for both `Cut` and `Stab`, `Queimado`), and its tend-state labels likewise
  (`enfaixado`, `suturado`, `fixado`) even on feminine wounds like `facada`. That is uniform across
  12+ defs, not a slip, so match it there rather than agreeing as es and fr do.

**Name grammar: pt-BR is the PREPOSED case, and it is the tightest constraint of any language so
far.** Odyssey's pt-BR `NamerUniqueWeapon` kept **English's word order** instead of adapting it to
Portuguese's postposing norm, and hardcoded the articles:

```
<li>r_weapon_name(p=2)->[weapon_adjective] [weapon_noun]</li>       <!-- PREPOSED -->
<li>r_weapon_name(p=0.5)->O [weapon_type] da [badass_concept]</li>  <!-- hardcoded O + da -->
<li>r_weapon_name(p=0.5)->O [weapon_adjective] [weapon_type]</li>   <!-- PREPOSED -->
<li>r_weapon_name(p=0.5)->[badass_concept] do [weapon_type]</li>    <!-- inverts the EN possessive -->
<li>weapon_adjective(p=2)->[trait_adjective]</li>
```

All three adjective slots prepose, and `weapon_noun` resolves to `[weapon_type]`, `[badass_noun]`
or `[badass_concept]` — all mixed gender. The file is also defective in ways a language folder
cannot fix: `O`/`A`/`da`/`do` are hardcoded (so `O espada longa da tormento` is reachable), and
`[badass_concept] do [weapon_type]` inverts English's `[badass_concept]'s [weapon_type]` — the
same inversion fr shipped. Our rules are *added* to this pack, so those keep firing. Keep ours
correct; do not try to repair theirs.

- **`traitAdjectives` must be gender-invariant, AND the prepositional escape is unavailable.**
  es and fr could satisfy invariance with a `de …` phrase because their slot *postposes*;
  preposed it is broken ("de aletas espada longa"). A bare noun is equally broken preposed
  ("ouro ceifador"), though Odyssey's pt-BR file does it. So the only legal shape is a genuinely
  **invariant adjective** — masculine form ending `-e` (`cortante`, `mordente`, `trovejante`,
  `perfurante`, `célebre`, `fulgurante`), `-l` (`letal`, `brutal`, `cruel`, `ancestral`,
  `ornamental`, `horrível`, `venerável`), `-z` (`veloz`, `feroz`), `-ar`/`-or`
  (`monomolecular`, `singular`), `-m`, `-s` — or an invariant colour compound (`vermelho sangue`,
  `preto carvão`, `verde jade`, `cinza`, `violeta`).
- **Because that is so restrictive, treat `traitAdjectives` in pt-BR as a free choice of invariant
  epithets in the trait's semantic field, not a literal rendering.** They are alternative flavour
  epithets for a generated name, not terminology; `label` and `description` stay literal. This is
  the same trade the de pass made for uninflected stems, one notch tighter. Worked departures from
  the 2026-07-29 run: gold drops the metal entirely for shine (`reluzente`, `resplandecente`,
  since `dourado`/`áureo` both inflect); "crystalline" becomes `impalpável`; "charred" becomes
  `incombustível`; `-forme` shape adjectives were the way into "cross-guarded" (`cruciforme`) and
  "hooked" (`falciforme`); Ugly re-maps to `rude`/`horrível`/`abominável`.
- **Odyssey's own pt-BR trait adjectives violate this throughout** (`preciso`, `sobrecarregado`,
  `desajeitado`, `volumoso`, `monstruoso`, `dourado`), surviving only on its near-all-masculine gun
  roster. Do not copy them even for the six ports whose labels and descriptions you do copy.
- **`namerLabels` are bare lowercase nouns with NO marker** — as in es and fr, the inverse of de.
  **Core pt-BR ships a curated weapon-noun corpus at `Strings/Words/Nouns/Weapons.txt`** (adaga,
  clava, cutelo, espada, faca, gládio, lâmina, lança, machado, marreta, martelo, pique, porrete) —
  exactly the register this field wants. Check it before coining.
- **The stuff frame is `de`, and the shared `weapon_adjective` rule must be DROPPED.** Core
  `ThingMadeOfStuffLabel` is `{1} de {0}` ("espada longa de aço") and every pt-BR `stuffAdjective`
  is a bare noun (`aço`, `plastiaço`, `madeira`, `jade`, `ouro`, `prata`, `urânio`), so `de` +
  material never contracts and never elides. But English's `weapon_adjective->[stuff_adjective]`
  lands in the **preposed** slot, so a `de …` value reads "de aço espada longa". Drop that rule as
  the de pass did, and build only postposed `r_weapon_name` patterns (`[weapon_noun] de
  [stuff_adjective]`), article-free. Dropping an entry is safe — the checker enforces no
  `<li>`-count parity on list-valued keys.
- **`[X_possessive]` is unusable; pt-BR joins fr in writing the possessive literally.** Core
  `Keyed/Grammar.xml` sets `Prohis`=`o`, `Proher`=`a`, `Proits`=`o(a)` — pt-BR maps the English
  possessive onto a bare **definite article** keyed off the **possessor's** gender, while
  Portuguese must agree with the **possessed** noun. Counting values vs comments confirms vanilla
  agrees: **zero** uses in the combat rulepacks (101 comment-only), which write `[deflecting] na
  sua armadura` instead; the 21 live uses are all Backstory/Research/Tale prose.
- **Battle-log `rulesStrings` are preterite** (`esquivou`, `desviou`, `raspou`, `deslizou`,
  `acertou`) — as in es, not fr's passé composé or de's Präteritum. `[skillAdv]` values are
  `-mente` adverbs (`incompetentemente`, `desajeitadamente`, `proficientemente`), and Core places
  `[skillAdvMaybe]` **before** the verb in `r_logentry`.
- **Quest grammar is the simple kind**, like es and fr: `[discoveryMethod]` carries no case markers
  and is used bare, so nothing needs `{replace:}`-ing away, and `questSubjectRules` needs only the
  plain `subject` / `questMapFeature` / `questMapText` families. Two Odyssey pt-BR renderings worth
  reusing verbatim: `uma arma [WEAPON_quality] única` and `Se você conseguir capturar ou matar o
  líder, poderá pegar essa arma única.` (also `O grupo contém:`).

**The trait row is INVERTED relative to de and es, which is the easiest single mistake to make
here.** pt-BR `WeaponTraits` and `Stat_ThingUniqueWeaponTrait_Label` are both **`Características`**,
while Core's pawn-trait `Traits` **and** Royalty's `Stat_Thing_PersonaWeaponTrait_Label` are both
**`Traços`**. So `características` is *our* word and `traços` belongs to pawn traits and PWU's
persona domain. In de and es all three collapse; in fr they never collide; pt-BR splits them the
other way round from ja.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | `característica` / `características`; standalone `Características da Arma` | **`traço`** | `WeaponTraits`, `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits`; `Traços` is the pawn-trait AND persona word |
| unique weapon | `arma única` | | Odyssey `UniqueWeapon` = `Arma única` |
| **unique \<weapon\>** (ThingDef label) | **`<arma> único/única`** — postposed and **AGREEING** | | Odyssey `arco grande único`, `fuzil de assalto único`. Unlike fr's invariant `unique`, `único` inflects: `espada longa única` but `machado único` |
| longsword / spear / mace / knife | `espada longa` (F) / `lança` (F) / `maça` (F) / `faca` (F) | `espada larga`, `clava` for mace | Core labels. **4 feminine vs 3 masculine on our roster**, so a masculine default is wrong more often than right |
| gladius / axe / warhammer / club | `gládio` (M) / `machado` (M) / `martelo de guerra` (M) / `porrete` (M) | `machado de guerra` | Core |
| monosword / plasmasword / zeushammer | `espada monomolecular` / `espada de plasma` / `martelo de zeus` (vanilla lowercases zeus) | | Royalty labels |
| **monomolecular (adjective)** | **`monomolecular`** — unhyphenated, and invariant so it is safe as a traitAdjective | `mono-molecular` | Royalty `MeleeWeapon_MonoSword.label`. es and fr both hyphenate; pt-BR does not |
| tool: handle / point / edge / blade / head / shaft | `cabo` / `ponta` / `lâmina` / `lâmina` / `cabeça` / `eixo` | `punho`, `fio`, `haste` | Core+Royalty `tools.*.label`. pt-BR uses `lâmina` for BOTH edge and blade; `eixo` for shaft is a weak vanilla choice but is the anchor |
| hilt | `empunhadura` | `punho` | Royalty |
| **cut / stab (DamageDef)** | **`corte` / `facada`** | `punhalada` (that is the **ToolCapacityDef** label) | **pt-BR does NOT split DamageDef from HediffDef** — both `Stab` are `facada`, both `Cut` are `corte`. The split is instead DamageDef/HediffDef `facada` vs ToolCapacityDef `punhalada`. Check all three def types, not just two |
| blunt / burn / flame / stun (DamageDef) | `pancada` / `queimadura` / `chama` / `atordoamento` | | Core |
| toxic \<damage\> label | postposed **agreeing** adjective: `arranhão tóxico`, `mordida tóxica`, so a toxic stab is `facada tóxica` | a prefix | Core `ScratchToxic`, `ToxicBite` |
| ragged / shredded wound | `dilacerado` / `dilacerada`; scar `cicatriz de laceração` | | Core `Shredded.labelNoun` = `uma ferida dilacerada`; scar built on Core's `cicatriz de corte` template |
| Cut off / Cut out | `Cortado fora` (Core uses it for `Stab` too); a burn is `Queimado` | a `(a)` hedge | Core `injuryProps` — bare masculine, unlike es/fr |
| bandaged / sutured / set | `enfaixado` / `suturado` / `fixado` — bare masculine even on feminine wounds | | Core `HediffComp_TendDuration`, uniform across 12+ defs |
| woozy / sedated / wearing off | `tonto` / `sedado` / `ficando tonto` | | Core `Anesthetic.stages.*`. **Do not spend `tonto` or `sedado` elsewhere**; `ficando tonto` labels the *decay* direction, so leave it free too |
| blood loss / toxic buildup / anesthetic | `perda de sangue` / `acúmulo tóxico` / `anestésico` | | Core |
| **Dodge (TextMote)** | **`Esquiva`** — a NOUN, so a parry mote must be a noun too | a participle | Core `TextMote_Dodge`. de and es use participles here; pt-BR and fr use nouns |
| **stagger** | **`cambalear`** (verb) / `cambaleio` | `escalonado` | Core `StoppingPowerExplanation`: "farão o alvo **cambalear**". `StaggerDurationFactor.label` = `multiplicador de tempo escalonado` and its desc `desaceleração escalonada` are vanilla **mistranslations** (escalonado = tiered/phased, not stumbling) — do not propagate, exactly as in fr |
| EMP | **`PEM`** | `EMP` | Core `EMP.label`; Royalty `capacitor de PEM`; Odyssey `pulsador PEM` |
| melee armor penetration / melee damage multiplier | `penetração de armadura corpo-a-corpo` / `multiplicador de dano corpo a corpo` | | Core StatDefs — vanilla is inconsistent on the hyphens; match the anchor the screen shows. Unhyphenated `corpo a corpo` wins 56:9 overall |
| move speed / max hit points / deterioration / flammability / market value | `velocidade de movimento` / `pontos de vida máximo` (sic, vanilla singular) / `taxa de deterioração` / `inflamabilidade` / `valor de mercado` | | Core StatDefs |
| **quest** | **`missão`** | `busca` | Core `LetterNewQuest` = "{DISCOVEREDFROM} uma nova **missão**." The Keyed `<Quest>Missões</Quest>` is a plural anomaly |
| **cooldown** | **`tempo de recarga`**; short `recarga`; "on cooldown" → `em recarga` | `esfriamento` | Core `Cooldown`, `StatsReport_Cooldown`, `AbilityOnCooldown`, Odyssey `cooldownGerund`. `CooldownTime`=`Esfriamento` is the lone outlier |
| ability / mood / colour / faction / radius / cells | `habilidade` / `humor` / `cor` / `facção` (sic, vanilla double-c) / `raio` / `células` | `casas` for cells | Core Keyed; "raio de 5 células" |
| quality tiers | `horrível·pobre·normal·bom·excelente·obra-prima·lendário` | `ruim` for poor | Core `QualityCategory_*`. Note `lendário` is spoken for by the tier, so it cannot render "storied"/"fabled" |
| Cancel / Reset / Reset to defaults / Default / None / Confirm | `Cancelar` / `Redefinir` / `Restaurar padrão` / `Padrão` / `Nenhum` / `Aceitar` | `Confirmar` | Core buttons. `Confirm`=`Aceitar`, `ResetBinding`=`Restaurar padrão` (the settings-window analog `RestoreToDefaultSettings` is the plural `Restaurar Padrões`) |
| wood / plasteel / uranium / jade / steel / silver / gold | `madeira` / **`plastiaço`** / `urânio` / `jade` / `aço` / `prata` / `ouro` | `plastaço`, `plástico` | Core labels; `stuffAdjective` is identical to the label for all of these |
| **purple (weapon colour)** | **`roxo`** | `púrpura` | Odyssey `UniqueWeapon_Purple`=`roxo`, `MutedPurple`=`roxo suave`. Colour compounds pattern on `laranja fogo`, `azul gelo`, `azul elétrico`, `verde tóxico` |
| mechanite / mechanoid | **`mecanitos`** (M) | `nanomáquinas`, `mecanitas` | Core+Royalty: `mecanitos` 47 vs `mecanitas` 4. `nanomáquinas` renders English *nanomachines*, a different word — the same trap as ko |
| wielder / bearer | `usuário` / `portador` | | Odyssey `EMPPulser` ("centrado no usuário"), Royalty descs |
| ultratech | `Ultra` (tech level); `ultratecnológico` attributively | | Core `TechLevel_Ultra`; cf. `TechLevel_Archotech`=`Arquotecnológico` |
| item stash / bandit camp / ancient mercenaries / sealed crate | `esconderijo de itens` / `acampamento de bandidos` / `mercenários antigos` / `Caixote Selado` | `caixa` for crate | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement | `assentamento abandonado` | `colônia abandonada` | Core+Odyssey SitePartDefs |
| tribesman / tribespeople / chief / fierce tribe | `nativo` / `nativos` / `chefe` / `Tribo Feroz` | `tribal` as the pawn noun | Core `TribeRough.pawnSingular`/`pawnsPlural`/`leaderTitle`/`label` (its prose does say `os tribais`) |
| **raider** | **`invasor`/`invasores`** | `saqueador` | Core: `invasores` 16 vs `assaltantes` 12 vs `saqueadores` 5 |
| **warlord** | **`senhor da guerra`** — vanilla-attested, lowercase in a `leaderTitle` slot | | Core `Warlordess56.title`, Ideology `place_foeLeader` |
| map loot / art inscription | `pilhagem do mapa` / `inscrição artística` | | Core `Reward_CampLoot_Label`; Core only has `TabArt`=`Arte`, so the inscription wording is ours |
| Traders will pay more/less for it. | `Comerciantes pagarão mais por ela.` / `Comerciantes pagarão menos por ela.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim |

The six Odyssey ports have official pt-BR labels, and descriptions matching our English word for
word for four of them — copy those verbatim (`ornamental`, `feia`, `incrustação de ouro`,
`incrustação de jade`). `Lightweight` (`leve`) and `Cumbersome` (`desajeitado`) need only their
aim-vs-swing clause adapted. As in ko/de/es/fr, Odyssey's `Ugly` adjective *indices* differ from
ours (its EN order is monstrous/crude/ugly, ours is crude/ugly/monstrous), so re-map by meaning —
then replace all six ports' adjectives with invariant forms per the rule above.

Mod-decided terms pending native review (from the 2026-07-29 commit). Labels are noun phrases,
which also sidesteps standalone-display agreement; every trait adjective is invariant by
construction: `espigão perfurante` (armor spike), `farpas` (barbed), `cabeça de sino` (bell-cast),
`manchas de sangue` (blood-stained), `superfície carbonizada` (carbonized), `contrapeso`
(counterweighted), `cabeça sem rebote` (dead-blow), `esmalte vítreo` (enameled), `ponta
envenenada` (envenomed), `aletas` (flanged, matching es), `cabeça pesada` (head-weighted), `ponta
de agulha` (needle point), `ponta opiácea` (opiated), **`bate-estacas`** (piledriver — the standard
pt-BR machine name), `núcleo de plasma` (plasma-cored), **`gavilões`** (quillons — a genuine
Portuguese sword term, with `cruz da guarda` for crossguard and `guarda` for guard; **not**
`guarnição`, which vanilla spends on "military garrison"), `lâmina de navalha` (razored), `dentes
de serra` (serrated), `linhagem de renome` (storied), `cravos` (studded), `cabeça de zeus`
(zeus-headed, lowercase to match `martelo de zeus`); **`Bloqueio`** for the parry mote with
**`bloquear`** as the single verb across mote, stat line and battle log (`Parada` is the fencing
term but Core spends it on `Poder de parada` = stopping power, `Aparada` reads first as "trimming",
and `Desvio` collides with Core's armor-`deflected`→`desviou`); `tremor de terra` (earthshake —
`terremoto` avoided because Royalty spends it on `Neuroquake`=`terremoto neural`), `grito de
guerra` (rallying cry — no vanilla anchor at all, neither `arenga` nor `brado`) / `enardecido`
(rallied), `acúmulo de sedativo` with the stage ladder `entorpecido`/`tonto`/`sedado`, `dilacerado`
(ragged), `forjado por um mestre` (master-forged), **`bando de guerra`** (warband — deliberately
NOT `banda de guerra`, which in Brazil is the established term for a drum-and-bugle marching band;
Core attests `bando de invasores`), `acampamento do bando de guerra`, `tropa de guerra` (war
party), `guerreiro`/`invasor` (the faction's pawn nouns), `gume` (edge, as a namer noun),
`azagaia` (lance), `clava` (bludgeon only — mace stays `maça`), `cutelo` (cleaver), `marreta`
(maul), and the colours `vermelho sangue` / `preto carvão` / `roxo esmalte` / `branco
monomolecular` / `laranja plasma` (patterned on Odyssey's `azul gelo` / `laranja fogo`).
The 2026-07-30 WeaponCategoryDef labels are likewise mod-decided: `corpo a corpo` (melee,
Core skill label), `cortante` / `perfurante` / `contundente` (bladed / pointed / blunt —
`perfurante` over the `MeleePiercer` StatCategory's odd `afiado`), `pesado` (heavy),
`guarnecido` (guarded, echoing `UMW_Studded`'s `guarnecida de cravos`).

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”) — injected labels never inflect, and quoting
  sidesteps case and agreement problems. **Korean is the exception, and porting
  the ja form actively breaks it**: ko solves the same problem mechanically with
  josa markers, and `FindLastChar` looks through only ASCII `'` `"` `)` to find
  the syllable that decides the particle. Curly `“ ”` and corner `「 」` are not
  skipped, so `「{0}」(을)를` silently ships an unresolved `(을)를`. Inject bare and
  mark the particle.
- **The same def field can demand a different part of speech per language, and no
  checker sees it.** `traitAdjectives` wants an attributive phrase in ja (`の`/`な`-
  terminated), a bare modifier in zh (no trailing `的`), either form in ko (spaces,
  so verb forms work), in de an **uninflected adjective stem** that a
  `{replace:}` appends `-er/-e/-es` to — a noun is silently broken there and fine
  elsewhere — and in es anything **gender-invariant**, i.e. an invariant adjective or
  a `de …` phrase, since it postposes onto nouns of both genders with no agreement
  machinery — and the same in fr, where the legal shapes are a prepositional phrase or
  an adjective whose masculine form already ends in `-e`. Likewise `namerLabels` is plain
  text in ja/ko/zh, must carry a `|M|/|F|/|N|` marker in de, and must be a **bare
  unmarked noun** in es, fr and pt-BR. Before translating a field that feeds name generation,
  read how the target language's vanilla namer *consumes* it, not just how it reads.
- **Whether an adjective slot PREPOSES or POSTPOSES decides which invariant shapes are
  legal, and you must read the namer rather than assume the language's norm.** es and fr
  could satisfy `traitAdjectives`' gender-invariance with a `de …`/`en …` prepositional
  phrase *only because* their Odyssey namers postpose (`[weapon_noun] [weapon_adjective]`).
  pt-BR's norm is postposing too, but its namer kept **English's** order and preposes
  (`[weapon_adjective] [weapon_noun]`), which silently invalidates the prepositional escape
  ("de aço espada longa") and the bare-noun one ("ouro ceifador") — leaving only genuinely
  invariant adjectives. Same consequence for the shared `weapon_adjective->[stuff_adjective]`
  rule: fr and es translate it prepositionally, de and pt-BR must **drop** it and postpose
  via their own `r_weapon_name` patterns instead. Grep the namer for the symbol's position
  before choosing a part of speech.
- **When invariance leaves no faithful adjective, treat `traitAdjectives` as free epithets
  rather than forcing a broken agreement.** The field is a list of alternative flavour words
  for a generated name, not terminology, so substituting a same-register invariant word
  (pt-BR `cortante` for "honed", `impalpável` for "crystalline", `incombustível` for
  "charred", `reluzente` for "golden") costs nothing, while an inflecting literal is wrong on
  half the roster. Keep `label` and `description` literal — only this field is free. Check
  the roster's actual gender split before assuming masculine is the safe default: this mod's
  is 4 feminine to 3 masculine in es/fr/pt-BR, so it is not.
- **Gendered/inflecting languages solve name grammar in one of three ways, and you must
  find out which before writing any rulepack.** German tags each noun inline
  (`|M|Mäher`) and strips the marker with `{replace:}` per syntactic slot. Spanish
  instead keeps **parallel symbol families** (`badass_concept` / `badass_conceptF`)
  and writes one rule per gender. French uses neither: it puts a **constraint on the rule
  itself** (`staggered(SUBJECT_gender==Female)->est stupéfaite`), which the resolver
  selects on. The three are mutually exclusive: porting German's markers into Spanish
  ships literal `|M|` to screen, and porting Spanish's split into German loses the
  adjective endings. With the fr form, **always write the `==None` branch too** (or use
  `!=Female`) — a missing branch fails to resolve for genderless pawns, i.e. mechanoids.
  **pt-BR adds a fourth technique, and it is the bluntest: hedge in the surface text with a
  literal `(a)`.** Core pt-BR does this pervasively and to everything — articles, contractions,
  participles, possessives (`O(a)`, `um(a)`, `do(a)`, `pelo(a)`, `danificado(a)`) — which is why
  it needs neither markers nor parallel families nor rule constraints. Two cautions: the hedge
  belongs in *prose*, not in a generated proper name (Odyssey's pt-BR namer instead just
  hardcodes `O`/`A`/`da`, which is why it is broken), and a few Core fields are deliberately
  bare masculine with no hedge at all (`injuryProps` and tend-state labels, uniformly across
  12+ defs) — so match the field, not the language-wide habit.
- **Check whether the worker contracts before writing any contraction scaffolding — the
  answer inverts between languages.** Spanish must fuse `de`+`el` by hand in the rulepack
  with `{replace: de [X_definite]; "de &lt;color=#D09B61FF>el "-"&lt;color=#D09B61FF>del "}`,
  colour code and all. French must do the **opposite and write nothing**:
  `LanguageWorker_French.PostProcessed` elides `de`/`le`/`la`/`que` before a vowel and
  fuses `à le`→`au`, `de les`→`des` automatically, so hand-contracting would double-apply.
  **And vanilla is not uniformly right in either** — Core es ships a truncated variant of
  its idiom 20 times that renders "de del", and the fr worker turns `de le` into `de`
  rather than `du` while eliding *h aspiré* it should leave alone. Verify a vanilla
  pattern actually works before copying it; frequency is not correctness.
  **pt-BR is the third case and the worst: contractions are mandatory, nothing supplies them,
  and vanilla never even attempted a fix.** `LanguageWorker_Portuguese` overrides only the two
  article helpers, so the base `PostProcessed` runs and merely merges spaces; there are **zero
  `{replace:}` blocks** in the whole language. Core therefore ships `de o pirata` and `em a
  colona` in its own combat packs. Two authoring answers, both vanilla-attested: prefer a
  preposition that does not contract (`com`/`para`/`contra`/`sem`/`sobre`/`entre`) or make the
  entity a subject; or use the bare `[X_label]` and write the contracted article yourself,
  hedged — `do(a) [INITIATOR_label]`, which is what Core's ranged pack does.
- **A "no hidden mechanics" worker is itself a finding, not a reason to skip the check.** es's
  and pt-BR's workers impose no authoring requirements, but pt-BR's *absence* of a
  `PostProcessed` override is precisely what makes every contraction the author's problem.
  Read what the worker does **not** do as carefully as what it does, and note that languages can
  share one: `PortugueseBrazilian` declares `LanguageWorker_Portuguese`, so a pt-PT pass would
  inherit the same constraints.
- **The possessive symbol has four different correct answers, so never generalize
  one.** ko drops `[RECIPIENT_possessive]` (Korean omits possessives), de keeps it and
  inflects it inline (`[RECIPIENT_possessive]em Handschutz`), es keeps it **only
  before a singular noun** (es `su` has no plural form, so a plural possessed noun
  needs the definite article instead), and fr **cannot use it at all**: French `son`/`sa`
  agrees with the *possessed* noun while the symbol resolves from the *possessor's*
  gender, so Core fr writes the possessive literally (`son armure`). **pt-BR joins fr in
  writing it literally, but for a different underlying reason worth knowing:** its
  `Prohis`/`Proher`/`Proits` are `o`/`a`/`o(a)` — a bare **definite article**, not a possessive
  pronoun at all — still keyed off the possessor while Portuguese must agree with the possessed
  noun. Core pt-BR has zero live uses in its combat packs and writes `na sua armadura` instead.
  Check `Keyed/Grammar.xml` for the language's actual `Prohis`/`Proher`/`Proits` values rather
  than assuming the symbol inflects — fr's are `son`/`sa`/`son/sa` and pt-BR's are articles,
  which is the tell in both cases.
- **A DamageDef and a HediffDef of the same name often have different labels**, and
  translating from the wrong one is an easy, invisible error: es Core has DamageDef
  `Stab`=`apuñalamiento` but HediffDef `Stab`=`puñalada`; ko has `찔림` vs `베임`; de has
  `Stich` vs `Stichwunde`. Always confirm which def *type* you are looking at — **and check
  `ToolCapacityDef` as a third, because the split does not always fall between the same two
  types.** pt-BR uses one word for the DamageDef and HediffDef (`facada` for both) but a
  different one for the ToolCapacityDef (`punhalada`), so a two-way check would have missed it.
- **Core ships curated word corpora under `Strings/Words/`, and they are the right register for
  namer fields.** pt-BR's `Strings/Words/Nouns/Weapons.txt` supplied almost every `namerLabels`
  value in that pass (adaga, clava, cutelo, gládio, marreta, pique, porrete...). Check the
  corpus before coining a name-grammar noun; it is a source the glossary tables don't cover.
- **When two vanilla files disagree, prefer the nearer analog, not the more central
  one.** es Core's generic ColorDefs render purple `morado`, but Odyssey's own
  `UniqueWeapon_*` colour defs — same def type, same purpose as ours — render it
  `púrpura`. The closer file wins.
- **Don't spend a vanilla word on the wrong slot.** es Core's `Anesthetic` stages
  already own `atontado` (woozy) and `sedado` (sedated); assigning `sedado` to a
  different stage of our own hediff forced an invented word for the stage that
  actually means it. Map a def's stages against vanilla's equivalent ladder *first*,
  then coin only for what is left over.
- **Keep a trait adjective material-neutral.** A universal trait rolls on every stuff,
  so an adjective naming one material ("de acero carbonizado") is wrong on the wooden
  and jade rolls. Name the feature, not the substrate.
- **Distinguish comment occurrences from value occurrences when mining the tar.**
  Grepping a symbol across a language's files counts English `<!-- EN: -->` text too,
  which inverts the conclusion: `[RECIPIENT_possessive]` looked used in ko but was
  comment-only (ko drops it), and `[INITIATOR_definite]'s` looks used in de (63 hits)
  but appears in only 4 values, all inside `{replace:}` blocks that *delete* it.
  Strip comments before counting.
- **Check for a `LanguageWorker_<Language>` before generating.** It post-
  processes every string, so it can impose authoring requirements no amount of
  reading the vanilla data will reveal as *mandatory* — Korean's josa markers
  are invisible until you find `ReplaceJosa`. Decompile it:
  `ilspycmd "$RIMWORLD_PATH/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t
  "Verse.LanguageWorker_<Language>"`. Languages with heavy inflection (Russian,
  Polish, Turkish, Czech, German) are the ones to check first. German's rewrites
  a trailing `'s`, so a closing ASCII single quote followed by lowercase `s` is
  silently mangled — and its `PostProcessThingLabelForRelic` truncates weapon
  labels against a hardcoded noun list, which matters for this repo's ThingDef
  labels. **A worker can also do work *for* you, which is just as important to know**:
  French's elides and contracts automatically, so the correct authoring there is to write
  the uncontracted form and leave it alone. Read the worker before deciding what to write,
  not only to find what it forbids.
- **Simulate the worker rather than reasoning about it.** Its regexes are short enough to
  reimplement in a few lines of Python, and running your actual strings through them
  catches what eyeballing does not — it is how the fr pass confirmed `en or` → "en or",
  `le assaut` → "l'assaut", `dégâts de IEM` → "dégâts d'IEM", and how it found that
  Odyssey's own `Le [weapon_type]` rule emits "Le lance" and "L'hache" no matter what a
  mod adds.
- **Vanilla's translation of the very def you are extending can be broken, not just
  incomplete — check that a file's symbols are actually *referenced* before copying its
  technique.** Odyssey's fr `NamerUniqueWeapon` defines four gender/vowel symbol families
  that no rule uses (so all four are dead), hardcodes a masculine article, inverts the
  English possessives, and carries the translator's own unresolved
  `<!-- WeaponType feminine/masculine? -->` question. "Vanilla does it" is not evidence
  it works; grep for the symbol on the *left* of `->` and on the right.
- **A pre-existing vanilla defect you cannot fix should still be recorded.** Our rulepacks
  are *added* to vanilla's namer, so a broken vanilla `r_weapon_name` keeps firing. Note
  it in the glossary and keep your own patterns correct rather than trying to repair
  theirs from a language folder.
- **Know which resolver your strings actually reach** (decompile-verified).
  `"key".Translate(args)` goes to `Verse.GrammarResolverSimple`, *not* the full
  rulepack `GrammarResolver`, and the two support different things. On a plain
  `string` arg `GrammarResolverSimple` gives you `{N_gender ? … : … : …}`,
  `{N_definite}`, `{N_indefinite}`, `{N_plural}` and the pronoun family — gender
  is looked up from the word itself via `LanguageWordInfo`, so no `NamedArgument`
  metadata is needed. It implements **no `lookup` function at all**, so
  `{lookup: {0}; decline; N}` and every case form it would produce are
  unavailable there. For inflecting languages that means gender is usually
  solvable and **case is not**: restructure so nothing has to agree with the
  injected label. See the German glossary for worked rewrites.
- **A gender lookup that misses defaults to masculine** (`ResolveGender`'s
  `defaultGender`), and this mod's own weapon labels are never in the vanilla
  Gender tables — so `{N_gender ? …}` on a UMW label is a silent coin-flip, not a
  fix. Reserve it for vanilla nouns in nominative slots. `UMW_WeaponEnabledDesc`
  is the live example.
- **Name-grammar gender is solved in the rulepack, not by the resolver** — by one of the
  three techniques above (de's inline markers, es's parallel symbol families, fr's
  rule-level constraints), never by the resolver's `{N_gender}`. A bare noun list cannot
  be inflected afterwards, so decide which technique applies before writing any
  `RulePackDef` entries, including the `stuff_adjective` symbol.
- **The checker compares argument placeholders, not grammar constructs, and that
  distinction is deliberate.** `{0}`/`{PAWN_labelShort}` are supplied by the C# call site
  and must match English exactly; `{PAWN_gender ? é : ée : é(e)}` is inflection the target
  language needs and uninflected English never has. `Scripts/check-translations.py`
  excludes any `{...}` containing `?` before comparing (see the comment on
  `GRAMMAR_CONSTRUCT_RE`), which is what lets fr render `Cut.deathMessage` the way Core fr
  does. Confirm the named argument exists at the call site before relying on one:
  `HealthUtility` passes `pawn.Named("PAWN")` alongside `{0}` specifically for this.
- When an English string is reworded, refresh the EN comments in every
  language **in the same commit** — the checker reports the mismatch as STALE
  either way, but batching avoids churn.
- Coined vanilla terms may be a portmanteau in one language and a plain word
  in another — always check, never extrapolate between languages.
- Mod-coined terms recur in def labels AND in Keyed settings prose that
  restates them. When generation is chunked across files or subagents,
  reconcile those terms across the whole language before committing (the
  zh-Hans run needed an alignment pass for earthshake / rallying cry /
  rallied / storied).

## Workflows

### Initial generation (`/translate <Language>`)

1. Run the checker; confirm English itself is clean.
2. Enumerate the target key set: every Keyed key in
   `1.6/Languages/English/Keyed/UMW_UI.xml` and `UMW_Stats.xml`, plus every
   `required` DefInjected entry in the `Scripts/expected-injections.json`
   sidecar, taking the English source text from each entry's `english`
   field — NOT from `Defs/` or an existing language's file structure, which
   both miss the fields the sidecar's probe-driven walk exists to catch (see
   the file-map bullet above). Route the Royalty-gated defs' entries
   (`UMW_Warhammer_Unique`/`UMW_Axe_Unique` ThingDef,
   `UMW_ZeusHeaded`/`UMW_PlasmaCored`/`UMW_Monomolecular` WeaponTraitDef,
   `UMW_PlasmaOrange`/`UMW_MonoWhite` ColorDef) to
   `1.6/Mods/Royalty/Languages/<Language>/...`; everything else goes in the
   main `1.6/Languages/<Language>/` tree. The checker enforces this both
   ways — an entry must live in the load root that declares its def — and
   its missing-entry errors name the root a translation belongs under.
3. Extract the vanilla tar for the target language into the scratchpad;
   build a term list for the grounded terms above.
4. Translate via subagent(s) carrying: the glossary, the vanilla term list,
   the EN-comment requirement, placeholder rules, and formatting rules.
   Chunk by file section if the key count is large.
5. Run the checker (`--strict` for new languages); fix everything.
6. Review the diff yourself before committing. Commit message and PR text
   must state machine-assisted origin and invite native review.

### Update pass (`/translate update`)

1. Run the checker; it lists missing keys and stale entries per language.
2. Translate only that delta, refreshing each entry's EN comment.
3. Leave correct existing entries untouched. Re-run the checker.

### Audit only (`/translate check`)

Run the checker and report; change nothing.

## Optional in-game verification

RimWorld Dev Mode offers "Save translation report" and "clean up translation
files" (Verse.LanguageReportGenerator / TranslationFilesCleaner). These need a
running game with the mod loaded — useful as a final QA pass, not a substitute
for the checker.
