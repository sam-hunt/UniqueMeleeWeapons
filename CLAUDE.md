# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Unique Melee Weapons** is a RimWorld 1.6 mod that adds a collection of unique,
individually-designed melee weapons. Requires Harmony and the Odyssey DLC.

> The mod is freshly scaffolded — this file documents the build/deploy system and
> conventions. Fill in the **Content** and **Architecture** sections as weapons and
> any custom C# behaviour are added.

**Key Technologies:** C# (.NET Framework 4.7.2), Harmony library, RimWorld modding API, XML definitions

## Build Commands

```bash
# Build the mod (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build UniqueMeleeWeapons.sln -c Release

# Build only the main project (also triggers the deploy)
dotnet build Source/1.6/UniqueMeleeWeapons.csproj

# Stage the mod into an arbitrary folder (used by CI; same manifest as the local deploy)
dotnet build Source/1.6/UniqueMeleeWeapons.csproj -c Release \
  -t:StageMod -p:StageDir=/path/to/output/UniqueMeleeWeapons
```

The build system auto-detects the RimWorld installation path on Windows/Linux/Mac (including WSL targeting a Windows install). For CI builds without RimWorld installed, it falls back to the `Krafs.Rimworld.Ref` NuGet package.

### Deployment

The repo lives in `~/dev/UniqueMeleeWeapons`, separate from the RimWorld Mods folder. Every local build redeploys automatically and atomically — there is no separate clean step to remember.

- **Single source of truth:** the file manifest lives in **one** place — the `_ModFiles` ItemGroup in the `StageMod` target of `Source/1.6/UniqueMeleeWeapons.csproj`. It's generic over the well-known RimWorld content folders — `About`, `Assemblies`, `Defs`, `Patches`, `Textures`, `Sounds`, `Languages`, plus root `LoadFolders.xml` — each matched at the mod root **and** under any version/`Common` folder (the `$(RepoRoot)/*/<Folder>` patterns), so a new version folder (a future `1.7/`) needs no change. Source layout is mirrored verbatim into `$(StageDir)` (via per-item `MakeRelative` metadata — note an inline `MakeRelative` inside an item transform evaluates only once, not per item). Dropping in e.g. a `Sounds/` folder deploys automatically; only a brand-new _file type_ needs a new line here.
- **Lean by extension whitelist:** only the formats RimWorld loads at runtime ship — `.dll` (no `.pdb`); `.xml` (Defs/Patches/Languages/About); `.png`/`.jpg`/`.jpeg` (Textures); `.wav`/`.mp3`/`.ogg` (Sounds); `.txt` (Languages/About, e.g. `PublishedFileId.txt`). `Verse.ModContentLoader` _lists_ `.psd`/`.dds` among acceptable texture extensions, but its runtime decode path (`Texture2D.LoadImage`) only handles PNG/JPEG — a shipped `.psd`/`.dds` would fail to render and just bloat the download, so they're **excluded**. OS junk, editor backups (incl. `.kra` art sources), dev notes, and `Source/` can never leak into a release.
- **Self-cleaning:** `StageMod` wipes `$(StageDir)` and recopies from source, so renamed/deleted Defs/Patches/Textures never linger. The post-build `DeployToModFolder` target calls `StageMod` with `StageDir = $RIMWORLD_PATH/Mods/UniqueMeleeWeapons` (only when a local RimWorld install is detected).
- **CI reuses the same target:** `.github/workflows/release.yml` invokes `StageMod` with `-p:StageDir=<release dir>` rather than its own `cp` list, so the release zip can never drift from the local deploy. The workflow triggers on `v*.*.*` tags.
- **Stop hook (`.claude/hooks/sync-mod.sh`):** runs after each conversation turn. It rebuilds+redeploys _only when mod-relevant source/content changed since the last deploy_ (skips doc/discussion turns), logs to `$TMPDIR/umw-build.log`, and prints a warning on build failure instead of silently leaving a stale DLL in the game folder. Change-detection uses a stamp at `Source/1.6/obj/.umw-deploy-stamp`. Both the hook config (`.claude/settings.local.json`) and the helper script live under `.claude/`, which is **gitignored** — so the whole post-turn sync is local-only and untracked. If it's ever promoted to a committed config, move the helper somewhere version-controlled. The script finds the repo root via `git rev-parse`, so it works regardless of where it's relocated.

