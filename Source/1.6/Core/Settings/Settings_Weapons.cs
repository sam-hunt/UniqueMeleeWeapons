using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Weapons" settings section: a per-weapon opt-out from every generation and reward pool. The gate
// itself is Patches/ThingSetMakerUtility_CanGenerate_Patch.cs.
//
// Rows are generated from UniqueWeaponDefs.All rather than hand-written, so the section needs no
// ModsConfig gate of its own — a MayRequire-gated def is absent without its DLC, so the Royalty pair's
// rows appear exactly when those defs do, and a weapon added later gets its row for free.
public partial class UniqueMeleeWeaponsSettings
{
    // Stores the defNames the player switched OFF, so the empty default means "all enabled" and a weapon
    // that appears later is on without a settings migration. Keyed by defName rather than by ThingDef so
    // the stored value survives a session with the def absent.
    public HashSet<string> disabledWeapons = new HashSet<string>();

    private void ExposeWeaponSettings()
    {
        Scribe_Collections.Look(ref disabledWeapons, "disabledWeapons", LookMode.Value);
        // Scribe_Collections leaves the field null on an absent or empty entry — which is the default
        // state, nothing disabled — so restore the empty set rather than carrying a null.
        disabledWeapons ??= new HashSet<string>();
    }

    private void ResetWeaponSettings()
    {
        disabledWeapons.Clear();
    }

    // Count first: the pool patch asks this about every candidate ThingDef of every roll, and the common
    // case is an empty set.
    public bool IsWeaponDisabled(ThingDef def)
    {
        return disabledWeapons.Count > 0 && def != null && disabledWeapons.Contains(def.defName);
    }

    // Whether any of our weapons can still be rolled at all. The warband quest's whole reward is one of
    // them, so it stops being offered when the answer is no (QuestNode_Root_Warband.TestRunInt).
    public bool AnyWeaponEnabled => UniqueWeaponDefs.All.Any(d => !IsWeaponDisabled(d));

    // Refreshes the one place a weapon's pool eligibility is CACHED rather than asked live, so toggling a
    // weapon mid-session needs no restart. ThingSetMakerUtility.allGeneratableItems is filled once at
    // play-data load (our patch is therefore already reflected in it at startup; this only matters for a
    // later change) and feeds base-gen's item scatterers and QuestNode_TradeRequest. Reset() does nothing
    // but refill that list, so re-running it is idempotent and safe outside a game.
    //
    // Every other pool path calls CanGenerate live through GetAllowedThingDefs and needs no invalidation.
    // (Decompile-verified, RimWorld 1.6.)
    public void ApplyWeaponAvailability()
    {
        ThingSetMakerUtility.Reset();
    }

    // One checkbox per weapon we own, labelled with the weapon's own (already localized) def label.
    //
    // The checkbox reads as "enabled" while the stored set holds the DISABLED ones, so the ref bool is a
    // local written back only on an actual change; that keeps the set free of entries for weapons the
    // player never touched.
    private void DrawWeaponsSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsWeapons".Translate());

        foreach (ThingDef weapon in UniqueWeaponDefs.All)
        {
            bool enabled = !IsWeaponDisabled(weapon);
            bool wasEnabled = enabled;
            listing.CheckboxLabeled(
                weapon.LabelCap,
                ref enabled,
                "UMW_WeaponEnabledDesc".Translate(weapon.label));
            if (enabled == wasEnabled)
            {
                continue;
            }
            if (enabled)
            {
                disabledWeapons.Remove(weapon.defName);
            }
            else
            {
                disabledWeapons.Add(weapon.defName);
            }
        }

        listing.Gap(SectionGap);
    }
}
