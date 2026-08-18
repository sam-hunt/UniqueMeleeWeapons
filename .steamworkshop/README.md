# .steamworkshop

Publishing metadata for the mod's Steam Workshop page. Nothing in this folder
ships with the mod (the StageMod manifest never matches it) or is loaded by
RimWorld. A `Media/` folder for Workshop images can live here later.

## Description/

One file per language, named after the RimWorld language folders in
`1.6/Languages/`. English is the source of truth; the others are
machine-assisted first passes pending native review. Format:

- Line 1: the Workshop title for that language
- Line 2: blank
- Rest: the BBCode description

Title convention: just as the English title leans on Odyssey's own vocabulary
("Unique" weapons) plus the ordinary word for melee combat, so players
searching either vanilla term find the mod, every localized title must
contain that language's vanilla-localized "unique weapon" term paired with
its ordinary word for melee (arme de mêlée unique, 근접 무기, 近战武器, ...).
Titles are fully localized with no English brand appended: Workshop search
is language-agnostic (any language's title matches regardless of UI
language, verified 2026-08-12) and the preview thumbnail already carries the
English name. Each title must equal that language's `UMW_SettingsCategory`
Keyed value so the in-game settings header matches the Workshop page (see the
CLAUDE.md localization note).

Steam has no API for per-language Workshop text, so updated files are pasted
manually into the Workshop page's edit UI (note Steam's own language names
differ: schinese, koreana, brazilian, latam, ...). The `release` skill diffs
`English.txt` against the last release tag and refreshes the translations
whenever it changed.

All nine language files exist (initial pass 2026-08-18); the non-English ones
are machine-assisted first passes pending native review, and still need their
first manual paste into the Workshop page's per-language edit UI.
