# Brazilian Portuguese — Unique Melee Weapons glossary

From this repo's 2026-07-29 machine-assisted generation. Family-shared
mechanics (`LanguageWorker_Portuguese`'s almost-nonexistent `PostProcessed`,
the mandatory-contraction consequences, the gender-hedging `(a)` technique,
`[X_possessive]`'s unusability, style rules, and vanilla-grounded common
vocabulary) live in the `l10n/` submodule at
`l10n/languages/PortugueseBrazilian.md` — this file holds only what is
specific to Unique Melee Weapons' weapon domain. RimWorld's language folder
is `PortugueseBrazilian` (tar: `PortugueseBrazilian (Português
Brasileiro).tar`) — European `Portuguese` is a wholly separate language.

`PostProcessThingLabelForRelic` is the **base** implementation here (returns
`null` for any label containing a space), so unlike German there is no
hardcoded noun list constraining this mod's `ThingDef` weapon labels.
`labelNoun`-style constructions also carry the indefinite article (`um
corte`, `uma facada`, `uma queimadura`) — the shape de/es/fr share and
ja/ko/zh lack.

## Name grammar: pt-BR is the PREPOSED case, and it is the tightest constraint of any language so far

Odyssey's pt-BR `NamerUniqueWeapon` kept **English's word order** instead of
adapting it to Portuguese's postposing norm, and hardcoded the articles:

```
<li>r_weapon_name(p=2)->[weapon_adjective] [weapon_noun]</li>       <!-- PREPOSED -->
<li>r_weapon_name(p=0.5)->O [weapon_type] da [badass_concept]</li>  <!-- hardcoded O + da -->
<li>r_weapon_name(p=0.5)->O [weapon_adjective] [weapon_type]</li>   <!-- PREPOSED -->
<li>r_weapon_name(p=0.5)->[badass_concept] do [weapon_type]</li>    <!-- inverts the EN possessive -->
<li>weapon_adjective(p=2)->[trait_adjective]</li>
```

