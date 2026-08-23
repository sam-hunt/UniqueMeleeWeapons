# Spanish (Castellano) — Unique Melee Weapons glossary

From this repo's 2026-07-29 machine-assisted generation (adds the
`RulePackDef` name-grammar findings on top of UWU's/PWU's shared pass).
Family-shared mechanics (`LanguageWorker_Spanish`'s lack of hidden
authoring requirements, the `de el`→`del`/`a el`→`al` contraction fix,
`[RECIPIENT_possessive]`'s no-plural limitation, the parallel-symbol-family
gender technique, style rules, and vanilla-grounded common vocabulary) live
in the `l10n/` submodule at `l10n/languages/Spanish.md` — this file holds
only what is specific to Unique Melee Weapons' weapon domain. RimWorld's
language folder is `Spanish` (the Castilian tar; `SpanishLatin` is a
separate language needing its own pass).

## Style facts specific to this mod's fields

- **`unique <weapon>` postposes and agrees**: Odyssey ships `arco grande
  único`, `escopeta automática única`, `minigun única` — so this mod's own
  `ThingDef` labels follow the same `<arma> único/única` shape.
- **Two different gender hedges, and the right one depends on the field**:
  a `deathMessage` takes the inline resolver form (`{0} ha muerto quemad{0_gender
  ? o : a}.`), while a bare-participle `injuryProps.destroyedLabel` takes a
  capitalized `(a)` (`Lacerado(a)`, `Seccionado(a)`, `Quemado(a)`).
- `labelNoun` **carries the indefinite article** (`un corte`, `una
  puñalada`, `una quemadura`) — the same shape German has and ja/ko/zh
  don't.

## `RulePackDef` naming grammar — this mod's own fields

Spanish solves name-grammar gender by splitting parallel symbol families
(`badass_concept` vs `badass_conceptF`, `concept` vs `conceptF` — see the
l10n submodule for the generic technique), which constrains this mod's own
fields:

- **`namerLabels` are bare lowercase nouns with NO markers** — the exact
  inverse of German. Odyssey's es namer never puts an article or an
  agreeing adjective beside `[weapon_type]`, precisely because its gender
  is unknowable there.
- **`traitAdjectives` must be GENDER-INVARIANT**, because they postpose
  straight onto a weapon noun of either gender (`[weapon_type]
  [trait_adjective]` → `espada larga` F or `martillo de guerra` M). Two
  legal shapes, both used throughout Odyssey's own es trait file: an
  invariant adjective (`-e`, `-al`, `-ar`, `-z`, `-ista`, `-ble`, `-il` —
  `torpe`, `elegante`, `ornamental`, `ágil`, `veloz`, `brillante`,
  `horripilante`), or a **prepositional phrase** (`de oro`, `de jade`, `de
  adorno`, `de gran tamaño`, `de manejo torpe`, `con buscador`). A bare
  `-o`/`-a` adjective is silently broken on half the weapons. Note the
  trait's own `label` is a different field and *may* inflect — Odyssey
  uses default masculine (`ligero`, `feo`). Also keep such an adjective
  **material-neutral**: a universal trait rolls on wood, jade and plasteel
  too, so `de acero carbonizado` is wrong where `de superficie carbonizada`
  is right.
- **Weapon names carry no definite article at all** in Odyssey's es
  patterns. Drop English's "The" rather than trying to supply `el`/`la`.
- **es redefines `weapon_adjective` as a prepositional phrase**, not an
  adjective (`weapon_adjective->del [concept]` / `de la [conceptF]`). Its
  `badass_adjective` list survives but is referenced by no rule — dead
  weight in es.

**The stuff frame inverts, and here the `weapon_adjective` rule SURVIVES**
(unlike German, where it had to be dropped). Core es `ThingMadeOfStuffLabel`
is `{1} de {0}`, and es `stuffProps.stuffAdjective` values are bare nouns
(`acero`, `plastiacero`, `madera`, `jade`, `oro`, plus pre-framed `piel
gruesa` / `cuero ligero`). Because es `weapon_adjective` is *already*
prepositional, `weapon_adjective->de [stuff_adjective]` composes correctly
with every Odyssey pattern (`espada larga de acero`, `filo de plastiacero`).
Build the `r_weapon_name` patterns on the same `de` frame, article-free.

**Battle-log `rulesStrings` are preterite** (`evitó`, `falló`, `vaciló`, `se
tropezó`, `se tambaleó`, `se resbaló`, `saltó`) — not the perfect. es
`[skillAdv]` values are adverbs (`incompetentemente`, `ineptamente`), and
Core places `[skillAdvMaybe]` *before* the verb.

**Quest grammar is markedly simpler than German's.** `[discoveryMethod]`
carries no case markers in es — Odyssey uses it bare (`[discoveryMethod] la
ubicación de una infame compañía de mercenarios.`) — so there is nothing to
`{replace:}` away, and `questSubjectRules` needs only the plain `subject` /
`questMapFeature` / `questMapText` families, with no genitive/dative
variants. Two Odyssey es renderings are worth reusing verbatim: `un arma
única: [WEAPON_quality]` (a colon sidesteps quality-adjective agreement)
and `Si logras capturar o matar al líder, puedes tomar su arma.`

