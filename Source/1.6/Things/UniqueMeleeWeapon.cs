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
//
// A stuff-less unique (VFE Pirates' warcasket weapons) has no material tint, so colour two falls
// through to the def's graphicData.colorTwo: that field is the per-def body placeholder, and a
// forced body-colour trait still replaces it.
public class UniqueMeleeWeapon : ThingWithComps
{
    // The stuff of the weapon currently being made (trait roll + naming inside
    // CompUniqueWeapon.PostPostMake), exposed for NameGenerator_StuffAdjective_Patch to
    // inject material adjectives into the name grammar. Thing generation is
    // single-threaded, so one static slot (cleared in finally) is safe.
    public static ThingDef StuffBeingNamed { get; private set; }

    // Generation-time work that must bracket or follow CompUniqueWeapon's trait roll and naming.
    // Ordering (decompile-verified 1.6): ThingMaker.MakeThing calls SetStuffDirect, PostMake, then
    // PostPostMake; and ThingWithComps.PostMake creates the comps and runs EVERY COMP'S PostPostMake
    // inside itself, so by the time base.PostMake() returns here the traits, quality and name are
    // all final. Nothing of ours belongs in Thing.PostPostMake: through 1.3.0 this work sat there and
    // ran one step too late — the material slot was still empty while the name was generated and
    // the inscription call found the title already written.
    // Hit points are NOT handled here: the Carbonized clamp lives on CompUniqueWeapon.AddTrait
    // (Patches/CompUniqueWeapon_TraitStatCache_Patch.cs), which generation also funnels through, so
    // it covers a trait added to an in-world weapon as well; a copy here would be redundant. That
    // patch also clears the per-thing stat caches a trait invalidates, which is why MaxHitPoints
    // reads correctly at all: see its header.
    public override void PostMake()
    {
        StuffBeingNamed = Stuff;
        try
        {
            base.PostMake();
        }
        finally
        {
            StuffBeingNamed = null;
        }

        // A quality-less def (the VFEP warcasket pair, mirroring their base) never reaches
        // CompQuality.SetQuality, the only generation-time caller of CompArt.InitializeArt
        // (decompile-verified 1.6). That is correct by default: an inscription is what Excellent-plus
        // quality earns (CompProperties_Art.minQualityForArtistic; the quality-bearing eight always
        // clear it because CompUniqueWeapon's Super roll never lands below Masterwork), and a weapon
        // with no quality tier has not earned it. So the pair is inscribed only when a rolled trait
        // guarantees art regardless of quality (ForcedArtExtension, i.e. UMW_Storied); that trait's
        // AddTrait patch cannot do it during generation, since its spawned-guard is what keeps colony
        // tales off outsider rewards. Otherwise CompArt is left as vanilla leaves a sub-Excellent
        // unique: title holding the unique name, no tale, so Active is false and no Art tab, inspect
        // line or description part appears.
        // The eight get theirs in CompUniqueWeapon.PostPostMake in this order: SetQuality ->
        // InitializeArt (the tale-less Outsider inscription, plus a generated art title) and THEN
        // the unique name written over the title. Mirror that here, after the fact:
        // InitializeArtInternal early-outs on an existing title, so clear first, initialise, then
        // put the unique name back. CanShowArt is unconditionally true without a CompQuality, so
        // the art is never nulled.
        if (GetComp<CompQuality>() == null
            && GetComp<CompArt>() is { } art
            && ForcedArtUtility.HasForcedArtTrait(this))
        {
            string uniqueName = art.Title;
            art.Clear();
            art.InitializeArt(ArtGenerationContext.Outsider);
            art.Title = uniqueName;
        }
    }

    // Two load-time repairs, both at PostLoadInit and only ever at load.
    //
    // NO STAT MAY BE EVALUATED IN HERE. A stat read (MaxHitPoints, MarketValue, any GetStatValue)
    // runs every mod's stat patches and StatParts, and a mod that lazily loads its settings on
    // first use (Mod.GetSettings -> LoadedModManager.ReadModSettings -> Scribe.loader.InitLoading)
    // then opens a second Scribe session inside the save load. InitLoading sees the mode is not
    // Inactive, logs "Called InitLoading() but current mode is PostLoadInit" and calls
    // Scribe.ForceStop, which clears the PostLoadIniter's set while DoAllPostLoadInits is still
    // foreach-ing it; the next item throws "Collection was modified" and the whole load aborts into
    // an empty, frozen map (decompile-verified 1.6; GitHub issue #2, 1.4.0 through 1.4.1, where the
    // hit-point repair below read MaxHitPoints directly). Which mod does the lazy read is beside the
    // point: the first stat evaluation of the session is whatever reaches it first, and nothing in
    // vanilla evaluates a stat during PostLoadInit. Stat-dependent work is queued instead
    // (ClampHitPointsAfterLoad).
    //
    // 1. Hit-point repair for saves made before 1.4.0: Carbonized never lowered a weapon's maximum in
    //    play (the per-thing MaxHitPoints cache held the trait-less value, see
    //    Patches/CompUniqueWeapon_TraitStatCache_Patch.cs), so those weapons were saved at the full
    //    trait-less total and would load reading e.g. "100 / 80" once the cache is right. The clamp
    //    runs from LongEventHandler.ExecuteWhenFinished: a save load is an asynchronous long event
    //    (Root_Play.Start), so the callback runs on the main thread after FinalizeLoading, with the
    //    Scribe inactive and a lazy settings read harmless. Not SpawnSetup, which an equipped or
    //    carried weapon never reaches; not a GameComponent, which would have to walk every thing.
    //    ExecuteWhenFinished runs its action immediately when no long event is current or the
    //    current one is not yet displayed, so the callback re-checks the Scribe mode and skips the
    //    repair rather than evaluate a stat mid-load: the repair is cosmetic and a weapon loaded on
    //    such a path simply keeps its saved total.
    //
    // 2. Interop guard: strip broken CompBladelinkWeapon grafts left by other mods.
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
    // non-functional and hold no player data. The graft happens at LoadingVars; in-session grafts
    // are left alone. Touches no stat, so it stays inline.
    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        if (def.useHitPoints)
        {
            LongEventHandler.ExecuteWhenFinished(ClampHitPointsAfterLoad);
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

    // The deferred half of the hit-point repair above. The first MaxHitPoints read of the session
    // happens here with the traits present, so the stat is already correct; a save's worth of
    // weapons queues one of these each, and each is a single cached stat read.
    private void ClampHitPointsAfterLoad()
    {
        if (Scribe.mode != LoadSaveMode.Inactive || Destroyed)
        {
            return;
        }
        if (HitPoints > MaxHitPoints)
        {
            HitPoints = MaxHitPoints;
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
