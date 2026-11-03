using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UniqueMeleeWeapons;

// Mod settings: the window frame, the ExposeData / ResetToDefaults fan-out, and the shared row helpers.
// The class is split across Core/Settings/, one partial-class file per UI section, each owning its own
// fields, scribe entries, defaults, Apply* def-writes and draw method — so a setting is a one-file edit.
//
// Every settings-window string is localized through .Translate() against Keyed/UMW_UI.xml, except where
// vanilla already localizes the exact string (reuse its Keyed key or def label rather than shipping a
// second copy translators would do twice) and except names of game content, which are injected as def
// labels so they track their own defs' translations.
//
// To add a setting, in its section's partial file:
//  1. declare a public field (its default as the initializer, plus a const for that default where other
//     code needs to name it),
//  2. Scribe_Values.Look it in Expose*Settings, passing the same default so an unset value loads right,
//  3. restore it in Reset*Settings,
//  4. add its label/description keys to UMW_UI.xml,
//  5. draw it in Draw*Section.
// A whole new section is a new file there plus three one-line calls here (Expose / Reset / Draw).
//
// Gated rows follow the family's settings-window rules (RimworldModTemplate,
// Source/<version>/Settings/CLAUDE.md): a row whose mod or DLC is missing stays VISIBLE, disabled and
// greyed, shows a forced value (off for a missing mod, on for a row another setting holds on) with the
// stored field untouched, and carries the requirement as its label suffix with the mod's name coloured
// (ToggleRow below is the family model; ModIntegrations owns names, colours and activity). Rows that
// come from defs cannot do that for an absent def, so the ones an optional mod owns sit under a
// tri-state group header for that mod (DrawGroupHeader below) which is what stays visible and labelled
// while the mod is missing (Settings_Weapons.cs, Settings_Traits.cs). Two sanctioned exceptions hide
// instead, each saying why at its hide site: a row that only tunes a compat shim for the other mod's
// own content (Settings_Compat.cs), and a row derived from data no loaded def provides
// (Settings_Traders.cs, the outlander rows).
//
// Three more patterns to copy rather than re-derive: a collection-valued setting scribes with
// Scribe_Collections, which means it must re-create itself on load (Settings_Weapons.cs); a setting that
// OVERRIDES A DEF FIELD leaves the shipped default in the XML and writes the live value onto the def in
// an Apply* method called from both UMW_Startup.Run (every play-data load, since a reload replaces every
// def instance) and UniqueMeleeWeaponsMod.WriteSettings (window close), never from the draw code
// (Settings_Quests.cs, Settings_Abilities.cs); and a setting read at XML patch time instead
// (PatchOperation_UMWSetting) is restart-to-apply and its row must say so (Settings_Compat.cs).
public partial class UniqueMeleeWeaponsSettings : ModSettings
{
    // Trailing space each section leaves below itself, so a section that early-returns leaves no gap
    // behind rather than a double gap between its neighbours.
    private const float SectionGap = 18f;

    // Indent of the member rows under a group header.
    private const float GroupIndent = 24f;

    // Vanilla's checkbox edge, the size CheckboxLabeled draws its box at.
    private const float CheckboxSize = 24f;

    // Presentation state for the scroll view, deliberately not scribed.
    private Vector2 scrollPosition;
    private float contentHeight;

    // These two fan out to the sections in display order; serialization order is immaterial (Scribe is
    // keyed by name).
    public override void ExposeData()
    {
        base.ExposeData();
        ExposeWeaponSettings();
        ExposeTraitSettings();
        ExposeGenerationSettings();
        ExposeTraderSettings();
        ExposeAbilitySettings();
        ExposeQuestSettings();
        ExposeCompatSettings();
    }

    public void ResetToDefaults()
    {
        ResetWeaponSettings();
        ResetTraitSettings();
        ResetGenerationSettings();
        ResetTraderSettings();
        ResetAbilitySettings();
        ResetQuestSettings();
        ResetCompatSettings();
    }

