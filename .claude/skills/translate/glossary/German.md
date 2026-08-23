# German — Unique Melee Weapons glossary

Preseeded from PersonaWeaponsUnbound's 2026-07-28 generation, generated and
extended in this repo the same day (resolved the stuff-naming and
`namerLabels`-marker questions PWU had left open). Family-shared mechanics
(case vs. gender, `lookup`/`decline` availability in plain Keyed strings,
`PostProcessed`'s `'s`-rewrite, `PostProcessThingLabelForRelic`'s 26-noun
list, stuff-naming inversion, style rules, and vanilla-grounded common
vocabulary) live in the `l10n/` submodule at `l10n/languages/German.md` —
this file holds only what is specific to Unique Melee Weapons' weapon
domain. RimWorld's language folder is `German` (tar: `German (Deutsch).tar`).

## The trait row collapses in German

Unlike Russian (свойство-not-черта, a different word for pawn traits),
German has no such split: Odyssey's `Stat_ThingUniqueWeaponTrait_Label`,
Royalty's `Stat_Thing_PersonaWeaponTrait_Label` **and** Core's pawn-trait
`<Traits>` are all **Merkmale**. The disambiguating form, when no weapon
context is present, is vanilla's own `StatsReport_WeaponTraits` =
**Waffenmerkmale**. Run the lookup anyway — just expect it to come back the
same.

## Case landmine — this repo's live examples

Two live cases in this repo, both plain-string injections, where the fix is
to restructure rather than rely on `{lookup: …}` (see the l10n submodule's
German file for why case, not gender, is the landmine): `UMW_ExcludeWoodStuffDesc`
(`MeleeWeapon_LongSword.label` = Langschwert, neuter) and
`UMW_WeaponEnabledDesc` (`weapon.label` — a **mod-coined** label, absent from
the Gender tables, so `ResolveGender` falls back to its `defaultGender` of
**Male** and `{0_gender ? …}` becomes a silent coin-flip). Reserve the gender
symbols for vanilla nouns in nominative slots.

## `RulePackDef` naming grammar — this mod's own fields

Two def fields this mod owns feed straight into Odyssey's German namer
machinery (inline `|M|`/`|F|`/`|N|` gender markers stripped by `{replace:}`
per syntactic slot — see the l10n submodule for the generic technique), so
German constrains their *form*, not just their wording:

- **`namerLabels` must each carry a `|M|`/`|F|`/`|N|` prefix**, marker then
  noun, no space (`|N|Langschwert`). Odyssey's own de namerLabels do:
  `|M|Großbogen`, `|N|Sturmgewehr`, `|F|Büchse`, `|F|schwere MP`. Odyssey's
  `{replace:}` slots are what emit the article and adjective ending, so an
  unmarked label leaves the strip with nothing to match and generates a
  broken name. Nothing in the checker sees this — it validates the key
  path, not the value's shape.
- **`traitAdjectives` must be uninflected adjective stems** that read
  correctly with `-er`/`-e`/`-es` appended (strong) and `-e`/`-en` after a
  definite article (weak), because `weapon_adjective_weapon_noun`
  concatenates the ending. Odyssey de ships `leicht`, `schnellziehbar`,
  `unhandlich`, `sperrig`, `klobig`, `schön`, `elegant`, `verziert`,
  `golden`, `vergoldet`, `jadeverziert`, `grässlich`, `primitiv`,
  `hässlich`, `zielsuchend`, `treffsicher`, `präzis`, `lahmlegend`,
  `EMP-verstärkt`. A **noun** is never valid here (`Panzerdorn` + `es`), nor
  is a stem ending in `-e`/`-er`, nor one containing a space. This is the
  inverse of ja/ko/zh, where the same field wants an attributive *phrase* —
  do not port those rules to German.

`PostProcessThingLabelForRelic`'s hardcoded 26-noun list is **directly
relevant to this repo's `ThingDef` weapon labels** — a de label ending
outside those 26 nouns yields a poor relic name. Note Waffe is *not* on the
list; Schwert, Hammer, Klinge, Messer, Speer, Keule, Axt and Stab are.

## Stuff naming — this mod's `stuff_adjective` symbol

German's stuff-naming inversion (`{1} aus {0}`, bare-noun `stuffAdjective`
values — see the l10n submodule) is settled by vanilla data, not a native
call: use the `aus [stuff_adjective]` frame. It composes correctly for
every material including the pre-inflected one ("aus dickem Fell"), and it
needs no gender agreement on the material at all. Concretely, in
`UMW_NamerStuffAdjectives`, **drop English's
`weapon_adjective->[stuff_adjective]` rule** — Odyssey's namer would append
`-er/-e/-es` to it and yield "Stahler" — and build `r_weapon_name` patterns
on the `aus` frame instead, reusing Odyssey's own `[weapon_noun_ungendered]`
to strip the gender marker (and noting its `badass_noun` list is unmarked,
so it is safe bare). Dropping a rule and adding others is fine: the checker
enforces no `<li>`-count parity on list-valued entries.

## Battle-log and quest content — register and case-marker findings

**Never *print* a `[X_definite]'s` genitive in German.** English
name-grammar and battle-log source is full of it; German cannot form a
genitive by suffixing a nominative definite phrase ("der Pirat" + s →
"der Pirats"). Vanilla de contains 63 occurrences — **all in
`<!-- EN: -->` comments** — and only 4 in actual German values, every one of
them inside a `{replace: …; " [INITIATOR_label]'s [WEAPON_label]"-""}` that
*deletes* the English construction before appending a
`{lookup: …; decline; …}` form. So keep the attacker a **nominative
subject** and restructure the clause (`[INITIATOR_definite] holte aus, doch
[RECIPIENT_definite] parierte den …`).

**German keeps `[RECIPIENT_possessive]` and inflects it inline** by
appending the ending — `von [RECIPIENT_possessive]er Panzerung`, `gegen
[RECIPIENT_possessive]e Panzerung`, `mit [RECIPIENT_possessive]em
Handschutz` (55 uses in Core combat packs). Unlike ko, do **not** drop it.

**Battle-log `rulesStrings` are Präteritum** (`wich … aus`, `verfehlte`,
`prallte … ab`, `sprang zur Seite`) — not the nominalized ko form and not
polite ja form. De's `[skillAdv]` values are adverbs/adjective stems
(`ungeschickt`, `geschickt`, `meisterhaft`, `kunstvoll`), so an optional
`[skillAdvMaybe]` composes cleanly as `[skillAdv] geführten` before a
masculine accusative noun.

**Quest descriptions must strip `[discoveryMethod]`'s case markers.** In
German that symbol resolves to a sentence frame containing `|thing_nom|` /
`|thing_gen|` / `|thing_dat|` / `|thing_acc|` plus four `_embedded`
variants (see Core `Keyed/Letters.xml` → `LetterNewQuest`). Every consumer
`{replace:}`s all eight and supplies its own noun phrase declined four
ways — Odyssey's `Script_AncientMercenaries.xml` is the worked example and
the direct template for this repo's warband quest, which reuses the same
symbol. Miss this and a raw `|thing_dat|` ships to screen. Odyssey's de
file also supplies the reusable renderings `eine einzigartige Waffe von
[WEAPON_quality]er Qualität` and `{LEADER_gender ? den Anführer : die
Anführerin} einzufangen oder zu töten`.

**`questSubjectRules` needs extra case families in German:** alongside
`questMapFeature`, Odyssey de adds `questMapFeatureGenIndef` and
`questMapFeatureDatIndef` (`einer Militärgarnison`), because
`Description_Map` consumes the oblique forms. Supply all three families.

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | Merkmal / Merkmale (standalone: Waffenmerkmale) | Eigenschaft, Attribut | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` |
| unique weapon | einzigartige Waffe | Unikat, besondere Waffe | Odyssey `UniqueWeapon` |
| **unique \<weapon\>** (ThingDef label) | **einzigartige/-r/-s \<Waffe\>** — lowercase adj, gender-agreeing | | Odyssey `einzigartiger Großbogen`, `einzigartiges Sturmgewehr`, `einzigartige Vollautomatikflinte` |
| longsword / spear / mace / knife / gladius | Langschwert / Speer / Streitkolben / Messer / Gladius | Schwert alone, Keule for mace | Core labels |
| axe / warhammer | Axt / Kriegshammer | Kriegsaxt, Streithammer | Core `MeleeWeapon_Axe`, `MeleeWeapon_Warhammer` |
| breach axe | Durchbruchsaxt | Sturmaxt, Brechaxt | Core `MeleeWeapon_BreachAxe` — official de Core label (verbatim); tools `Griff`/`Kopf` also match Core |
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
| cooldown | Abklingzeit; "on cooldown" → `klingt gerade ab` | | Odyssey `abilityProps.cooldownGerund` |
| tribesman/tribespeople / chief | Ureinwohner (same sing+plural) / Häuptling | Stammesangehöriger | Core `TribeRough` |
| abandoned settlement / ancient sealed crate | verlassene Siedlung / versiegelte Kiste | | Core+Odyssey SitePartDefs |
| warlord | **Kriegsherr** (vanilla-attested, not a coinage) | | Core `BackstoryDef Warlordess56.title` |
| mod (the noun) | **feminine** — `die Mod`, `dieser Mod` | der/das Mod | Core Keyed `Die Mod muss nach {1} geladen werden.` |
| monosword / plasmasword / zeushammer | Monoschwert / Plasmaschwert / Zeushammer | | Royalty labels (persona forms prefix Persona-) |
| cut / stab / blunt / burn (DamageDef) | Schnitt / Stich / Wucht / Verbrennung | Schnittwunde, Stichwunde (hediff labels) | Core DamageDefs |
| blood loss / bleed rate | Blutverlust / Blutung | Blutung for the hediff | Core `BloodLoss.label`, `BleedingRate` |
| EMP stun | Betäubt durch EMP | | Core `StunnedByEMP` |
| armor penetration / damage / accuracy | Rüstungsdurchdringung / Schaden / Genauigkeit | Panzerung, Treffsicherheit | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| stopping power / burst count / burst speed | Mannstoppwirkung / Schüsse pro Feuerstoß / Feuerrate | Durchschlagskraft | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| ability / mood / colour / faction | Fähigkeit / Stimmung / Farbe / Fraktion | | Core `Abilities`, `Mood`, `Color`, `Faction` |
| bandit camp / item stash / ancient mercenaries | Banditenlager / Versteck mit Waren / antike Söldner | Räuberlager, Warenlager | Core `BanditCamp.label`, `ItemStash.label`, PawnGroup label |
| mechanite | Mechaniten | Mechanite | Royalty monosword desc |
| wielder | Träger | Anwender, Nutzer | Royalty weapon-trait descs |
| Crafting (the skill) | Handwerk | Herstellung, Basteln | Core `Crafting.label` |
| bill / recipe (both) | Auftrag | Rezept, Rechnung | Core `TabBills`, `AddBill`, every `Stat_Recipe_*_Desc` — de collapses the two |

The six Odyssey trait ports have official de labels and adjectives, and four
have descriptions matching our English word for word — copy those verbatim
(`verziert`, `hässlich`, `vergoldet`, `jadeverziert`). `Lightweight`
(`leicht`) and `Cumbersome` (`unhandlich`) differ only in aim-vs-swing, so
adapt that clause alone. As in ko, Odyssey's `Ugly` adjective *indices*
differ from ours: re-map by meaning (crude=`primitiv`, ugly=`hässlich`,
monstrous=`grässlich`). `Lightweight`'s "nimble" has a Core anchor — the
Gladius description's `leicht und wendig`.

## Mod-decided terms pending native review (from the 2026-07-28 commit)

All uninflected stems where they are trait adjectives: `Panzerdorn` (armor
spike), `Widerhaken` (barbed), `glockengegossen` (bell-cast), `blutbefleckt`
(blood-stained), `karbonisiert` (carbonized), `Gegengewicht`
(counterweighted), `rückschlagfrei` (dead-blow), `emailliert` (enameled),
`vergiftet` (envenomed), `gerippt` (flanged, flanges = `Schlagrippen`),
`kopfschwer` (head-weighted), `monomolekular`, `Nadelspitze` (needle
point), `opiatbeschichtet` (opiated), `Rammkopf` (piledriver),
`plasmaumhüllt` (plasma-cored), `Parierstangen` (quilloned;
quillon/crossguard = `Parierstange`, third synonym `Handschutz`),
`rasierscharf` (razored), `gezahnt` (serrated), `geschichtsträchtig`
(storied), `genietet` (studded, studs = `Nieten`), `Zeuskopf` (zeus-headed,
capacitor = `Zeus-Kondensator`); `parieren`/`Pariert` (parry,
register-matched to `Ausgewichen`), `Erdstoß` (earthshake), `Schlachtruf`
(rallying cry), `angespornt` (rallied), `Sedierung` (sedative buildup),
`ausgefranst` (ragged), `meistergeschmiedet` (master-forged), `Kriegerbande`
(warband), `Kriegerbandenlager` (warband camp), `Kriegszug` (war party),
`Stammeskrieger` / `Stammesraider` (tribal warrior/raider, on Core's
dominant `Raider`), `Spalter` (cleaver), and the colours `blutrot` /
`karbonschwarz` / `emailviolett` / `monomolekularweiß` / `plasmaorange`
(patterned on Odyssey's `eisblau`/`feuerorange`). The 2026-07-30
WeaponCategoryDef labels are likewise mod-decided: `Nahkampf` (melee, Core
skill label), `Schnitt` / `Stich` / `Wucht` (bladed / pointed / blunt, the
Core DamageDef labels), `schwer` (heavy), `bewehrt` (guarded, matching the
`-bewehrt` pattern of the Quilloned family).

The 2026-08-23 breach axe (`UMW_BreachAxe_Unique`) namerLabels coin two
agent nouns distinct from the grounded `|F|Axt` (axe) and vanilla `|M|Kopf`
(head) slots: `Durchbrecher` (breacher — agent noun from `durchbrechen`,
"to break through", echoing the vanilla `Durchbruchsaxt` label itself) and
`Brecher` (breaker — the plain agent noun from `brechen`, "to break"),
both masculine and both distinct stems so the two namer slots don't
collide.
