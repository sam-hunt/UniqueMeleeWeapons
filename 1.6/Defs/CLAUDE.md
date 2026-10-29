# Def guidance (`1.6/Defs/`, `1.6/Patches/`, and the defs under `1.6/Mods/*/Defs/`)

Design rules and def-level traps. Per-trait values, tuning and the history of each decision live
in the trait file's header; the per-category membership, trait families, exclusion-token registry
and rationale live on the `WeaponCategoryDef` files. Read the relevant category file before adding
a trait. C#-side mechanics are in `Source/1.6/CLAUDE.md`.

## Layout

- **One def per file**, named after the def with the `UMW_` prefix stripped and the meaningful
  `_Unique` suffix kept. Trait files sit in a subfolder named after their `WeaponCategoryDef`
  (`Melee/`, `Bladed/`, `Pointed/`, `Blunt/`, `Heavy/`, `Guarded/`), so a trait's `<weaponCategory>`
  is obvious from its path. Defs load recursively and the deploy manifest globs, so new files and
  subfolders need no build change.
- `texPath` points at the weapon's texture *folder* with `graphicClass`
  `UniqueMeleeWeapons.Graphic_RandomComplex`; art and mask rules are in `Textures/CLAUDE.md`.
- **Royalty-analog traits are def-level `MayRequire`-gated**, as is any mod-owned def only they
  consume. A skipped def never enters the `DefDatabase`, so category rolls simply don't see it.
  Content that cannot gate itself (textures, DefInjected) ships from a compat root instead
  (`1.6/Mods/CLAUDE.md`).

## Design rules for weapons and traits

- **A trait must read as a physical property of the weapon** (flanges, studs, guards, coatings,
  provenance), never an unexplained wielder blessing. Ability traits are the accepted exception,
  tone-checked case by case.
- **Trait names name the physical feature** (quillons, a dead-blow head, an opiated point), never
  the effect verb. The granted *ability* and any wielder-side hediff name the act instead.
- **Control-effect budget:** a new stun/stagger/control proposal must *displace* an existing
  control effect, not add to the census (held at Odyssey's ~1-in-8 share).
- **Category taxonomy is locked at 6:** `UMW_Melee` (universal), the mechanism categories
  `UMW_Bladed`/`UMW_Pointed`/`UMW_Blunt`, the handling category `UMW_Heavy`, and the weapon-gating
  category `UMW_Guarded`. Single-trait categories are fine (Odyssey ships several); the bar is that
  each trait be mechanically meaningful (a `UMW_Reach` category was rejected: melee has no reach
  mechanic, so its traits would be flavour-only).
- **A category gates by weapon; `exclusionTags` only prevent co-rolls.** Use a new category when a
  trait requires a construction feature the weapon may not have (`UMW_Guarded`); use a token when
  traits are alternatives within a family. Tokens are global across categories; the registry is on
  `WeaponCategoryDefs/Melee.xml`.
- **Generation throws** unless a weapon's categories include a `canGenerateAlone=true` trait that
  yields a `traitAdjective`. `UMW_Melee`'s universal `UMW_Lightweight` guarantees this: don't
  remove it, and don't make it inheritance-dependent (`WeaponCategoryDefs/Melee.xml` has why the
  six Odyssey ports are deliberate self-contained copies, not XML inheritance).
- **`MarketValue`: factor only for value-scaling, flat offset for everything else.** Following
  Odyssey: factor for precious-inlay scaling or devaluation (`Ugly`/`Cumbersome`-likes), offset
  for any added capability. A trait that is both carries both halves. Size an offset from the
  nearest Odyssey analog and name it inline in the trait file.

## What a trait can and cannot do on melee

- **Most mechanically interesting `WeaponTraitDef` fields do nothing on melee.**
  `damageDefOverride`, `extraDamages`, `additionalStoppingPower`, `burstShot*` and
  `ignoresAccuracyMaluses` are read only by the projectile path; `marketValueOffset`, `killThought`,
  the `bonded*` fields and `equippedStatOffsets` only by bladelink (persona) weapons. All of those
  are **silently inert** on `CompUniqueWeapon`: never use them, and don't patch one live either (a
  1.0.x patch routing `equippedStatOffsets` rode vanilla's stat pipeline per frame for one trait;
  the swap is in `WeaponTraitDefs/Pointed/NeedlePoint.xml`). What reaches melee natively:
  `statOffsets`/`statFactors`, `equippedHediffs`, `abilityProps`, `forcedColor`.
- **Wielder-side effects are expressed as weapon stats first.** A trait is a physical property of
  the weapon, and Odyssey prices every trait buff/malus as a weapon-thing stat, so
  `equippedHediffs`, the only *general* vanilla-applied wielder-side vehicle, stays an unused escape
  hatch, not a precedented tool. A combat *outcome* gets its own mechanism instead (Quilloned's
  parry). Market value rides `statOffsets`/`statFactors` on `MarketValue`.