    public void DoWindowContents(Rect inRect)
    {
        const float buttonHeight = 30f;
        const float buttonGap = 10f;
        const float buttonWidth = 200f;
        const float scrollBarWidth = 16f;

        // Reserve the bottom strip for the pinned reset button; the scroll view gets everything above it.
        Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
        Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);

        // Content is the view minus the scrollbar gutter wide, and the content or the view tall —
        // whichever is larger — so the scrollbar appears only once the rows overflow. contentHeight is 0
        // on the first frame and measured off the listing below for every frame after.
        float innerWidth = viewRect.width - scrollBarWidth;
        Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(contentHeight, viewRect.height));

        Widgets.BeginScrollView(viewRect, ref scrollPosition, innerRect);

        Listing_Standard listing = new Listing_Standard();
        // Tall scratch rect so the listing never clamps its own height; the real one comes back below
        // via CurHeight.
        listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));
        GameFont prevFont = Text.Font;

        listing.Gap();

        DrawWeaponsSection(listing);
        DrawTraitsSection(listing);
        DrawGenerationSection(listing);
        DrawTradersSection(listing);
        DrawAbilitiesSection(listing);
        DrawQuestsSection(listing);
        DrawCompatSection(listing);

        Text.Font = prevFont;
        contentHeight = listing.CurHeight;
        listing.End();
        Widgets.EndScrollView();

        if (Widgets.ButtonText(buttonRect, "RestoreToDefaultSettings".Translate()))
        {
            ResetToDefaults();
        }
    }

    // Top-level section heading (medium font), e.g. "Weapons".
    private static void SectionHeader(Listing_Standard listing, string label)
    {
        Text.Font = GameFont.Medium;
        listing.Label(label);
        Text.Font = GameFont.Small;
        listing.Gap(6f);
    }

    // One checkbox row in the family's model. `forced` is null for a live row; false for a row a
    // missing mod or DLC locks (shown off); true for a row another setting holds on (shown on). The
    // stored field is never written while forced, so the player's choice comes back once the row
    // unlocks. A forced row is greyed whole: CheckboxLabeled(disabled: true) greys only the box, so
    // the label is drawn in Widgets.InactiveColor too (on 1.7 a <color> tag inside the label replaces
    // the draw colour rather than tinting it, so a coloured mod name stays bright; accepted). Row
    // height and spacing match Listing_Standard.CheckboxLabeled, so the two can sit in one list.
    // `label` and `tooltip` are plain strings: a caller injecting a coloured name must Resolve() its
    // TaggedString first, or the implicit conversion strips the tag.
    private static void ToggleRow(Listing_Standard listing, string label, string tooltip, ref bool value,
        bool? forced = null)
    {
        Rect row = listing.GetRect(Text.LineHeight);
        Widgets.DrawHighlightIfMouseover(row);
        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(row, tooltip);
        }
        if (forced is bool shown)
        {
            GUI.color = Widgets.InactiveColor;
            Widgets.CheckboxLabeled(row, label, ref shown, disabled: true);
            GUI.color = Color.white;
        }
        else
        {
            Widgets.CheckboxLabeled(row, label, ref value);
        }
        listing.Gap(listing.verticalSpacing);
    }

    // The tri-state header of a group of def rows an optional mod or DLC owns (the family's "Grouped
    // def rows" pattern). The box is a pure derivation of the members, never separate state: on when
    // every member is enabled, off when none, partial otherwise, and a click (on the box or, like
    // CheckboxLabeled, anywhere on the row) writes every member through `setEnabled`, which may refuse
    // (a trait some weapon needs), so the header is allowed to land partial. Returns whether the
    // caller should draw the member rows beneath it.
    //
    // While the mod is missing there are no members, so the derived state is off, which is exactly the
    // forced-off value the family rule wants; the row is then drawn through the locked ToggleRow path
    // (CheckboxMulti has no disabled parameter) with the "(requires X)" suffix, and nothing beneath.
    // A mod that IS active but owns no loaded member (a stale table entry) draws nothing: that is a
    // data-derived row, and there is nothing to group.
    private static bool DrawGroupHeader(Listing_Standard listing, ModIntegration integration, string labelKey,
        IReadOnlyList<Def> members, Func<Def, bool> isEnabled, Action<Def, bool> setEnabled)
    {
        string name = integration.ColoredName;
        string label = labelKey.Translate(name).Resolve();
        string tooltip = "UMW_IntegrationGroupDesc".Translate(name).Resolve();

        if (!integration.IsActive)
        {
            bool off = false;
            ToggleRow(listing, label + "UMW_RequiresSuffix".Translate(name).Resolve(), tooltip, ref off, forced: false);
            return false;
        }
        if (members.Count == 0)
        {
            return false;
        }

        int enabledCount = members.Count(isEnabled);
        MultiCheckboxState state = enabledCount == members.Count ? MultiCheckboxState.On
            : enabledCount == 0 ? MultiCheckboxState.Off
            : MultiCheckboxState.Partial;

        Rect row = listing.GetRect(Text.LineHeight);
        Widgets.DrawHighlightIfMouseover(row);
        TooltipHandler.TipRegion(row, tooltip);
        Rect labelRect = new Rect(row.x, row.y, row.width - CheckboxSize, row.height);
        TextAnchor prevAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, label);
        Text.Anchor = prevAnchor;

        Rect boxRect = new Rect(row.xMax - CheckboxSize, row.y, CheckboxSize, CheckboxSize);
        MultiCheckboxState newState = Widgets.CheckboxMulti(boxRect, state);
        // Same binary cycle CheckboxMulti applies to a click on the box (on or partial to off, off to
        // on), same sounds.
        if (newState == state && Widgets.ButtonInvisible(labelRect))
        {
            newState = state == MultiCheckboxState.Off ? MultiCheckboxState.On : MultiCheckboxState.Off;
            (newState == MultiCheckboxState.On ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff)
                .PlayOneShotOnCamera();
        }
        if (newState != state)
        {
            // CheckboxMulti only ever returns On or Off from a click.
            bool enabled = newState == MultiCheckboxState.On;
            foreach (Def member in members)
            {
                setEnabled(member, enabled);
            }
        }
        listing.Gap(listing.verticalSpacing);
        return true;
    }

    // One labelled slider row in the house style: "Subject property: value" with the description as a
    // hover tooltip, and the returned value snapped to `step` measured from `min` (so a 1.9-to-12.9
    // radius lands on 1.9, 2.9, ... and never between). `subject` is the row's own content name (an
    // ability, hediff or quest LabelCap), injected as the label key's {0} so the name tracks that def's
    // translation; the value is {1}.
    //
    // `annotateAt` + `annotationLabel` optionally mark another value as a named reference point
    // ("(same as ancient mercenaries)"), for a setting whose number means nothing on its own.
    //
    // Suffixes are independent " (word)" fragments, each tested on its own and appended in order with NO
    // precedence between them, so a value that is both the default and a reference point shows both.
    // Keep it that way when adding a suffix: no else-chaining. Both tests are Mathf.Approximately rather
    // than ==, because snapping off a non-zero `min` does not reproduce the default's exact float (3.9f
    // comes back as 3.8999999761) and an exact compare would silently never show the suffix on those
    // rows. The residue is far below anything the game can act on.
    //
    // `descArg` is the description key's optional {0}, a coloured mod or DLC name today, which is why
    // the tooltip is Resolve()d rather than implicitly converted (the conversion strips colour tags).
    private static float SliderRow(Listing_Standard listing, string labelKey, string descKey,
        string subject, float value, float defaultValue, float min, float max, float step, string format,
        float? annotateAt = null, string annotationLabel = null, string descArg = null)
    {
        string label = labelKey.Translate(subject, value.ToString(format));
        if (Mathf.Approximately(value, defaultValue))
        {
            label += "UMW_DefaultSuffix".Translate();
        }
        if (annotateAt.HasValue && annotationLabel != null && Mathf.Approximately(value, annotateAt.Value))
        {
            label += "UMW_SameAsSuffix".Translate(annotationLabel);
        }
        string tooltip = (descArg == null ? descKey.Translate() : descKey.Translate(descArg)).Resolve();
        listing.Label(label, tooltip: tooltip);
        return Mathf.Round((listing.Slider(value, min, max) - min) / step) * step + min;
    }
}
