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

    // Two indent steps: every section's body sits one step under its header (SectionHeader indents
    // the listing itself, EndSection outdents it), and the member rows of a grouped section sit one
    // more under their group header (a mod or DLC's name), so the groups read as sub-blocks. The
    // section step is applied through Listing.Indent PLUS a matching ColumnWidth cut: Indent alone
    // only moves curX and leaves ColumnWidth alone, so an indented CheckboxLabeled row would overflow
    // the listing by the indent and its box, drawn at the row's right edge, would be clipped by the
    // scroll view. The group step is taken off the grid row's left edge (DrawDefGrid).
    private const float SectionIndent = 24f;
    private const float GroupRowIndent = 24f;

    // Extra space below each group's last member row, so groups read as blocks.
    private const float GroupGap = 8f;

    // Group headers draw one font step above their member rows (Medium over Small; vanilla has no
    // intermediate size), so a long list scans by its owners.
    private const GameFont GroupHeaderFont = GameFont.Medium;

    // The def grids (DrawDefGrid): members fill columns top to bottom, like a newspaper, so an
    // alphabetical list still reads downwards. Space between the columns.
    private const float ColumnGutter = 16f;

    // A cell's icon slot is this many times wider than the row is tall: the weapon art lies
    // horizontal across its square texture, so a square slot would leave the blade a sliver.
    // Then the gap between a cell's parts (box, icon slot, label), and how far the icon fades
    // while its row is off or locked, so the eye can skim the enabled set.
    private const float IconSlotAspect = 2f;
    private const float CellGap = 6f;
    private const float DisabledIconAlpha = 0.35f;

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

    // Top-level section heading (medium font), e.g. "Weapons". Indents the listing for the section's
    // body; every section ends with EndSection, on every return path after its header.
    private static void SectionHeader(Listing_Standard listing, string label)
    {
        Text.Font = GameFont.Medium;
        listing.Label(label);
        Text.Font = GameFont.Small;
        listing.Gap(6f);
        listing.Indent(SectionIndent);
        listing.ColumnWidth -= SectionIndent;
    }

    // Undoes SectionHeader's indent and leaves the trailing section gap.
    private static void EndSection(Listing_Standard listing)
    {
        listing.ColumnWidth += SectionIndent;
        listing.Outdent(SectionIndent);
        listing.Gap(SectionGap);
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
    //
    // `paintable` turns on vanilla's click-and-drag painting (Widgets.CheckboxLabeled's own parameter,
    // the Animals tab's behaviour): a drag that starts on a paintable row sets every paintable row it
    // crosses to the same value, with the brush shown at the cursor, until the button is released.
    // The paint state is one static in Widgets, so it carries across sections and groups; the long
    // generated def lists opt in, the handful of standalone toggles do not. A forced row never takes
    // part (CheckboxLabeled skips its click handling when disabled), so a held-on row stays held.
    private static void ToggleRow(Listing_Standard listing, string label, string tooltip, ref bool value,
        bool? forced = null, bool paintable = false)
    {
        TipSignal? tip = tooltip.NullOrEmpty() ? null : new TipSignal(tooltip);
        ToggleRow(listing, label, tip, ref value, forced, paintable);
    }

    // The same row with a TipSignal, for a tooltip that is built lazily: the def rows render a def's
    // description and effect lines into theirs, which TooltipHandler then only asks for while the row
    // is actually hovered (a Func<string> TipSignal), rather than every row every frame.
    private static void ToggleRow(Listing_Standard listing, string label, TipSignal? tooltip, ref bool value,
        bool? forced = null, bool paintable = false)
    {
        Rect row = listing.GetRect(Text.LineHeight);
        ToggleCell(row, label, tooltip, ref value, forced, paintable);
        listing.Gap(listing.verticalSpacing);
    }

    // The row's body, in any rect: the unit a grid cell and a full-width row share. The box sits
    // at the cell's right edge, where Widgets.CheckboxLabeled puts it, unless `boxLeft`, which puts
    // it first: the grid cells and group headers pass that, because in a multi-column grid a box at
    // the right edge of one cell sits flush against the next column's icon and label and reads as
    // theirs, while at the left edge it leads its own cell's icon and label (and a group header's
    // box then sits one indent step left of its members', a tree). `drawIcon`, if given, gets a
    // slot of the cell's height and IconSlotAspect times that wide, after the box when the box
    // leads and at the cell's left edge otherwise (and whether the cell shows on, so it can dim),
    // and the label takes the rest. The three parts are Widgets.CheckboxLabeled's own primitives
    // in its order (Label, ToggleInvisibleDraggable, CheckboxDraw) rather than that call, because
    // the hit rect must stay the WHOLE cell while the label starts after the icon: through
    // CheckboxLabeled the icon would be dead to a click and to a paint drag running down the icon
    // column. The label is clamped to its space with an ellipsis rather than wrapped (a wrapped
    // line would spill into the row beneath). A row's own tooltip is expected to lead with the
    // label (the def rows do), so a clamped label is tipped in full only when the row has no
    // tooltip. A label carrying a colour tag is never clamped: the clamp cuts the raw string and
    // could leave a half tag on screen (only the locked group header passes one, full width).
    private static void ToggleCell(Rect cell, string label, TipSignal? tooltip, ref bool value,
        bool? forced = null, bool paintable = false, Action<Rect, bool> drawIcon = null, bool boxLeft = false)
    {
        Widgets.DrawHighlightIfMouseover(cell);
        if (tooltip is TipSignal tip)
        {
            TooltipHandler.TipRegion(cell, tip);
        }
        bool shownValue = forced ?? value;
        Rect labelRect = cell;
        float boxX;
        if (boxLeft)
        {
            boxX = cell.x;
            labelRect.xMin += CheckboxSize + CellGap;
        }
        else
        {
            boxX = cell.xMax - CheckboxSize;
            labelRect.xMax -= CheckboxSize;
        }
        if (drawIcon != null)
        {
            float slotWidth = cell.height * IconSlotAspect;
            drawIcon(new Rect(labelRect.x, cell.y, slotWidth, cell.height).ContractedBy(1f), shownValue);
            labelRect.xMin += slotWidth + CellGap;
        }
        string shownLabel = label.IndexOf('<') >= 0 ? label : Text.ClampTextWithEllipsis(labelRect, label);
        if (shownLabel != label && tooltip == null)
        {
            TooltipHandler.TipRegion(cell, new TipSignal(label, label.GetHashCode()));
        }

        TextAnchor prevAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;
        float boxY = cell.y + (cell.height - CheckboxSize) / 2f;
        if (forced is bool shown)
        {
            GUI.color = Widgets.InactiveColor;
            Widgets.Label(labelRect, shownLabel);
            Widgets.CheckboxDraw(boxX, boxY, shown, disabled: true);
            GUI.color = Color.white;
        }
        else
        {
            Widgets.Label(labelRect, shownLabel);
            Widgets.ToggleInvisibleDraggable(cell, ref value, doMouseoverSound: true, paintable);
            Widgets.CheckboxDraw(boxX, boxY, value, disabled: false);
        }
        Text.Anchor = prevAnchor;
    }

    // Lays a group's member rows out in `columns` columns under its header, every cell `rowHeight`
    // tall, members running down each column before the next (so column one holds the first
    // ceil(n / columns) members). Each grid row is one listing rect, so the listing's own height
    // tracking and the scroll view stay correct, and the paint drag crosses cells in whichever
    // direction the mouse moves.
    private static void DrawDefGrid<T>(Listing_Standard listing, IReadOnlyList<T> members, int columns,
        float rowHeight, Action<Rect, T> drawCell)
    {
        int rows = (members.Count + columns - 1) / columns;
        for (int row = 0; row < rows; row++)
        {
            Rect rowRect = listing.GetRect(rowHeight);
            rowRect.xMin += GroupRowIndent;
            float cellWidth = (rowRect.width - ColumnGutter * (columns - 1)) / columns;
            for (int column = 0; column < columns; column++)
            {
                int index = column * rows + row;
                if (index >= members.Count)
                {
                    break;
                }
                Rect cell = new Rect(rowRect.x + column * (cellWidth + ColumnGutter), rowRect.y, cellWidth, rowHeight);
                drawCell(cell, members[index]);
            }
            listing.Gap(listing.verticalSpacing);
        }
    }

    // The tri-state header of a group of def rows an optional mod or DLC owns (the family's "Grouped
    // def rows" pattern). Its label is the owner's bare coloured name: the section header above it
    // already says whether these are weapons or traits. The box is a pure derivation of the members,
    // never separate state: on when every member is enabled, off when none, partial otherwise, and a
    // click (on the box, which leads the row like its members' boxes lead their cells, or, like
    // CheckboxLabeled, anywhere on the row) writes every member through
    // `setEnabled`, which may refuse (a trait some weapon needs), so the header is allowed to land
    // partial. Returns whether the caller should draw the member rows beneath it.
    //
    // The header is deliberately NOT paintable, although the member rows are: a paint drag that
    // crosses a header on its way down a list would otherwise switch that whole group, including rows
    // the drag never reached. The drag passes over the header without effect and resumes on the rows
    // beneath; painting stays a per-row brush, and bulk changes stay a deliberate click.
    //
    // While the mod is missing there are no members, so the derived state is off, which is exactly the
    // forced-off value the family rule wants; the row is then drawn through the locked ToggleCell path
    // (CheckboxMulti has no disabled parameter) with the "(not active)" suffix, which with the name as
    // the label is the family's "(requires X)" in the one form that does not repeat X, and nothing
    // beneath. A mod that IS active but owns no loaded member (a stale table entry) draws nothing:
    // that is a data-derived row, and there is nothing to group.
    private static bool DrawGroupHeader(Listing_Standard listing, ModIntegration integration,
        IReadOnlyList<Def> members, Func<Def, bool> isEnabled, Action<Def, bool> setEnabled)
    {
        string name = integration.ColoredName;
        string tooltip = "UMW_IntegrationGroupDesc".Translate(name).Resolve();

        if (integration.IsActive && members.Count == 0)
        {
            return false;
        }

        // The header spans the row's full width (the member grids below split theirs).
        GameFont prevFont = Text.Font;
        Text.Font = GroupHeaderFont;
        Rect row = listing.GetRect(Text.LineHeight);

        if (!integration.IsActive)
        {
            bool off = false;
            ToggleCell(row, name + "UMW_NotActiveSuffix".Translate().Resolve(), new TipSignal(tooltip), ref off,
                forced: false, boxLeft: true);
            listing.Gap(listing.verticalSpacing);
            Text.Font = prevFont;
            return false;
        }

        int enabledCount = members.Count(isEnabled);
        MultiCheckboxState state = enabledCount == members.Count ? MultiCheckboxState.On
            : enabledCount == 0 ? MultiCheckboxState.Off
            : MultiCheckboxState.Partial;

        // Box first, like the member cells beneath it (ToggleCell's boxLeft).
        Widgets.DrawHighlightIfMouseover(row);
        TooltipHandler.TipRegion(row, tooltip);
        Rect boxRect = new Rect(row.x, row.y + (row.height - CheckboxSize) / 2f, CheckboxSize, CheckboxSize);
        Rect labelRect = new Rect(boxRect.xMax + CellGap, row.y, row.width - CheckboxSize - CellGap, row.height);
        TextAnchor prevAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, name);
        Text.Anchor = prevAnchor;

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
        Text.Font = prevFont;
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
