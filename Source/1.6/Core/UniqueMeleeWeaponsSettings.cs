using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Mod settings. No configurable fields yet — but the settings window is already
// wired for a robust list of balance toggles: the body renders inside a scroll
// view that only shows a scrollbar once the content would overflow vertically,
// and a "Reset to defaults" button sits pinned below it.
//
// To add a setting:
//  1. declare a public field here (with its default as the initializer),
//  2. persist it in ExposeData with Scribe_Values.Look
//     (pass the same default so an unset value loads correctly),
//  3. restore it in ResetToDefaults,
//  4. surface it in DoWindowContents via the listing
//     (see the commented example there).
// The scroll view measures its own content each frame, so new rows need no
// layout bookkeeping — they just push the scrollbar in once they don't fit.
public class UniqueMeleeWeaponsSettings : ModSettings
{
    // --- Settings fields (none yet) ---------------------------------------
    // public bool exampleToggle = true;

    // --- Transient UI state (not persisted) -------------------------------
    // Scroll offset and last-measured content height for the settings list.
    // These are presentation state, so they are deliberately NOT scribed.
    private Vector2 scrollPosition;
    private float contentHeight;

    public override void ExposeData()
    {
        base.ExposeData();
        // Scribe_Values.Look(ref exampleToggle, "exampleToggle", true);
    }

    // Restores every setting to its shipped default. Called by the
    // "Reset to defaults" button. Keep this in sync with the field
    // initializers above (and the Scribe_Values.Look defaults).
    public void ResetToDefaults()
    {
        // exampleToggle = true;
    }

    public void DoWindowContents(Rect inRect)
    {
        const float buttonHeight = 30f;
        const float buttonGap = 10f;
        const float buttonWidth = 200f;
        const float scrollBarWidth = 16f;

        // Reserve the bottom strip for the pinned reset button; the scroll
        // view gets everything above it.
        Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
        Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);

        // The inner (content) rect is as wide as the view minus the scrollbar
        // gutter, and as tall as the content OR the view — whichever is larger.
        // When content fits, inner == view height, so no scrollbar shows; once
        // content exceeds the view, the inner grows and the scrollbar appears.
        // contentHeight is 0 on the first frame (so no scroll) and is measured
        // from the listing below for every frame after.
        float innerWidth = viewRect.width - scrollBarWidth;
        Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(contentHeight, viewRect.height));

        Widgets.BeginScrollView(viewRect, ref scrollPosition, innerRect);

        Listing_Standard listing = new Listing_Standard();
        // Begin with a tall scratch rect (99999f) so the listing never clamps
        // its own height; we read the real height back via CurHeight afterwards.
        listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));

        // --- Settings rows go here ----------------------------------------
        // Example (uncomment alongside an `exampleToggle` field above):
        //   listing.CheckboxLabeled("Example toggle", ref exampleToggle, "What it does.");
        //   listing.Gap();
        listing.Label("No configurable settings yet.");

        // Measure the content height for next frame's scroll calculation,
        // then close the listing and the scroll view.
        contentHeight = listing.CurHeight;
        listing.End();
        Widgets.EndScrollView();

        if (Widgets.ButtonText(buttonRect, "Reset to defaults"))
        {
            ResetToDefaults();
        }
    }
}
