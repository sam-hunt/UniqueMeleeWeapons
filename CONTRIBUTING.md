# Contributing

Thanks for your interest in improving Unique Melee Weapons! Bug reports,
suggestions and pull requests are welcome.

## Localization

The mod targets the languages below, chosen by RimWorld's per-language
audience size. Contributions for any other language RimWorld supports are
welcome too.

| Language             | Status           | Credit  |
| -------------------- | ---------------- | ------- |
| English              | Source           | —       |
| Simplified Chinese   | Machine-assisted | Fable 5 |
| Russian              | Machine-assisted | Fable 5 |
| Korean               | Machine-assisted | Opus 5  |
| German               | Machine-assisted | Opus 5  |
| Spanish              | Machine-assisted | Opus 5  |
| French               | Machine-assisted | Opus 5  |
| Brazilian Portuguese | Machine-assisted | Opus 5  |
| Japanese             | Machine-assisted | Opus 5  |

Spanish here means Castilian (RimWorld's `Spanish` language folder). RimWorld also
ships a separate Latin American Spanish (`SpanishLatin`); a translation for it is
welcome as its own folder rather than as edits to this one.

Brazilian Portuguese likewise means RimWorld's `PortugueseBrazilian` folder. European
Portuguese (`Portuguese`) is a separate language folder in RimWorld, so a translation
for it is welcome in its own right rather than as edits to this one.

Statuses: **Source** (the authoritative English strings), **Machine-assisted**
(generated with terminology grounded against the official RimWorld
localization; awaiting native review), **Native** (written or reviewed by a
native speaker), **Planned** (not started — contributions welcome).

### Contributing a translation

- Files live under `1.6/Languages/<Language>/` (`Keyed/` and `DefInjected/`),
  mirroring the structure of `1.6/Languages/English/`.
- Every translated entry carries the current English source in a comment
  directly above it, e.g. `<!-- EN: Reset to defaults -->` — this is how stale
  translations are detected when the English changes.
- Placeholders (`{0}`, `{1}`, ...) must match the English exactly.
- Exception: entries for content gated on Royalty live under
  `1.6/Mods/Royalty/Languages/<Language>/...` (a LoadFolders-gated root that
  only loads when Royalty is active — MayRequire is ignored on DefInjected
  entries, so the folder is the gate). Translate them there, mirroring that
  root's own structure, never in the main `1.6` tree — the checker enforces
  this placement.
- Vanilla def types use bare DefInjected folder names (`ThingDef`,
  `AbilityDef`); any of this mod's own def classes would use
  namespace-qualified names (`UniqueMeleeWeapons.<DefClass>`).
- Formatting: UTF-8 without BOM, LF line endings, 2-space indent.
- Validate before opening a PR:

  ```bash
  python3 Scripts/check-translations.py --strict
  ```

  It checks key coverage, placeholders, DefInjected paths and load-root
  placement, staleness, and file hygiene.

- Improving a machine-assisted language? Corrections from native speakers
  are gladly merged, no matter how small.