- **Gear-routed pawn stats (the one exception, decompile-verified on 1.7):** `PsychicSensitivity`,
  `PsychicEntropyMax` and `PsychicEntropyRecoveryRate` carry a `StatPart_GearStatOffset`
  (sensitivity also a `StatPart_GearStatFactor`) with `includeWeapon`, which reads the primary
  weapon's `PsychicSensitivityOffset`/`PsychicSensitivityFactor`/`PsychicEntropyMaxOffset`/
  `PsychicEntropyRecoveryRateOffset` through the same comp path as `MaxHitPoints` and lists the
  weapon under "Relevant gear". So a trait `statOffsets` entry on one of those weapon stats reaches
  the wielder with no hediff, no patch and no summary case (Anomaly's bioferrite stuff already does
  this). A full assembly scan found no other pawn-side read of a weapon-thing stat
  (`MeleeHitChance` has none, so Needle-point's malus stays a weapon stat). The worked case, with
  its quality-scaling and cache caveats, is the eltex-plated spec in `Docs/Specs/` until it ships.
- **Trait stat mods reach any stat of the weapon *thing***, not just combat ones: item-condition
  stats (`MaxHitPoints`, `DeteriorationRate`, `Flammability`) are fair game. Melee damage and armor
  penetration share the single `MeleeWeapon_DamageMultiplier` stat (there is **no** melee AP stat),
  so raising AP via stats always raises damage; use `MeleeToolModExtension` for independent
  per-tool changes.
- **Before a trait names a new part-less stat, check how hot its readers are.** A stat only traits
  modify is cached per thing forever (handled by the C# side, `Source/1.6/CLAUDE.md`), but Unique
  Weapons Unbound marks every trait-named part-less stat mutable **game-wide**, so under UWU that
  stat loses its per-thing cache for every thing in the game. The current three are read per
  attack, per fire-spread check, or behind a 10-tick cache, so they cost nothing measurable.
- **On-hit `DamageDef` payloads work for free.** `DamageDef.additionalHediffs` and the damage
  workers (`Flame`'s ignition, `EMP`'s stun) are applied source-agnostically by every
  `Thing.TakeDamage`, so an extra-damage effect carrying the right `DamageDef` needs no new C#.
  Reuse a Core `DamageDef` where one fits; clone only when a field must change.
- Anything beyond the four native fields goes through one of the six `DefModExtension`s in
  `Source/1.6/Traits/`, and **every new effect mechanism must gain a `TraitEffectSummary` case** or
  it ships with no stated effect in-game (`Source/1.6/CLAUDE.md`).

## Weapon defs

- **Every weapon def must carry a `CompEquippable`-derived ability comp.** `CompUniqueWeapon.Setup`
  dereferences `CompEquippableAbilityReloadable` with no null check whenever a rolled trait carries
  `abilityProps`, and only one such comp is allowed per thing, so every unique def replaces its
  inherited comps wholesale (`<comps Inherit="False">`, uniform even where no ability can roll).
  **If base-game weapon comps change in a vanilla update, replicate the change in every unique.**
  The one sanctioned difference: a unique mirrors its base's quality, so the quality-less VFEP
  warcasket pair carries no `CompQuality` but keeps `CompArt` for `UMW_Storied` alone.
- **Vanilla melee weapons have no `Name=`, so they can't be `ParentName` targets.**
  `1.6/Patches/AddNameToBaseMeleeWeapons.xml` adds one per base weapon we mirror (add-if-missing,
  DLC-gated by node existence). Add an Operation there for each new base weapon; the unique then
  overrides only its deltas. **A base from a third-party mod also needs that mod in `About.xml`
  `loadAfter`:** inheritance only accepts a parent owned by a mod at or before the child's load
  order, and the Name-add doesn't change ownership. Misordered, the unique logs "Could not find
  parent node" and loads parentless; the smoke test can't catch it because it pins its own order.
- **Back-reference the base weapon via `<descriptionHyperlinks>`.** The `UMW_` prefix means the base
  def isn't derivable from the unique's defName.
- **Reward pools are split in def space, not by Harmony.** Odyssey's `ThingSetMaker_UniqueWeapon`
  makes things with no stuff, which errors on our stuffable weapons and dilutes the ranged pool.
  Every `*_Unique` weapon carries the `UMW_UniqueMelee` tag (a published contract, root
  `CLAUDE.md`); `1.6/Patches/RepointUniqueWeaponPool.xml` repoints the two class-based vanilla
  consumers onto `ThingSetMaker_UMWUnique`, and our own pool filters on the tag.

## Other def traps

- **An AoE ability's radius lives in two places and must agree, at `X.9`.** The gizmo-hover
  preview reads `verbProperties.range`, never a comp field, so a mismatch draws a ring that lies;
  and `X.0` or the wrong `X.9` draw a filled square or a sparse diamond. Both ability defs carry the
  worked arithmetic; `AbilityDefs/Earthshake.xml` has the fullest version.
- **Nullified *situational* thoughts still render as a grey "0" row** (only memories are dropped at
  `MoodOffset()==0`). A trait-flipped mood must therefore be **one multi-stage def with a
  stage-routing worker**, not a penalty def plus a `requiredTraits` buff def. Every other
  personality exemption stays declarative (`nullifyingTraits`/`nullifyingGenes`, with `MayRequire`
  on mod-specific entries). See `Source/1.6/Traits/ThoughtWorker_BloodStainedWeapon.cs`.