**The trait row collapses in Spanish, exactly as it does in German.**
Odyssey's `Stat_ThingUniqueWeaponTrait_Label`, `WeaponTraits` **and**
Core's pawn-trait `Traits` are all **Rasgos**; the disambiguated form is
vanilla's own `StatsReport_WeaponTraits` = **Rasgos del arma**. Royalty's
*persona* word is `Características` (PWU's domain, not ours). Run the
lookup anyway; expect a collision.

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | `rasgo` / `rasgos`; standalone `Rasgos del arma` | `propiedad`, `característica` | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits`; `Características` is Royalty's persona word |
| unique weapon | `arma única` | | Odyssey `UniqueWeapon` |
| longsword / spear / mace / knife / gladius | `espada larga` (F) / `lanza` (F) / `maza` (F) / `cuchillo` (M) / `gladius` (M) | | Core labels; genders matter for the `único/única` suffix |
| axe / warhammer / club | `hacha` (F, takes *el/un hacha*) / `martillo de guerra` (M) / `porra` (F) | | Core |
| breach axe | `hacha zapeadora` (F) | | Core `MeleeWeapon_BreachAxe.label` — official es language pack |
| **breach axe handle** | **`mango`** | `empuñadura` | Core `MeleeWeapon_BreachAxe.tools.handle.label` — landmine: breaks the axe/warhammer family pattern above (`empuñadura`); the official string uses `mango` instead, matching bladed/blunt weapons. Ground the handle word per weapon, don't assume the family convention. The breach axe's tool-head is `cabezal`, also Core-grounded, distinct from the mace/warhammer `cabeza` |
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
| cooldown / ability / radius / cells | `enfriamiento` / `habilidad` / `radio` / `casillas` | | Core |
| mechanite / mechanoid | `mecanita`/`mecanitas` (F) / `mecanoide` | `nanomáquina` | Royalty monosword desc |
| wielder / bearer | `usuario` / `portador` | | Odyssey `EMPPulser` (`pulso PEM`, `centrado en el usuario`), Royalty trait descs |
| item stash / bandit camp / ancient mercenaries / sealed crate | `Alijo de objetos` / `campamento de bandidos` / `mercenarios antiguos` / `caja sellada` | | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement | `asentamiento abandonado` (Odyssey) or `colonia abandonada` (Core) | | both attested; prefer Odyssey's for a site part |
| tribesman / tribespeople / chief / fierce tribe | `tribal` / `tribales` / `jefe` / `tribu agresiva` | | Core `TribeRough` |
| **warlord** | **`señor de la guerra`**; short `caudillo` | | Core `Warlordess56.title`/`.titleShort` — vanilla-attested, not a coinage |
| Traders will pay more/less for it. | `Los comerciantes pagarán más por ella.` / `… menos por ella.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim |

The six Odyssey ports have official es labels, adjectives and — for four of
them — descriptions matching our English word for word (`ornamental`,
`feo`, `incrustación de oro`, `incrustación de jade`); copy those verbatim.
`Lightweight` (`ligero`) and `Cumbersome` (`torpe`) need only their
aim-vs-swing clause adapted. As in ko and de, Odyssey's `Ugly` adjective
*indices* differ from ours: re-map by meaning (crude=`de aspecto
horrible`, ugly=`horripilante`, monstrous=`horrible`).

## Mod-decided terms pending native review (from the 2026-07-29 commit)

Every trait adjective among them gender-invariant by construction: `pico
perforante` (armor spike), `con lengüetas` (barbed), `fundido en campana`
(bell-cast), `manchado de sangre` (blood-stained), `carbonizado`,
`contrapesado`, `sin rebote` (dead-blow), `esmaltado`, `envenenado`, `de
aletas` (flanged; flanges = `aletas`), `de cabeza pesada` (head-weighted),
`punta de aguja`, `opiáceo`, `martinete` (piledriver), `núcleo de plasma`,
`con gavilanes` (quilloned; quillons = `gavilanes`, crossguard = `cruz`,
guard = `guarda` — **not** `guarnición`, which vanilla uses for "military
garrison"), `filo de navaja` (razored), `serrado`, `de renombre` (storied),
`con tachuelas` (studded), `cabeza de Zeus`; `Desviado` (the parry mote,
register-matched to `Esquivado`; `Parada` is the fencing term and the
likeliest reviewer alternative) with `desviar`/`paró`/`detuvo` in the log
lines, `sacudida telúrica` (earthshake), `arenga` (rallying cry) /
`arengado` (rallied), `acumulación de sedante` with the stage ladder
`medicado`/`atontado`/`sedado`, `corte desgarrado` / `puñalada desgarrada`
(ragged), `forjado por un maestro` (master-forged), `banda de guerra`
(warband), `campamento de la banda de guerra`, `partida de guerra` (war
party), `guerrero tribal` / `saqueador tribal`, `machete` (cleaver,
vanilla-attested), `mazo` (maul), `lanzón` (lance), `pica` (pike),
`garrote` (bludgeon), and the colours `rojo sangre` / `negro carbón` /
`púrpura esmalte` / `blanco mono-molecular` / `naranja plasma` (patterned
on Odyssey's `azul hielo` / `naranja fuego`). The 2026-07-30
WeaponCategoryDef labels are likewise mod-decided: `cuerpo a cuerpo`
(melee, Core skill label), `cortante` / `punzante` / `contundente` (bladed
/ pointed / blunt), `pesado` (heavy), `con guarda` (guarded, matching the
`de guarda` construction already used for Quilloned).

The 2026-08-23 breach axe pass coins two `namerLabels` nouns (bare, no
markers needed — these aren't postposed trait adjectives): `zapador`
(breacher) reuses vanilla's own root — Core's `hacha zapeadora` derives from
`zapador`, the Spanish military term for a sapper/combat engineer whose job
is breaching fortifications, so this isn't a fresh coinage so much as
un-collapsing an adjective vanilla already ships back into its noun; and
`demoledor` (breaker) is chosen to sit clearly apart from `zapador` while
still evoking the vanilla description's own "derribar paredes, puertas y
otras estructuras" (tearing down walls, doors and other structures).
