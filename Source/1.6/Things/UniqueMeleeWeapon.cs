using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Thing class for stuffable unique melee weapons. It combines two independent
// recolours in a single texture via the CutoutComplex shader:
//   - Colour one (DrawColor, mask red) — the unique accent
//     colour. Supplied unchanged by vanilla CompUniqueWeapon.ForceColor() (a randomly
//     picked weapon ColorDef, or a trait's forcedColor such as gold/jade).
//   - Colour two (DrawColorTwo, mask green) — the stuff/material
//     tint. Vanilla leaves this at the def's colorTwo (white); we redirect it to the stuff
//     colour so the blade tints like any ordinary smithed weapon.
// The mask drives the two colours per pixel, so one diffuse renders both the material tint and
// the unique accent at once — the trick the whole mod relies on. The mask must contain no
// black over the weapon silhouette: black means "not painted" and would show the raw,
// un-tinted diffuse (a black blade would ignore its material entirely).
public class UniqueMeleeWeapon : ThingWithComps
{
    // The stuff of the weapon currently running PostPostMake (trait roll + naming
    // inside CompUniqueWeapon), exposed for NameGenerator_StuffAdjective_Patch to
    // inject material adjectives into the name grammar. Thing generation is
    // single-threaded, so one static slot (cleared in finally) is safe.
    public static ThingDef StuffBeingNamed { get; private set; }

    public override void PostPostMake()
    {
        StuffBeingNamed = Stuff;
        try
        {
            base.PostPostMake();
        }
        finally
        {
            StuffBeingNamed = null;
        }

        // Traits roll inside the base call above, but ThingMaker.MakeThing runs PostMake — which
        // sets HitPoints from MaxHitPoints — BEFORE PostPostMake. So a trait that factors
        // MaxHitPoints (UMW_Carbonized's ×0.8) leaves the fresh weapon above its own new maximum,
        // reading e.g. "100 / 80". Re-clamp now that the trait list exists. No stat-cache concern:
        // MaxHitPoints is `cacheable`, not `immutable`, so its 10-tick window self-corrects.
        if (def.useHitPoints)
        {
            HitPoints = Mathf.Min(HitPoints, MaxHitPoints);
        }
    }

    // Interop guard: strip broken CompBladelinkWeapon grafts left by other mods.
    // More Persona Traits' Blade Whisperer save-restore (BladeWhisperer_ExposeData_Patch)
    // re-attaches `new CompBladelinkWeapon()` on load to any thing whose save data has a
    // node named "traits" — meaning every weapon with CompUniqueWeapon, whose trait list
    // scribes under that exact name — and never assigns the comp's props. A props-null
    // bladelink comp cannot function: vanilla CompBiocodable.Notify_Equipped dereferences
    // Props on every equip, so the weapon shows "Not yet bonded" and hard-crashes
    // JobDriver_Equip, permanently unequippable (player-reported 2026-08 against MPT).
    // The filter is exact: a comp built from any def always has props assigned by
    // InitializeComps, and a genuinely bonded graft loads biocoded=true in base.ExposeData
    // before this runs — so `props == null && !Biocoded` matches only comps that are both
    // non-functional and hold no player data. Runs at PostLoadInit (the graft happens at
    // LoadingVars), and only ever at load: in-session grafts are left alone.
    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }
        for (int i = AllComps.Count - 1; i >= 0; i--)
        {
            if (AllComps[i] is CompBladelinkWeapon bladelink
                && bladelink.props == null && !bladelink.Biocoded)
            {
                AllComps.RemoveAt(i);
                Log.WarningOnce(
                    "[Unique Melee Weapons] Removed a non-functional bladelink comp (no CompProperties, "
                    + "never bonded) that another mod attached to a unique melee weapon on load; it would "
                    + "have made the weapon unequippable. Known cause: More Persona Traits' Blade Whisperer "
                    + "save-restore misidentifying unique-weapon trait data as its own.",
                    "UMW_StrippedBladelinkGraft".GetHashCode());
            }
        }
    }

    // The unique name hides the material that an ordinary weapon's label shows
    // ("plasteel longsword" → "The Grim Reaper"), so surface it in the inspect
    // pane instead. Reuses the info card's own "Stuff" stat label (Stat_Stuff_Name)
    // — matching the term the stats card shows players — so it's already translated.
    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (Stuff != null)
        {
            string line = "Stat_Stuff_Name".Translate() + ": " + Stuff.label;
            text = text.NullOrEmpty() ? line : text + "\n" + line;
        }
        return text;
    }

    // Colour one (the red-masked accent) is left to the base implementation, which returns
    // the first comp's ForceColor() — i.e. CompUniqueWeapon's unique colour.
    // Colour two (the green-masked body) defaults to the stuff/material tint, but a trait may
    // override it via ForcedColorTwoExtension — the colour-two analogue of vanilla's
    // colour-one forcedColor. A forced body colour replaces the material tint
    // (there is no third mask channel); first such trait wins.
    public override Color DrawColorTwo
    {
        get
        {
            ColorDef forced = ForcedBodyColor();
            if (forced != null)
            {
                return forced.color;
            }
            if (Stuff != null)
            {
                return def.GetColorForStuff(Stuff);
            }
            return base.DrawColorTwo;
        }
    }

    // The body (colour two) override from the first equipped trait carrying a
    // ForcedColorTwoExtension, or null for the default material tint.
    private ColorDef ForcedBodyColor()
    {
        var comp = this.TryGetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return null;
        }
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            ColorDef color = traits[i].GetModExtension<ForcedColorTwoExtension>()?.color;
            if (color != null)
            {
                return color;
            }
        }
        return null;
    }
}
