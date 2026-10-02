using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace UniqueMeleeWeapons.Patches;

// Mixes the weapon's material into unique-name generation ("The Golden Reaper",
// "Plasteel Fang") — unique names hide the stuff an ordinary weapon's label shows.
//
// Grammar generation is purely symbolic: neither GenerateName nor GrammarRequest
// carries a Thing (verified by decompile), so the stuff can't ride along inside the
// request — it has to reach us out-of-band. Two routes do that, and this prefix
// handles both (gated on rootKeyword "r_weapon_name", so unrelated name requests that
// resolve in the same window — e.g. a CompArt title — are untouched):
//
//   1. Our own generation. CompUniqueWeapon.PostPostMake builds the request
//      (weapon_type / color / trait_adjective rules + Odyssey's NamerUniqueWeapon
//      pack) and resolves it here; UniqueMeleeWeapon.PostMake has parked the
//      stuff in StuffBeingNamed (only ever set while one of OUR weapons is being
//      made — vanilla ranged uniques never set it). The request has no
//      stuff_adjective rule yet, so we add both the rule and the rulepack.
//
//   2. A companion tool that re-rolls the name outside generation (e.g. Unique
//      Weapons Unbound's customization dialog). It can't reach StuffBeingNamed, so
//      by convention it publishes the material itself as a "stuff_adjective" rule on
//      the request it builds. We detect that rule and add only the rulepack that
//      consumes it. This is a deliberate, dependency-free integration contract: the
//      companion emits a well-known grammar symbol, we supply the grammar — neither
//      mod references the other's code, and the rule is inert if we're absent.
//
// The stuff_adjective value (route 1) prefers the stuff's stuffAdjective ("wooden",
// "golden") and falls back to its label ("plasteel", "jade" — English stuff labels
// read fine adjectivally); UMW_NamerStuffAdjectives supplies the patterns that weave
// [stuff_adjective] into NamerUniqueWeapon's grammar. The comp/dialog request is
// sealed around, but its rule/include lists are plain shared references, so a prefix
// can extend it without touching vanilla or companion code.
[HarmonyPatch(typeof(NameGenerator), nameof(NameGenerator.GenerateName),
    typeof(GrammarRequest), typeof(Predicate<string>), typeof(bool), typeof(string), typeof(string))]
public static class NameGenerator_StuffAdjective_Patch
{
    private const string WeaponNameRoot = "r_weapon_name";
    private const string StuffAdjectiveSymbol = "stuff_adjective";

    public static void Prefix(ref GrammarRequest request, string rootKeyword)
    {
        if (rootKeyword != WeaponNameRoot)
        {
            return;
        }

        // Route 2 (companion supplied the adjective) vs. route 1 (we supply it from
        // the parked stuff). If neither is present there is nothing to weave in.
        if (!request.HasRule(StuffAdjectiveSymbol))
        {
            ThingDef stuff = UniqueMeleeWeapon.StuffBeingNamed;
            if (stuff == null)
            {
                return;
            }
            string adjective = stuff.stuffProps?.stuffAdjective;
            if (adjective.NullOrEmpty())
            {
                adjective = stuff.label;
            }
            request.Rules.Add(new Rule_String(StuffAdjectiveSymbol, adjective));
        }

        // Idempotent: a fully self-sufficient caller might include the pack itself,
        // and double-including would double the material patterns' weight.
        if (!request.Includes.Contains(UMW_DefOf.UMW_NamerStuffAdjectives))
        {
            request.Includes.Add(UMW_DefOf.UMW_NamerStuffAdjectives);
        }
    }
}
