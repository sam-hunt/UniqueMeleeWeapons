# Korean — Unique Melee Weapons glossary

From this repo's 2026-07 machine-assisted generation. Family-shared mechanics
(the full `ReplaceJosa` particle-marker breakdown, the digit-fallback and
quoting-interaction traps, the josa lint, style rules, and vanilla-grounded
common vocabulary) live in the `l10n/` submodule at `l10n/languages/Korean.md`
— this file holds only what is specific to Unique Melee Weapons' weapon
domain. RimWorld's language folder is `Korean` (tar: `Korean (한국어).tar`).

**Korean uses spaces**, unlike JP/zh: the ko namer composes `[weapon_adjective]
[weapon_noun]` with a space, so this mod's own `traitAdjectives` field may be
attributive verb forms (`가벼운`, `저주받은`) *or* bare noun modifiers (`황금`,
`신속`, `특제`). Genitive epithets carry their own `의` (`죽음의`).

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 특성 (stats-entry title 무기 특성) | 개성 | Odyssey `WeaponTraits` / `Stat_ThingUniqueWeaponTrait_Label`; 개성 is Royalty's *persona* word (`Stat_Thing_PersonaWeaponTrait_Label`), PWU's domain |
| unique weapon | 고유 무기 | | Odyssey `UniqueWeapon` |
| **unique \<weapon\>** (label) | **특제 \<weapon\>** | | Odyssey's ranged uniques: 특제 장궁, 특제 돌격소총 |
| longsword / spear / mace / knife / gladius / axe / warhammer | 장검 / 창 / 철퇴 / 단검 / 검 / 도끼 / 전투망치 | | Core/Odyssey/Royalty labels |
| breach axe | 파괴용 도끼 | | Core `MeleeWeapon_BreachAxe` — verbatim official ko label; tools.handle/head = 손잡이/머리 |
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
| item stash / bandit camp / ancient mercenaries / sealed crate | 귀중품 은닉처 / 도적 캠프 / 고대 용병들 / 밀봉된 상자 | | Core sites, Odyssey quest + `AncientSealedCrate` |
| tribesman / tribespeople / chief / fierce | 부족민 / 부족민들 / 족장 / 호전적인 | | Core `TribeRough` |
| wielder / bearer | 사용자 / 주인 | | Odyssey `EMPPulser`, Royalty descs |

**Cross-checked against PWU's own ko pass (landed the same day, independently
grounded).** Two rows genuinely diverge, and neither repo should silently
"fix" the other:

- **PWU's `mechanite` → 나노머신 row is wrong; use 기계입자.** PWU grounded on
  Royalty/Biotech only and concluded ko has no term for it. Core in fact has
  one in 7 files (`Hediffs_Local_Infections`, `Luciferium`, `Items_Exotic`,
  ...): all 36 English "mechanite" occurrences render 기계입자. 나노머신 is
  Core/Biotech's word for English *nanomachines*. Correct this when
  mirroring.
- **armor penetration: 방어 관통력 here, 관통력 in PWU — both correct.** Core
  Keyed `ArmorPenetration` is 관통력, but the StatDef this mod's info-card
  lines sit under is `MeleeWeapon_AverageArmorPenetration` = 근접 방어 관통력.
  Match whichever anchor the surrounding screen shows.

The six Odyssey trait ports have official ko labels/adjectives, and
descriptions that match our English verbatim for four of them (장식용,
난잡한 외형, 금 상감, 옥 상감); `Lightweight` 경량 and `Cumbersome` 불편 differ
only in aim-vs-swing, so adapt that clause alone. Note Odyssey's `Ugly`
adjective *indices* differ from ours: re-map by meaning (crude=조잡한,
ugly=난잡한, monstrous=끔찍한).

## Mod-decided terms pending native review (from the 2026-07 commit)

받아넘김 (parry, register-matched to `TextMote_Dodge` 회피), 전사단 (warband,
parallel to vanilla 용병단), 습격단 (war party), 두목 (warlord, distinct from
Pirate 대장), 날받이 / 십자 가드 (quillons / crossguard), 지진 강타
(earthshake), 결집의 외침 (rallying cry), 결집됨 (rallied), 유서 있는
(storied), 항타기 (piledriver), 무반동 (dead-blow), 아편 도포 (opiated), 독
도포 (envenomed), 법랑 (enameled), 날개 돌기 (flanged), 징 박음 (studded),
관통 스파이크 (armor spike), 선단 편중 (head-weighted), 균형추
(counterweighted), 종 주조 (bell-cast), 바늘 끝 (needle point), 미늘 (barbed,
keeping 갈고리 for its "hooked" adjective), 탄화 (carbonized), 혈흔
(blood-stained), 톱니 (serrated), 면도날 (razored), 단분자 / 플라즈마 코어 /
제우스 헤드 (the ultratech trio), 진정제 축적 (sedative buildup), 투여됨
(dosed), 찢긴 (ragged), 명장이 벼린 (master-forged), 도살도 (cleaver), 쇠메
(maul), 쇠뭉치 (mace head), 혈홍색 / 탄흑색 (colours, patterned on Odyssey's
염홍색 / 전청색). The 2026-07-30 WeaponCategoryDef labels are likewise
mod-decided: 근접 (melee, from Core 근접 무기), 잘림 / 찔림 / 맞음 (bladed /
pointed / blunt, the Core DamageDef labels), 중량 (heavy), 가드 (guarded,
matching the mod's 십자 가드). 파쇄부 / 파괴부 (breach axe's `breacher`/`breaker`
namerLabels, 2026-08): coined Sino-Korean axe-compounds distinct from the
plain axe entry's own namer vocabulary (도끼/도살도/도끼날/칼날) — 파쇄
("crush/shatter") vs 파괴 ("destroy") keep the two nouns clearly separate
while both read as axe-like weapon names ending in the 斧(부) "axe" root, in
the same coinage style as the existing 도살도 ("slaughter blade").
