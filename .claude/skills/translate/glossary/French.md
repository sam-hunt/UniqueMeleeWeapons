# French — Unique Melee Weapons glossary

From this repo's 2026-07-29 machine-assisted generation. Family-shared
mechanics (`LanguageWorker_French`'s elision/contraction regexes, the `de
le`/`h`-aspiré traps, `WithDefiniteArticle`/`WithIndefiniteArticle` being
overridden, gender hedging via inline word-splitting, `labelNoun`'s
indefinite article, style rules, and vanilla-grounded common vocabulary)
live in the `l10n/` submodule at `l10n/languages/French.md` — this file
holds only what is specific to Unique Melee Weapons' weapon domain.
RimWorld's language folder is `French` (tar: `French (Français).tar`).

## `RulePackDef` naming grammar — this mod's own fields

The rule-level gender-constraint technique itself (one rule per agreement
class via `SUBJECT_gender==…`, the `!=Female` shorthand, and the
must-cover-`None` trap with its whole-string-falls-back-to-English failure
mode) is family-generic and lives upstream in `l10n/languages/French.md`
(promoted and decompile-verified 2026-08-18). Two def fields this mod
owns, both constrained by that namer:

- **`traitAdjectives` must be GENDER-INVARIANT** — the same requirement
  Spanish has, for the same reason (they postpose onto a `[weapon_noun]` of
  unknowable gender), and it bites harder here: this mod's roster is four
  feminine weapons (`hache`, `épée longue`, `masse`, `lance`) against three
  masculine (`glaive`, `couteau`, `marteau de guerre`), so a
  masculine-default adjective is wrong more often than right.
  **Odyssey's own fr file violates this throughout** (`léger`, `légère`,
  `lourde`, `gênante`, `perçante`, `laid`, `exacte`, plus plurals like
  `surdimensionnées`), which mostly survives on its almost entirely
  masculine gun roster — do not copy the adjectives even for the six ports
  whose labels and descriptions you do copy. Two legal shapes, both
  attested in that same file: a **prepositional phrase** (`à …`, `de …`,
  `en …`, `au …`, `sans …`, `d'…` — vanilla ships `sur mesure`, `à
  percussion`, `de choc`, `à sabot`, `haute capacité`), or an adjective
  already invariant in gender, i.e. one whose masculine form ends in `-e`
  (`agile`, `féroce`, `magnifique`, `malcommode`, `infâme`,
  `mono-moléculaire`). An invariant colour compound (`rouge sang`, `noir
  carbone`) also works.
- **`namerLabels` are bare lowercase nouns with NO marker** — as in
  Spanish, the inverse of German. Odyssey's fr namer never places an
  agreeing adjective or article beside `[weapon_type]`, precisely because
  its gender is unknowable there.

**The stuff frame is `en`, and it needs no elision work at all.** Core fr
`ThingMadeOfStuffLabel` is **`{1} en {0}`** ("épée longue en acier"), and
fr `stuffProps.stuffAdjective` values are bare nouns. So build the
`stuff_adjective` rules on `en [stuff_adjective]`: it composes with
Odyssey's postposing `[weapon_noun] [weapon_adjective]` pattern, and
unlike a `de` frame it cannot trip the `de le` bug. Keep English's
`weapon_adjective->[stuff_adjective]` rule but make it prepositional (as
in es; de had to drop it). **Trap: `Steel.stuffProps.stuffAdjective` is
`métal`, not `acier`** — the label and the stuff adjective differ, so a
steel weapon reads "épée longue en métal". Verify per material rather than
assuming the label.

**Quest grammar is the simple kind, like Spanish's.**
`[discoveryMethod]` carries no case markers and is used bare
(`[discoveryMethod] l'emplacement d'une infâme compagnie de mercenaires.`),
so there is nothing to `{replace:}` away, and `questSubjectRules` needs
only the plain `subject` / `questMapFeature` / `questMapText` families —
no oblique variants. Two Odyssey fr renderings are worth reusing verbatim:
`une arme unique [WEAPON_quality]` and `Si vous parvenez à capturer ou à
tuer le chef, vous pouvez prendre l'arme unique.`

**`unique <weapon>` is the easy case here:** `unique` is invariant in
gender and postposes, so one form serves every weapon — Odyssey ships
`arc long unique`, `fusil d'assaut unique`, `minigun unique`. No es-style
`único/única` or de-style ending needed.

**The trait row does NOT collapse in French.** Odyssey's `WeaponTraits` is
`Traits d'arme` and `Stat_ThingUniqueWeaponTrait_Label` is `Traits`, while
Core's pawn-trait section header is `Éléments marquants :` — a different
word entirely, unlike de (`Merkmale`) and es (`Rasgos`), where weapon and
pawn traits collide. Royalty's *persona* label is also `Traits`, but that
is PWU's domain.

## Weapon-domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | `Traits`; standalone `Traits d'arme` | | Odyssey `WeaponTraits`, `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` (which says `Traits d'armes` — vanilla is inconsistent on the plural) |
| unique weapon | `arme unique` | | Odyssey `UniqueWeapon` |
| longsword / spear / mace / knife | `épée longue` (F) / `lance` (F) / `masse` (F) / `couteau` (M) | `épée large`, `massue` for mace | Core labels |
| **gladius** | **`glaive`** | `gladius` | Core `MeleeWeapon_Gladius` — French translates it rather than borrowing |
| axe / warhammer / club | `hache` (F) / `marteau de guerre` (M) / `massue` (F) | `hache de guerre` | Core+Royalty. `hache` is h-aspiré: never put `la`/`de` straight before it |
| **breach axe** | **`hache de brèche`** | `hache de siège` | Core `MeleeWeapon_BreachAxe.label` — official fr; still h-aspiré like plain `hache` |
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
| wood / plasteel / uranium / jade / steel / silver / gold | `bois` / **`plastacier`** / `uranium` / `jade` / `acier` (stuffAdjective **`métal`**) / `argent` / `or` | `plastacier` as `plastique`, `acier` as the stuff adjective | Core labels + `stuffAdjective` |
| monosword / plasmasword / zeushammer | `épée mono-moléculaire` / `épée plasmique` / `marteau de Zeus` | | Royalty labels; the adjective is hyphenated `mono-moléculaire` |
| mechanite / mechanoid | `mécanites` (F) / `mécanoïde` | `nanomachine` | Core `FibrousMechanites`, Royalty monosword desc |
| **ultratech** | **`ultratechnologie`** (noun), `ultratechnologique` attributively | `ultra-tech` | Royalty `BroadshieldCore` ("Une pièce d'ultratechnologie"); Core `TechLevel_Ultra` is just `ultra` |
| wielder / bearer | `utilisateur` / `porteur` | | Odyssey `EMPPulser` ("centrée sur l'utilisateur"), Core gene descs |
| item stash / bandit camp / ancient mercenaries / sealed crate | `planque` / `camp de bandits` / `mercenaires anciens` / `caisse scellée` | | Core sites, Odyssey quest + `AncientSealedCrate` |
| abandoned settlement | `colonie abandonnée` | | Odyssey `AbandonedSettlement` (its own label is oddly plural); Core's WorldObjectDef says `base de faction abandonnée` |
| tribesman / tribespeople / chief / fierce tribe | `indigène` / `indigènes` / `chef` / `tribu indigène féroce` | | Core `TribeRough` |
| **raider** | **`pillards`** | `assaillants` | Core `RaiderKing38.title`=`roi des pillards`; Ideology's MemeDef `Raider`=`pilleur` is the outlier |
| Traders will pay more/less for it. | `Les commerçants en paieront un prix plus élevé.` / `Les commerçants en paieront moins cher.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim (its `JadeInlay` uses a third variant, `paieront plus cher pour cela`) |

The six Odyssey ports have official fr labels, and descriptions matching
our English word for word for four of them (`ornemental`, `laid`,
`incrusté d'or`, `incrusté de jade`); copy those verbatim. `Lightweight`
(`léger`) and `Cumbersome` (`encombrant`) need only their aim-vs-swing
clause adapted. As in ko, de and es, Odyssey's `Ugly` adjective *indices*
differ from ours, so re-map by meaning — but replace all six ports'
adjectives with invariant forms per the rule above.

## Mod-decided terms pending native review (from the 2026-07-29 commit)

Every trait adjective among them gender-invariant by construction: `pointe
perforante` (armor spike, with `perce-armure` / `brise-plaque`), `barbelé`
(barbed; barbs = `barbelures`), `fondu en cloche` (bell-cast), `taché de
sang` (blood-stained), `carbonisé`, `à contrepoids` (counterweighted),
`sans rebond` (dead-blow, from the real tool term *marteau sans rebond*),
`émaillé`, `empoisonné` (envenomed), `à ailettes` (flanged; flanges =
`ailettes`), `à tête lourde` (head-weighted), `pointe d'aiguille`,
`opiacé`, **`marteau-pilon`** (piledriver — the literal *sonnette de
battage* is too obscure), `à cœur de plasma`, `à quillons` (quilloned —
`quillon` is itself the French source word; crossguard = `garde en croix`,
guard = `garde`), `fil de rasoir` (razored), `à dents de scie` (serrated),
`de renom` (storied), `clouté` (studded; studs = `clous`), `tête de
Zeus`; **`Parade`** (the parry mote, register-matched to the noun
`Esquive`) with `parer`/`détourner` in the log lines, `secousse tellurique`
(earthshake), `cri de ralliement` (rallying cry) / **`galvanisé`**
(rallied — `rallié` reads as "joined a cause" in French, so the pair is
deliberately loosened and is the likeliest reviewer question), `accumulation
de sédatif` with the stage ladder `dosé`/`vaseux`/`sous sédatif`, `taillade
déchiquetée` / `perforation déchiquetée` (ragged), `forgé par un maître`
(master-forged), `bande de guerre` (warband), `camp de la bande de
guerre`, `troupe de guerre` (war party), **`chef de guerre`** (warlord —
Core's `Warlordess56.title` is `machine de guerre`, a loose rendering that
does not mean warlord, so this one is a coinage), `couperet` (cleaver),
`mailloche` (maul), `taillant` (bit), `épieu` (lance), `pique` (pike),
`gourdin` (bludgeon), **`défonceur`** (breacher — agentive noun from
*défoncer une porte/un mur*, "to smash/break down a door or wall", the exact
verb the vanilla `MeleeWeapon_BreachAxe` description uses for what the tool
does) and **`abatteur`** (breaker — grounded directly in that same vanilla
description's own wording, "elle excelle dans l'abattage des murs [...]",
so the noun echoes vanilla's own vocabulary rather than coining fresh), and
the colours `rouge sang` / `noir carbone` /
`violet émail` / `blanc mono-moléculaire` / `orange plasma` (patterned on
Odyssey's `bleu glacier` / `orange feu`). The 2026-07-30 WeaponCategoryDef
labels are likewise mod-decided: `mêlée` (melee, Core skill label),
`coupant` / `perçant` / `contondant` (bladed / pointed / blunt, the
ToolCapacityDef adjectives), `lourd` (heavy), `à garde` (guarded).
