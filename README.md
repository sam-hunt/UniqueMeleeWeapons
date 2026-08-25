# Unique Melee Weapons

> A RimWorld mod adding individually-designed unique melee weapons

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-blue.svg)](https://rimworldgame.com/)
[![Odyssey DLC](https://img.shields.io/badge/DLC-Odyssey-red.svg)](https://store.steampowered.com/app/2380740/RimWorld__Odyssey/)
[![Subscribers](https://img.shields.io/steam/subscriptions/3776637622?logo=steam&label=subscribers)](https://steamcommunity.com/sharedfiles/filedetails/?id=3776637622)
[![Downloads](https://img.shields.io/steam/downloads/3776637622?logo=steam&label=downloads)](https://steamcommunity.com/sharedfiles/filedetails/?id=3776637622)
[![Favorites](https://img.shields.io/steam/favorites/3776637622?logo=steam&label=favorites)](https://steamcommunity.com/sharedfiles/filedetails/?id=3776637622)
[![Views](https://img.shields.io/steam/views/3776637622?logo=steam&label=views)](https://steamcommunity.com/sharedfiles/filedetails/?id=3776637622)

![Preview](About/Preview.png)

## About

RimWorld's Odyssey DLC introduced unique weapons — one-off variants with rolled traits, colours and names, found as quest rewards and in ancient caches. But every one of them is a gun. Melee got nothing.

This mod fills the gap. Eight unique melee weapons, each stuffable, each rolling its own traits, art variant, colours and name — so the silver longsword pulled from a warband chief is unlike any other's. Traits are built as physical features of the weapon (serrated edges, flanged heads, barbed points, quillons) rather than unexplained blessings, and each is gated to the weapons that could plausibly carry it.

## Features

### Eight Unique Melee Weapons

Unique variants of the vanilla knife, gladius, longsword, spear, mace and breach axe, plus the axe and warhammer with Royalty — mirroring Odyssey's `_Unique` convention and inheriting their base weapon's tools and stats.

- **Stuffable**, unlike Odyssey's ranged uniques: a unique weapon is made of a real material, and its material stat multipliers apply as normal
- **Five hand-drawn art variants per weapon**, double-masked so the unique accent colour and the material tint render independently on the same sprite
- **Material surfaced explicitly** on the inspect pane and woven into the generated name, since a unique name hides the stuff an ordinary label would show

### Twenty-Eight Weapon Traits

Traits roll from six categories, gated by what the weapon physically is — a spear can be barbed but not flanged, a mace can be bell-cast but not razored.

- **Bladed** — razored, serrated, monomolecular, plasma-cored
- **Pointed** — needle point, armor spike, barbed, envenomed, opiated
- **Blunt** — flanged, studded, bell-cast, dead-blow, zeus-headed, piledriver
- **Heavy** — head-weighted, counterweighted
- **Guarded** — quilloned (weapons with a real hand guard only)
- **Universal** — ornamental, ugly, lightweight, cumbersome, gold inlay, jade inlay, blood-stained, carbonized, enameled, storied

Exclusion tokens keep the rolls coherent: one edge treatment, one head, one finish, one active ability, one body colour per weapon.

### Effects That Actually Work On Melee

Most of `WeaponTraitDef`'s interesting fields are silently inert on melee weapons — they're read only by projectile or bladelink code. Everything here runs through a purpose-built extension layer so traits stay ordinary defs:

- **On-hit effects** — extra damage, stun, stagger, mental states, bleeding wounds, toxic and sedative buildup
- **Base-damage conversion** — reroute a hit's damage type (a serrated edge tears ragged wounds; an envenomed point delivers tox)
- **Per-tool damage and armor penetration**, independently of each other
- **Wielder-side hediffs** — a needle point's demanding grip, a quilloned guard's parry chance
- **Forced body colour** — carbonized black, enamel violet, monomolecular white, plasma orange, dried-blood red

### Two Active Abilities

- **Earthshake** (piledriver) — slam the weapon's mass into the ground and send a stunning shockwave out through the earth. It travels through walls but not across gaps, and it's indiscriminate: only the braced wielder is spared
- **Rallying cry** (storied) — raise the weapon and rally every non-hostile humanlike who can see you, halving their pain and stagger duration. Sleepers are woken instead, and resent it

### The Warband Quest

A low-tech sibling of Odyssey's ancient mercenaries quest, offered by traders, beggars and reading. A roving tribal war party has holed up in an abandoned settlement; their chief carries one of our uniques. Kill or capture the leader and the weapon is yours.

Built on a temporary hidden faction, reused vanilla tribal pawnkinds, and a ruined tribal settlement site — no permanent faction clutter, and the quest cleans up after itself.

### Reward Pool Separation

Melee uniques are reserved to their own reward pool rather than diluting Odyssey's ranged one, and Odyssey's own unique-weapon rolls are hardened along the way (its stock maker makes stuffable weapons with no stuff, which errors and forces steel). Tag-based sources — ancient crates, fishing, map-generation loot — pass a material already and include our weapons normally.

### Mod Compatibility

- **[Unique Weapons Unbound](https://github.com/sam-hunt/UniqueWeaponsUnbound)**: our weapons are fully customizable in UWU's dialog, and material is published through a dependency-free naming contract so custom names read correctly. Neither mod references the other's code
- **Royalty**: the unique axe and warhammer, the three ultratech traits (monomolecular, plasma-cored, zeus-headed), and the defs only they consume are all def-level `MayRequire`-gated, so they simply never load without Royalty

## Requirements

- **RimWorld 1.6** or later
- **Odyssey DLC** (required — depends on Odyssey's unique weapon system)
- **Harmony** (auto-download from Steam Workshop if you don't have it)

Royalty is optional; two weapons and three traits unlock with it.

## Installation

### Steam Workshop (Recommended)

Subscribe on the Steam Workshop and it will auto-download.

### Manual Installation

1. Download the latest release from the [Releases](https://github.com/sam-hunt/UniqueMeleeWeapons/releases) page
2. Extract the `UniqueMeleeWeapons` folder to your RimWorld `Mods` directory:
   - **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - **Mac**: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
   - **Linux**: `~/.steam/steam/steamapps/common/RimWorld/Mods/`
3. Enable the mod in RimWorld's mod menu
4. Restart RimWorld

## Compatibility

- **Safe to add** to existing saves.
- **Unsafe to remove** from saves.
- Not tested with Combat Extended.

## Contributing

Bug reports and feature requests welcome on [GitHub Issues](https://github.com/sam-hunt/UniqueMeleeWeapons/issues).
Please attach any relevant hugslib logs/stack traces/mod lists etc.

For development setup, see [CLAUDE.md](CLAUDE.md).

## Credits

**Author**: Sam Hunt ([@sam-hunt](https://github.com/sam-hunt))

Companion to [Unique Weapons Unbound](https://github.com/sam-hunt/UniqueWeaponsUnbound), which lets you customize the traits, colours and names of the weapons this mod adds.

**Built With**:

- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike - Runtime patching library
- RimWorld modding API, community examples

**Special Thanks**:

- [Art by IcyCheeseCake](https://steamcommunity.com/profiles/76561198094174176/myworkshopfiles/?appid=294100)
- [Ludeon Studios](https://ludeon.com) for RimWorld and modding API
- [The RimWorld modding community](https://steamcommunity.com/app/294100/workshop/) for inspiration
