#!/usr/bin/env python3
# UniqueMeleeWeapons' config shim over the shared translation checker
# (l10n/checker/check_translations.py — the rimworld-l10n submodule). The
# engine holds all logic; this file holds only this repo's config and the
# rationale behind it. Usage is unchanged:
#   python3 Scripts/check-translations.py [--strict] [--root PATH]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "checker"))
import check_translations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

# No [TranslationCanChangeCount]-style matching-token fields in this repo.
engine.PARITY_EXEMPT_FIELDS = set()

# RATIONALE: Odyssey is a hard dependency (without it the mod's defs do not
# load at all); Royalty gates the 1.6/Mods/Royalty compat load root (the
# unique Axe/Warhammer and their Royalty-tech traits/colours), whose defs
# would drop out of a dump made without it, turning its shipped translations
# illegal.
engine.REQUIRED_DLCS = {"Royalty", "Odyssey"}

# Empty here today; ArchotechAndroidHardware's shim carries the first real
# entry (VREA's AndroidGeneDef -> GeneDef).
engine.DEF_TYPE_ALIASES = {}

# This mod ships a real Keyed surface, so a missing Languages/ tree is a
# hard config error, not a legal state.
engine.ALLOW_NO_KEYED_SURFACE = False

# The localized Steam Workshop title lives in this Keyed key (the
# settings-window header); the checker enforces the title-coupling rule
# against each .steamworkshop/Description/<Language>.txt title line.
engine.WORKSHOP_TITLE_KEY = "UMW_SettingsCategory"

raise SystemExit(engine.main())