**WSL Setup:** Requires `RIMWORLD_PATH` env var in `~/.bashrc` pointing to the Windows RimWorld install (e.g., `/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`).

## Architecture

### Directory Structure

```
About/              # Mod metadata (About.xml, ModIcon.png, Preview.png)
1.6/
├── Assemblies/         # Compiled DLL (build output, gitignored)
├── Defs/
│   ├── ThingDefs/          # Weapon ThingDefs
│   ├── WeaponCategoryDefs/ # Trait-system categories (which traits a weapon may roll)
│   └── WeaponTraitDefs/    # Unique-weapon traits (stat mods, forced accent colours)
│                           #   (one def per file in every Defs .xml — see Naming)
└── Patches/        # XPath patches (if/when needed)
Textures/
└── Things/Item/Equipment/WeaponMelee/UniqueWeapons/<Weapon>/   # per-weapon variant folder
Source/1.6/
├── Core/           # Mod subclass (Harmony setup + settings window) and ModSettings
├── Things/         # Custom thingClass subclasses (e.g. UniqueMeleeWeapon)
├── Graphics/       # Graphic_RandomComplex (random variant, preserves colour two)
├── Traits/         # Melee trait on-hit effects (MeleeTraitEffectExtension + MeleeOnHitEffect)
├── Patches/        # Harmony patches (e.g. Verb_MeleeAttackDamage on-hit-trait postfix)
└── Properties/     # AssemblyInfo
```

### Def Naming Convention

All defs use the `UMW_` prefix (Unique Melee Weapons). For a unique variant of a vanilla
weapon, mirror Odyssey's official convention of suffixing the base weapon name with `_Unique`
— e.g. the unique longsword (vanilla `MeleeWeapon_LongSword`) is `UMW_LongSword_Unique`.

**File layout:** one def per file (for discoverability) in *every* `Defs/` `.xml` — current and
any future subfolder — named after the def with the redundant `UMW_` prefix stripped — e.g.
`UMW_Serrated` → `WeaponTraitDefs/Bladed/Serrated.xml`,
`UMW_LongSword_Unique` → `ThingDefs/LongSword_Unique.xml` (the `_Unique` suffix stays; it's
meaningful). Trait files are further grouped into per-category subfolders under `WeaponTraitDefs/`
— one folder per `WeaponCategoryDef`, named after the category with the `UMW_` prefix stripped
(`Melee/`, `Bladed/`, `Pointed/`, `Blunt/`, `Heavy/`) — so a trait sits beside its siblings and
its `<weaponCategory>` is obvious from its path. RimWorld loads `Defs/` recursively and the
`StageMod` manifest is generic over the folder (its globs use `**`), so adding per-def files or
new subfolders needs no build change. Keep each category's shared theme /
on-hit-effect rationale on its `WeaponCategoryDef` file, and the broad system overview in this
doc — don't re-duplicate it across the individual trait files.

**Texture naming** also follows Odyssey: variants live in a per-weapon folder
(`Textures/Things/Item/Equipment/WeaponMelee/UniqueWeapons/<Weapon>/`) and are named
`Unique<Weapon><Letter>` with a matching `_m` colour mask — e.g. `UniqueLongSwordA.png` +
`UniqueLongSwordA_m.png`. The def's `texPath` points at the **folder** and uses
`graphicClass>UniqueMeleeWeapons.Graphic_RandomComplex` — our subclass of `Graphic_Random` (random
variant per spawn; `_m` masks auto-pair per variant). The custom class is required because vanilla
`Graphic_Random` clamps colour two to white (see the double-masking note below).

### Conventions

- **C# entry point:** `UniqueMeleeWeaponsMod` (in `Source/1.6/Core/`) applies all Harmony patches via `PatchAll()` at startup, so any `[HarmonyPatch]` class under the assembly is picked up automatically — no manual registration.
- **Namespace:** root namespace is `UniqueMeleeWeapons`. Use a `*Patches` suffix for patch namespaces (e.g. `UniqueMeleeWeapons.Patches`) to avoid RimWorld type-name conflicts.
- **Settings:** persisted via `UniqueMeleeWeaponsSettings` (ModSettings). Currently empty — add fields + `Scribe_Values.Look` calls and surface them in `DoWindowContents`.

### Architecture notes