All three adjective slots prepose, and `weapon_noun` resolves to
`[weapon_type]`, `[badass_noun]` or `[badass_concept]` — all mixed gender.
The file is also defective in ways a language folder cannot fix: `O`/`A`/
`da`/`do` are hardcoded (so `O espada longa da tormento` is reachable), and
`[badass_concept] do [weapon_type]` inverts English's `[badass_concept]'s
[weapon_type]` — the same inversion fr shipped. Our rules are *added* to
this pack, so those keep firing. Keep ours correct; do not try to repair
theirs.

- **`traitAdjectives` must be gender-invariant, AND the prepositional
  escape is unavailable.** es and fr could satisfy invariance with a `de …`
  phrase because their slot *postposes*; preposed it is broken ("de aletas
  espada longa"). A bare noun is equally broken preposed ("ouro
  ceifador"), though Odyssey's pt-BR file does it. So the only legal shape
  is a genuinely **invariant adjective** — masculine form ending `-e`
  (`cortante`, `mordente`, `trovejante`, `perfurante`, `célebre`,
  `fulgurante`), `-l` (`letal`, `brutal`, `cruel`, `ancestral`,
  `ornamental`, `horrível`, `venerável`), `-z` (`veloz`, `feroz`), `-ar`/
  `-or` (`monomolecular`, `singular`), `-m`, `-s` — or an invariant colour
  compound (`vermelho sangue`, `preto carvão`, `verde jade`, `cinza`,
  `violeta`).
- **Because that is so restrictive, treat `traitAdjectives` in pt-BR as a
  free choice of invariant epithets in the trait's semantic field, not a
  literal rendering.** They are alternative flavour epithets for a
  generated name, not terminology; `label` and `description` stay
  literal. This is the same trade the de pass made for uninflected stems,
  one notch tighter. Worked departures from the 2026-07-29 run: gold drops
  the metal entirely for shine (`reluzente`, `resplandecente`, since
  `dourado`/`áureo` both inflect); "crystalline" becomes `impalpável`;
  "charred" becomes `incombustível`; `-forme` shape adjectives were the way
  into "cross-guarded" (`cruciforme`) and "hooked" (`falciforme`); Ugly
  re-maps to `rude`/`horrível`/`abominável`.
- **Odyssey's own pt-BR trait adjectives violate this throughout**
  (`preciso`, `sobrecarregado`, `desajeitado`, `volumoso`, `monstruoso`,
  `dourado`), surviving only on its near-all-masculine gun roster. Do not
  copy them even for the six ports whose labels and descriptions you do
  copy.
- **`namerLabels` are bare lowercase nouns with NO marker** — as in es and
  fr, the inverse of de. **Core pt-BR ships a curated weapon-noun corpus at
  `Strings/Words/Nouns/Weapons.txt`** (adaga, clava, cutelo, espada, faca,
  gládio, lâmina, lança, machado, marreta, martelo, pique, porrete) —
  exactly the register this field wants. Check it before coining.
- **The stuff frame is `de`, and the shared `weapon_adjective` rule must be
  DROPPED.** Core `ThingMadeOfStuffLabel` is `{1} de {0}` ("espada longa de
  aço") and every pt-BR `stuffAdjective` is a bare noun (`aço`,
  `plastiaço`, `madeira`, `jade`, `ouro`, `prata`, `urânio`), so `de` +
  material never contracts and never elides. But English's
  `weapon_adjective->[stuff_adjective]` lands in the **preposed** slot, so
  a `de …` value reads "de aço espada longa". Drop that rule as the de
  pass did, and build only postposed `r_weapon_name` patterns
  (`[weapon_noun] de [stuff_adjective]`), article-free. Dropping an entry
  is safe — the checker enforces no `<li>`-count parity on list-valued
  keys.
- **Battle-log `rulesStrings` are preterite** (`esquivou`, `desviou`,
  `raspou`, `deslizou`, `acertou`) — as in es, not fr's passé composé or
  de's Präteritum. `[skillAdv]` values are `-mente` adverbs
  (`incompetentemente`, `desajeitadamente`, `proficientemente`), and Core
  places `[skillAdvMaybe]` **before** the verb in `r_logentry`.
- **Quest grammar is the simple kind**, like es and fr: `[discoveryMethod]`
  carries no case markers and is used bare, so nothing needs
  `{replace:}`-ing away, and `questSubjectRules` needs only the plain
  `subject` / `questMapFeature` / `questMapText` families. Two Odyssey
  pt-BR renderings worth reusing verbatim: `uma arma [WEAPON_quality]
  única` and `Se você conseguir capturar ou matar o líder, poderá pegar
  essa arma única.` (also `O grupo contém:`).

**The trait row is INVERTED relative to de and es, which is the easiest
single mistake to make here.** pt-BR `WeaponTraits` and
`Stat_ThingUniqueWeaponTrait_Label` are both **`Características`**, while
Core's pawn-trait `Traits` **and** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` are both **`Traços`**. So
`características` is *our* word and `traços` belongs to pawn traits and
PWU's persona domain. In de and es all three collapse; in fr they never
collide; pt-BR splits them the other way round from ja.

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | `característica` / `características`; standalone `Características da Arma` | **`traço`** | `WeaponTraits`, `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits`; `Traços` is the pawn-trait AND persona word |
| unique weapon | `arma única` | | Odyssey `UniqueWeapon` = `Arma única` |
| **unique \<weapon\>** (ThingDef label) | **`<arma> único/única`** — postposed and **AGREEING** | | Odyssey `arco grande único`, `fuzil de assalto único`. Unlike fr's invariant `unique`, `único` inflects: `espada longa única` but `machado único` |
| longsword / spear / mace / knife | `espada longa` (F) / `lança` (F) / `maça` (F) / `faca` (F) | `espada larga`, `clava` for mace | Core labels. **4 feminine vs 3 masculine on our roster**, so a masculine default is wrong more often than right |
| gladius / axe / warhammer / club | `gládio` (M) / `machado` (M) / `martelo de guerra` (M) / `porrete` (M) | `machado de guerra` | Core |
| breach axe | `machado de irrupção` (M) | | Core `MeleeWeapon_BreachAxe.label` — official pt-BR term; `machado` is masculine, so `único` agrees as `machado de irrupção único` |
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
| melee armor penetration / melee damage multiplier | `penetração de armadura corpo-a-corpo` / `multiplicador de dano corpo a corpo` | | Core StatDefs — vanilla is inconsistent on the hyphens; match the anchor the screen shows. Unhyphenated `corpo a corpo` wins 56:9 overall |
| move speed / max hit points / deterioration / flammability / market value | `velocidade de movimento` / `pontos de vida máximo` (sic, vanilla singular) / `taxa de deterioração` / `inflamabilidade` / `valor de mercado` | | Core StatDefs |
| **cooldown** | **`tempo de recarga`**; short `recarga`; "on cooldown" → `em recarga` | `esfriamento` | Core `Cooldown`, `StatsReport_Cooldown`, `AbilityOnCooldown`, Odyssey `cooldownGerund`. `CooldownTime`=`Esfriamento` is the lone outlier |
| ability / mood / colour / faction / radius / cells | `habilidade` / `humor` / `cor` / `facção` (sic, vanilla double-c) / `raio` / `células` | `casas` for cells | Core Keyed; "raio de 5 células" |
| wood / plasteel / uranium / jade / steel / silver / gold | `madeira` / **`plastiaço`** / `urânio` / `jade` / `aço` / `prata` / `ouro` | `plastaço`, `plástico` | Core labels; `stuffAdjective` is identical to the label for all of these |
| **purple (weapon colour)** | **`roxo`** | `púrpura` | Odyssey `UniqueWeapon_Purple`=`roxo`, `MutedPurple`=`roxo suave`. Colour compounds pattern on `laranja fogo`, `azul gelo`, `azul elétrico`, `verde tóxico` |
| mechanite / mechanoid | **`mecanitos`** (M) | `nanomáquinas`, `mecanitas` | Core+Royalty: `mecanitos` 47 vs `mecanitas` 4. `nanomáquinas` renders English *nanomachines*, a different word — the same trap as ko |
| wielder / bearer | `usuário` / `portador` | | Odyssey `EMPPulser` ("centrado no usuário"), Royalty descs |
| ultratech | `Ultra` (tech level); `ultratecnológico` attributively | | Core `TechLevel_Ultra`; cf. `TechLevel_Archotech`=`Arquotecnológico` |
| item stash / bandit camp / ancient mercenaries / sealed crate | `esconderijo de itens` / `acampamento de bandidos` / `mercenários antigos` / `Caixote Selado` | `caixa` for crate | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement | `assentamento abandonado` | `colônia abandonada` | Core+Odyssey SitePartDefs |
| tribesman / tribespeople / chief / fierce tribe | `nativo` / `nativos` / `chefe` / `Tribo Feroz` | `tribal` as the pawn noun | Core `TribeRough.pawnSingular`/`pawnsPlural`/`leaderTitle`/`label` (its prose does say `os tribais`) |
| **raider** | **`invasor`/`invasores`** | `saqueador` | Core: `invasores` 16 vs `assaltantes` 12 vs `saqueadores` 5 |
| map loot / art inscription | `pilhagem do mapa` / `inscrição artística` | | Core `Reward_CampLoot_Label`; Core only has `TabArt`=`Arte`, so the inscription wording is ours |

