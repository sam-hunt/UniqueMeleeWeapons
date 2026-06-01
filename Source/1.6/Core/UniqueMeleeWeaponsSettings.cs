using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Mod settings. Empty for now — add fields here, persist them in <see cref="ExposeData"/>
/// with <c>Scribe_Values.Look</c>, and surface them in <see cref="DoWindowContents"/>.
/// </summary>
public class UniqueMeleeWeaponsSettings : ModSettings
{
    public override void ExposeData()
    {
        base.ExposeData();
        // Scribe_Values.Look(ref someSetting, "someSetting", defaultValue);
    }

    public void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("No settings yet.");
        listing.End();
    }
}