One short entry per non-obvious decision (custom `CompProperties`/`Verb` subclasses, weapon
comps, Harmony patch rationale, Odyssey-specific hooks, etc.), mirroring the build-system notes.

- **Stuffable unique weapons = double-masked recolour (`Source/1.6/Things/UniqueMeleeWeapon.cs`).**
  This is the load-bearing trick of the whole mod. Odyssey's ranged uniques are *not* stuffable —
  they bake the body colour into the diffuse and use the mask only for a single forced accent
  colour. Our melee weapons *are* stuffable, so they must show **both** the material tint *and* a
  unique accent at once. We do this with one `CutoutComplex` texture:
  - **Mask red → colour one (`DrawColor`)** = the unique accent. Supplied for free by vanilla
    `CompUniqueWeapon.ForceColor()` (a random weapon `ColorDef`, or a trait's `forcedColor` like
    `UniqueWeapon_Gold`). No code needed for this half.
  - **Mask green → colour two (`DrawColorTwo`)** = the stuff/material tint. Vanilla leaves
    `DrawColorTwo` at the def's white `colorTwo`; our `thingClass` (`UniqueMeleeWeapon : ThingWithComps`)
    overrides it to `def.GetColorForStuff(Stuff)`. `GraphicData.GraphicColoredFor` bakes both colours
    into the thing's `Graphic`, so colour two also reaches the equipped/held view. **Gotcha:** vanilla
    `Graphic_Random.GetColoredVersion` clamps colour two to white (logs *"Cannot use
    Graphic_Random.GetColoredVersion with a non-white colorTwo"*), which renders the green-masked body
    plain white. We use `Graphic_RandomComplex` (`Source/1.6/Graphics/`), a one-method subclass that
    forwards colour two; `Graphic_Single` itself already supports it.
  - **Mask black → "not painted"** = the raw diffuse, *untinted*. A black blade would therefore
    ignore its material entirely — so masks for stuffable weapons must paint the body **green**, not
    leave it black the way Odyssey's non-stuffable masks do. Keep the weapon silhouette all
    red/green, no black. Keep the diffuse light/neutral so the multiply yields a clean tint.
- **Unique-weapon traits/categories (`Defs/WeaponTraitDefs`, `Defs/WeaponCategoryDefs`).**
  `CompUniqueWeapon` rolls 1–3 `WeaponTraitDef`s filtered to the categories listed on the weapon's
  `CompProperties_UniqueWeapon`. Odyssey's stock traits are all tagged `Ranged`/`Gun`/etc., so they
  never roll on a melee-categorised weapon — melee needs its own traits (we reuse Odyssey's
  category-agnostic `ColorDef`s like `UniqueWeapon_Gold` for inlays). Generation **throws** unless at
  least one eligible trait has `canGenerateAlone=true` (the first pick) *and* the rolled set yields a
  `traitAdjective` — so every weapon's category set must include an alone-able trait with adjectives.
  Ours is guaranteed by **`UMW_Melee`'s universal `UMW_Lightweight`** plus each mechanism category's
  alone-able traits. The locked taxonomy is **5 categories**: `UMW_Melee` (universal — six generic
  cosmetic/value/weight traits ported from Odyssey: Ornamental, Ugly, Lightweight, Cumbersome,
  Gold/Jade inlay), the mechanism categories `UMW_Bladed`/`UMW_Pointed`/`UMW_Blunt` (each gates a
  damage-family on-hit effect — see next note), and the handling category `UMW_Heavy` (slow-swing
  stat archetypes). Within a mechanism category, traits share an `exclusionTags` token (`Edge`,
  `Point`, `Head`, `SwingProfile`) so a weapon gets at most one effect per family. **Discipline:**
  every category has ≥2 traits behind it — `UMW_Reach` was considered for the spear but dropped
  (melee has no reach mechanic, so its traits would be flavor-only orphans). The six `UMW_Melee`
  ports are **intentional self-contained copies, not XML inheritance from Odyssey's defs** — three
  (Ornamental/Lightweight/Cumbersome) re-pointed onto melee stats because Odyssey's `RangedWeapon_*`
  mods are inert on melee, three (Ugly/Gold/Jade inlay) verbatim-equivalent. `WeaponCategoryDefs/Melee.xml`
  documents why inheritance is avoided; each ported trait file notes its deltas inline.
- **Melee trait on-hit effects (`Source/1.6/Traits/`, `Source/1.6/Patches/Verb_MeleeAttackDamage_OnHitTraits_Patch.cs`).**
  Vanilla's mechanically interesting `WeaponTraitDef` fields — `damageDefOverride`, `extraDamages`,
  `additionalStoppingPower`, `burstShot*`, `ignoresAccuracyMaluses` — are consumed **only** by
  `Projectile`/`Verb_LaunchProjectile`, so they silently do **nothing** on a melee weapon. Only
  `statOffsets`/`statFactors` (on the live melee stats `MeleeWeapon_DamageMultiplier` — which also
  drives armor pen, there is *no* separate melee AP stat — and `MeleeWeapon_CooldownMultiplier`, plus
  `Mass`/`Beauty`/`MarketValue`), `equippedStatOffsets` (buffs the *wielder's* pawn stats), and
  `forcedColor` reach melee. To give the mechanism categories real, distinct effects we add our own
  layer: a `MeleeTraitEffectExtension : DefModExtension` (a `List<MeleeOnHitEffect>` — `…_ExtraDamage`
  for Bladed bleed / Pointed armor-pierce, `…_Stun` for Blunt) attached to the trait def, fired by a
  Harmony **postfix on `Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget`** (gated on a landed,
  wounding hit by a weapon with a `CompUniqueWeapon`). Using a `DefModExtension` + postfix — rather
  than subclassing `WeaponTraitDef` — keeps the trait an ordinary def, so vanilla generation/naming/
  stats stay untouched. `…_ExtraDamage` calls `Thing.TakeDamage` directly (not the verb), so it can't
  re-trigger the postfix. This same extra-damage mechanism is the path for any future on-hit element
  (tox/incendiary just need the matching `DamageDef`).
- **Parenting: patch a `Name=` onto the base weapon, then inherit it.** RimWorld's `ParentName`
  resolves against a node's `Name=` attribute, not its `defName`. Vanilla *ranged* weapons expose
  `Name=` (so Odyssey does `ParentName="Gun_Revolver"`), but **no concrete vanilla *melee* weapon
  does** — `ParentName="MeleeWeapon_LongSword"` raises "Could not find parent node" and the def
  fails to load. Rather than re-implement the weapon (which drifts when vanilla rebalances), we
  add the missing attribute: `Patches/AddNameToBaseMeleeWeapons.xml` does a
  `PatchOperationAttributeAdd` of `Name=<defName>` to each base weapon we mirror. Patches run
  **before** inheritance resolution (`LoadedModManager`: `ApplyPatches` → `ParseAndProcessXML`), and
  `AttributeAdd` is **add-if-missing** (skips nodes that already have it), so it's safe to stack with
  other mods. The unique def then `ParentName`s the real weapon and overrides only the deltas
  (graphic, `thingClass`, comps, reward tags, nulled `recipeMaker`) — inheriting tools/stats/stuff
  automatically. Add one patch Operation per new base weapon. **DLC-gated bases:** the Axe and
  Warhammer are Royalty weapons but the mod requires only Odyssey, so their two name patches are
  wrapped in `PatchOperationConditional` testing the base node's existence (`Defs/ThingDef[defName=…]`)
  — this tracks the actual weapon rather than a mod name, skipping cleanly with no "could not find
  node" error when Royalty is absent. Their unique defs carry `MayRequire="Ludeon.RimWorld.Royalty"`
  to match.
- **Back-reference the base via `descriptionHyperlinks`.** `<descriptionHyperlinks><ThingDef>MeleeWeapon_LongSword</ThingDef></descriptionHyperlinks>`
  — vanilla/Odyssey convention, and the link "Unique Weapons Unbound" reads to let the weapon revert
  to its base. The `_Unique` suffix alone isn't enough: our prefixed `UMW_LongSword_Unique` minus the
  suffix (`UMW_LongSword`) isn't a real def, so the explicit hyperlink is required.

## Debugging

1. **Enable RimWorld Dev Mode:** Settings > Dev Mode > Logging
2. **Log locations:**
   - **Windows:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   - **WSL:** `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
3. **Logging:** Use `Log.Message("[Unique Melee Weapons] ...")` for mod-specific logs
4. **Inspect RimWorld API:** `ilspycmd "/mnt/c/.../RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t "Namespace.ClassName"`
