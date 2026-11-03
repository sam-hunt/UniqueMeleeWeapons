using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// "Abilities" settings section: tuning for the two trait-granted abilities and the buff hediff one of
// them applies.
//
// Every field here is a def-field OVERRIDE applied by ApplyAbilityTuning, so the XML holds only the
// shipped default and the consts below are the real ones. Values are stored in the units the slider
// shows rather than in ticks, so the label needs no conversion and the snap grid is exact.
public partial class UniqueMeleeWeaponsSettings
{
    // Earthshake (Piledriver's ability).
    public const float EarthshakeCooldownHoursDefault = 12f;
    public float earthshakeCooldownHours = EarthshakeCooldownHoursDefault;

    public const float EarthshakeRadiusDefault = 3.9f;
    public float earthshakeRadius = EarthshakeRadiusDefault;

    // Rallying Cry (Storied's ability) and the UMW_Rallied buff it grants. Same
    // def-field-override pattern; the cooldown is in days because it is an heirloom
    // moment measured against Earthshake's hours.
    public const float RallyingCryCooldownDaysDefault = 3f;
    public float rallyingCryCooldownDays = RallyingCryCooldownDaysDefault;

    public const float RallyingCryRadiusDefault = 9.9f;
    public float rallyingCryRadius = RallyingCryRadiusDefault;

    public const float RalliedDurationHoursDefault = 2f;
    public float ralliedDurationHours = RalliedDurationHoursDefault;

    private void ExposeAbilitySettings()
    {
        Scribe_Values.Look(ref earthshakeCooldownHours, "earthshakeCooldownHours", EarthshakeCooldownHoursDefault);
        Scribe_Values.Look(ref earthshakeRadius, "earthshakeRadius", EarthshakeRadiusDefault);
        Scribe_Values.Look(ref rallyingCryCooldownDays, "rallyingCryCooldownDays", RallyingCryCooldownDaysDefault);
        Scribe_Values.Look(ref rallyingCryRadius, "rallyingCryRadius", RallyingCryRadiusDefault);
        Scribe_Values.Look(ref ralliedDurationHours, "ralliedDurationHours", RalliedDurationHoursDefault);
    }

    private void ResetAbilitySettings()
    {
        earthshakeCooldownHours = EarthshakeCooldownHoursDefault;
        earthshakeRadius = EarthshakeRadiusDefault;
        rallyingCryCooldownDays = RallyingCryCooldownDaysDefault;
        rallyingCryRadius = RallyingCryRadiusDefault;
        ralliedDurationHours = RalliedDurationHoursDefault;
    }

    // Writes the configured ability tuning onto the live defs. Called after defs load
    // (UMW_Startup) and whenever the settings window closes
    // (UniqueMeleeWeaponsMod.WriteSettings), same as ApplyWarbandQuestWeight.
    //
    // Every field written here is read fresh at use, so no restart is needed:
    // AbilityDef.cooldownTicksRange is sampled per cast (Ability.StartCooldown takes
    // .RandomInRange), and Verb.EffectiveRange resolves verbProps.AdjustedRange live
    // rather than caching (both decompile-verified, RimWorld 1.6).
    public void ApplyAbilityTuning()
    {
        SetCooldown(UMW_DefOf.UMW_Earthshake, earthshakeCooldownHours * GenDate.TicksPerHour);
        SetRadius(UMW_DefOf.UMW_Earthshake, earthshakeRadius);
        ScaleAreaFleck(UMW_DefOf.UMW_Earthshake, earthshakeRadius / EarthshakeRadiusDefault);

        SetCooldown(UMW_DefOf.UMW_RallyingCry, rallyingCryCooldownDays * GenDate.TicksPerDay);
        SetRadius(UMW_DefOf.UMW_RallyingCry, rallyingCryRadius);
        SetHediffDuration(UMW_DefOf.UMW_Rallied, ralliedDurationHours * GenDate.TicksPerHour);
    }

    private static void SetCooldown(AbilityDef def, float ticks)
    {
        if (def == null)
        {
            return;
        }
        int rounded = Mathf.RoundToInt(ticks);
        def.cooldownTicksRange = new IntRange(rounded, rounded);
    }

    // An ability's radius lives in two places that must agree, per 1.6/Defs/CLAUDE.md: verbProperties.range
    // drives the hover ring (VerbProperties.DrawRadiusRing reads verb.EffectiveRange and never a comp
    // field), and the effect comp's own radius drives what actually happens. Writing only one of them
    // would leave a preview that lies about the burst, so this owns both.
    private static void SetRadius(AbilityDef def, float radius)
    {
        if (def == null)
        {
            return;
        }
        if (def.verbProperties != null)
        {
            def.verbProperties.range = radius;
        }
        for (int i = 0; i < def.comps.Count; i++)
        {
            switch (def.comps[i])
            {
                case CompProperties_AbilityGroundShockwave shockwave:
                    shockwave.explosionRadius = radius;
                    break;

                case CompProperties_AbilityRallyAllies rally:
                    rally.radius = radius;
                    break;
            }
        }
    }