The six Odyssey ports have official pt-BR labels, and descriptions matching
our English word for word for four of them — copy those verbatim
(`ornamental`, `feia`, `incrustação de ouro`, `incrustação de jade`).
`Lightweight` (`leve`) and `Cumbersome` (`desajeitado`) need only their
aim-vs-swing clause adapted. As in ko/de/es/fr, Odyssey's `Ugly` adjective
*indices* differ from ours (its EN order is monstrous/crude/ugly, ours is
crude/ugly/monstrous), so re-map by meaning — then replace all six ports'
adjectives with invariant forms per the rule above.

## Mod-decided terms pending native review (from the 2026-07-29 commit)

Labels are noun phrases, which also sidesteps standalone-display
agreement; every trait adjective is invariant by construction: `espigão
perfurante` (armor spike), `farpas` (barbed), `cabeça de sino`
(bell-cast), `manchas de sangue` (blood-stained), `superfície
carbonizada` (carbonized), `contrapeso` (counterweighted), `cabeça sem
rebote` (dead-blow), `esmalte vítreo` (enameled), `ponta envenenada`
(envenomed), `aletas` (flanged, matching es), `cabeça pesada`
(head-weighted), `ponta de agulha` (needle point), `ponta opiácea`
(opiated), **`bate-estacas`** (piledriver — the standard pt-BR machine
name), `núcleo de plasma` (plasma-cored), **`gavilões`** (quillons — a
genuine Portuguese sword term, with `cruz da guarda` for crossguard and
`guarda` for guard; **not** `guarnição`, which vanilla spends on "military
garrison"), `lâmina de navalha` (razored), `dentes de serra` (serrated),
`linhagem de renome` (storied), `cravos` (studded), `cabeça de zeus`
(zeus-headed, lowercase to match `martelo de zeus`); **`Bloqueio`** for the
parry mote with **`bloquear`** as the single verb across mote, stat line
and battle log (`Parada` is the fencing term but Core spends it on `Poder
de parada` = stopping power, `Aparada` reads first as "trimming", and
`Desvio` collides with Core's armor-`deflected`→`desviou`); `tremor de
terra` (earthshake — `terremoto` avoided because Royalty spends it on
`Neuroquake`=`terremoto neural`), `grito de guerra` (rallying cry — no
vanilla anchor at all, neither `arenga` nor `brado`) / `enardecido`
(rallied), `acúmulo de sedativo` with the stage ladder
`entorpecido`/`tonto`/`sedado`, `dilacerado` (ragged), `forjado por um
mestre` (master-forged), **`bando de guerra`** (warband — deliberately NOT
`banda de guerra`, which in Brazil is the established term for a
drum-and-bugle marching band; Core attests `bando de invasores`),
`acampamento do bando de guerra`, `tropa de guerra` (war party),
`guerreiro`/`invasor` (the faction's pawn nouns), `gume` (edge, as a
namer noun), `azagaia` (lance), `clava` (bludgeon only — mace stays
`maça`), `cutelo` (cleaver), `marreta` (maul), and the colours `vermelho
sangue` / `preto carvão` / `roxo esmalte` / `branco monomolecular` /
`laranja plasma` (patterned on Odyssey's `azul gelo` / `laranja fogo`).
The 2026-07-30 WeaponCategoryDef labels are likewise mod-decided: `corpo a
corpo` (melee, Core skill label), `cortante` / `perfurante` /
`contundente` (bladed / pointed / blunt — `perfurante` over the
`MeleePiercer` StatCategory's odd `afiado`), `pesado` (heavy),
`guarnecido` (guarded, echoing `UMW_Studded`'s `guarnecida de cravos`).

`UMW_BreachAxe_Unique`'s namer nouns for `breacher`/`breaker` (added
2026-08-23) are coined agent nouns, since neither maps to an existing Core
weapon-noun: **`arrombador`** (one who forces open locks/doors — the
established Portuguese term for a breacher/burglar, e.g. "arrombador de
cofres") and **`demolidor`** (demolisher), chosen distinct from `arrombador`
and from the already-spent `invasor`/`destruidor`-family words, evoking the
def's own "tearing down walls and doors" framing. The other two namer slots
ground to existing vocabulary: `axe`→`machado` (this table's breach axe row)
and `head`→`cabeça` (vanilla `MeleeWeapon_BreachAxe.tools.head.label`).
