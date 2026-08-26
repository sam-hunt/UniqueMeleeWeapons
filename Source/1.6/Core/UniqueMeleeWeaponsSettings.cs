using UnityEngine;
using Verse;

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
// Two patterns to copy rather than re-derive: a row that only means something with a DLC present is
// HIDDEN behind a ModsConfig check rather than disabled, and its stored value is never touched, so it
// survives a session without that DLC (Settings_Generation.cs — a wholly DLC-specific section
// early-returns from its Draw*Section instead); and a collection-valued setting scribes with
// Scribe_Collections, which means it must re-create itself on load (Settings_Weapons.cs).
public partial class UniqueMeleeWeaponsSettings : ModSettings
{
    // Trailing space each section leaves below itself, so a section that early-returns leaves no gap
    // behind rather than a double gap between its neighbours.
    private const float SectionGap = 18f;

    // Presentation state for the scroll view, deliberately not scribed.
    private Vector2 scrollPosition;
    private float contentHeight;

    // These two fan out to the sections in display order; serialization order is immaterial (Scribe is
    // keyed by name).
    public override void ExposeData()
    {
        base.ExposeData();
        ExposeWeaponSettings();
        ExposeGenerationSettings();
        ExposeTraderSettings();
        ExposeAbilitySettings();
        ExposeQuestSettings();
        ExposeCompatSettings();
    }

    public void ResetToDefaults()
    {
        ResetWeaponSettings();
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
        DrawGenerationSection(listing);
        DrawTradersSection(listing);
        DrawAbilitiesSection(listing);
        DrawQuestsSection(listing);
        DrawCompatSection(listing);

        Text.Font = prevFont;
        contentHeight = listing.CurHeight;
        listing.End();
        Widgets.EndScrollView();

        if (Widgets.ButtonText(buttonRect, "UMW_ResetToDefaults".Translate()))
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
    private static float SliderRow(Listing_Standard listing, string labelKey, string descKey,
        string subject, float value, float defaultValue, float min, float max, float step, string format,
        float? annotateAt = null, string annotationLabel = null)
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
        listing.Label(label, tooltip: descKey.Translate());
        return Mathf.Round((listing.Slider(value, min, max) - min) / step) * step + min;
    }
}