    // Resize an ability's fleck along with its radius, for a fleck that depicts the AREA of the effect:
    // otherwise a resized burst keeps a fixed-size shimmer sitting over it. Opt-in per ability rather
    // than folded into SetRadius, because a fleck is not necessarily an area indicator — Rallying Cry's
    // lightshaft is a beam over the wielder, and scaling THAT to a 12.9-cell rally would put a column of
    // light over one pawn. Only Earthshake's overhead ripple qualifies.
    //
    // Deliberately approximate: FleckDef.growthRate adds a component that this factor does not scale
    // (FleckStatic grows linearScale additively), so at large radii the ripple lands a little inside the
    // effect edge rather than a little outside it. It tracks, which is the point.
    private static void ScaleAreaFleck(AbilityDef def, float factor)
    {
        if (def == null)
        {
            return;
        }
        for (int i = 0; i < def.comps.Count; i++)
        {
            if (def.comps[i] is CompProperties_AbilityFleckOnTarget fleck)
            {
                fleck.scale = factor;
            }
        }
    }

    // Hediff duration is an IntRange on the disappear comp's props, read at HediffComp_Disappears
    // .CompPostMake via .RandomInRange — so a props write governs every hediff added from then on, with
    // no restart and without touching instances already running on a pawn.
    private static void SetHediffDuration(HediffDef def, float ticks)
    {
        if (def?.comps == null)
        {
            return;
        }
        int rounded = Mathf.RoundToInt(ticks);
        for (int i = 0; i < def.comps.Count; i++)
        {
            if (def.comps[i] is HediffCompProperties_Disappears disappears)
            {
                disappears.disappearsAfterTicks = new IntRange(rounded, rounded);
            }
        }
    }

    // One subsection per ability (SubsectionHeader: its gizmo icon, its name, its description as the
    // tooltip), the rows beneath it, so a row's label names only the property ("Cooldown: 12 hours");
    // the Rallied hediff's row keeps its own name, being a different def from the ability above it.
    private void DrawAbilitiesSection(Listing_Standard listing)
    {
        // Vanilla already localizes this exact word (Core Keyed <Abilities>), so reuse it
        // rather than shipping a duplicate key translators would have to do twice.
        SectionHeader(listing, "Abilities".Translate());

        AbilityDef earthshake = UMW_DefOf.UMW_Earthshake;
        SubsectionHeader(listing, earthshake.LabelCap, earthshake.uiIcon, HeaderTip(earthshake.LabelCap, earthshake.description));

        // The description names Odyssey's EMP pulser as the default's reference, so the DLC's name is
        // injected coloured (family rule 6, passing mentions included).
        earthshakeCooldownHours = SliderRow(
            listing, "UMW_CooldownHours", "UMW_EarthshakeCooldownDesc", null,
            earthshakeCooldownHours, EarthshakeCooldownHoursDefault,
            min: 0f, max: 24f, step: 1f, format: "0",
            descArg: ModIntegrations.Odyssey.ColoredName);

        earthshakeRadius = SliderRow(
            listing, "UMW_RadiusCells", "UMW_EarthshakeRadiusDesc", null,
            earthshakeRadius, EarthshakeRadiusDefault,
            min: 1.9f, max: 12.9f, step: 1f, format: "0.0");

        EndSubsection(listing);

        AbilityDef rallyingCry = UMW_DefOf.UMW_RallyingCry;
        SubsectionHeader(listing, rallyingCry.LabelCap, rallyingCry.uiIcon, HeaderTip(rallyingCry.LabelCap, rallyingCry.description));

        rallyingCryCooldownDays = SliderRow(
            listing, "UMW_CooldownDays", "UMW_RallyingCryCooldownDesc", null,
            rallyingCryCooldownDays, RallyingCryCooldownDaysDefault,
            min: 1f, max: 15f, step: 0.5f, format: "0.#");

        rallyingCryRadius = SliderRow(
            listing, "UMW_RadiusCells", "UMW_RallyingCryRadiusDesc", null,
            rallyingCryRadius, RallyingCryRadiusDefault,
            min: 1.9f, max: 12.9f, step: 1f, format: "0.0");

        // The hediff's own label as the subject, so the row tracks that def's translation.
        ralliedDurationHours = SliderRow(
            listing, "UMW_HediffDurationHours", "UMW_RalliedDurationDesc",
            UMW_DefOf.UMW_Rallied.LabelCap,
            ralliedDurationHours, RalliedDurationHoursDefault,
            min: 1f, max: 24f, step: 1f, format: "0");

        EndSubsection(listing);
        EndSection(listing);
    }
}
