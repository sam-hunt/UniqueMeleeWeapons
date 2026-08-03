using System.Collections.Generic;
using Verse;

namespace UniqueMeleeWeapons;

// A trait's effects as structured, unstyled lines — the one source of truth every UI renders from.
//
// PUBLISHED CONTRACT. Unlike our other DefModExtensions this one is not authored in XML: it is
// attached at startup by TraitEffectSummary, and it exists so that consumers OUTSIDE this assembly
// can render a melee trait's effects without referencing our code. Vanilla's WeaponTraitDef exposes
// no structured slot for this (WeaponTraitWorker has no display hook, Def has no effect list), so
// modExtensions is the idiomatic per-def carrier and duck-typing is the dependency-free reader.
//
// THE TYPE NAME AND THE FIELD NAME ARE THE CONTRACT — a consumer finds us by scanning
// def.modExtensions for a type named "TraitEffectLinesExtension" and reflecting the public
// "lines" field. Unique Weapons Unbound's trait-picker tooltip does exactly that. Renaming either
// silently empties that tooltip, the same way renaming the stuff_adjective grammar symbol would
// silently drop material adjectives from generated names. Don't.
//
// Lines are UNSTYLED on purpose: no bullet, no indent, no trailing punctuation. Vanilla's info card
// wants " - Cut damage x90%"; UWU's tooltip wants "  Cut damage x90%" under its own "Effects:"
// heading. Formatting belongs to whoever is drawing.
public class TraitEffectLinesExtension : DefModExtension
{
    // One short line per effect, already localized into the active language, in display order.
    // [NoTranslate] because the lines are DERIVED at startup from Keyed strings (UMW_Stats.xml),
    // which are what translators actually translate: without it, DefInjectionUtility's walker
    // (the in-game report, and the L10nProbe expectations dump built on it) advertises
    // modExtensions.N.lines as injection points that would fight the derivation. The attribute
    // does not touch the name or type, so the duck-typing contract above is unaffected.
    [NoTranslate]
    public List<string> lines = new List<string>();
}
