# Localization (`1.6/Languages/`, `1.6/Mods/*/Languages/`, `Scripts/`)

English (Keyed files + def fields) is the source of truth; every other language derives from it
via the `/translate` skill (`.claude/skills/translate/SKILL.md`: this mod's translation surface,
grounding domain and glossary) and is validated deterministically by
`python3 Scripts/check-translations.py`, also a CI release gate.

- **Shared toolkit (`l10n/` submodule):** the family-wide process, per-language references,
  cross-language lessons and the checker/refresh/smoke engines live in the `rimworld-l10n` repo
  (canonical checkout `~/dev/rimworld-l10n`); the `Scripts/` files are thin per-repo config shims
  over its engines. If `l10n/` is empty, run `git submodule update --init`. Never edit `l10n/` in
  place here: mod-independent learnings go upstream in the canonical checkout, mod-specific ones
  (this mod's coined weapon-trait and name-grammar vocabulary) go in the skill's glossary. Upstream
  ships semver tags (a major means this repo's shim or flow needs an edit); the pin moves only at
  release, at the start of a translation pass, or when a new major lands, never per upstream commit.
- **The DefInjected expected set is the checked-in sidecar `Scripts/expected-injections.json`:** a
  dump of every injection point the *live* game sees for this mod, including vanilla-inherited
  fields and C#-default comp strings that never appear in this repo's XML, produced by
  `Scripts/refresh-translation-expectations.py` driving the L10nProbe dev mod (source `l10n/probe/`;
  build and deploy it only from the canonical checkout) through the game's own walker. The checker
  refuses to run against stale expectations, so new content forces a regen; the release skill
  regenerates every release, which also covers vanilla updates changing inherited text. **Never
  hand-edit the sidecar; regenerate it.** Its fields mirror the live game, not the XML, and the
  staleness check doesn't cover all of them (a hand-patched comp index, `086c554`, passed the
  checker and sat wrong until the next regen).
- **What the checker validates:** key parity, placeholders, DefInjected legality, load-root
  placement (an entry must live in the same load root as the def it targets), cross-root file-path
  collisions (`1.6/Mods/CLAUDE.md`), staleness, file hygiene, and the Workshop title coupling.
- **Workshop title coupling:** each language's `UMW_SettingsCategory` Keyed value is the localized
  Steam Workshop title and must equal line 1 of `.steamworkshop/Description/<Language>.txt`; always
  change the two together (English keeps `Unique Melee Weapons` in both).
- **The public language roster lives in `CONTRIBUTING.md`** and must move in the same commit as
  any language change.
- **Keyed files split by purpose:** `UMW_UI.xml` settings strings, `UMW_Combat.xml` in-combat
  floating text, `UMW_Stats.xml` info-card trait-effect lines. No em dashes in any of them (root
  `CLAUDE.md`).
