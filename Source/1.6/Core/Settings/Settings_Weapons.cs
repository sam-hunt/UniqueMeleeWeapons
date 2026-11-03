using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// "Weapons" settings section: a per-weapon opt-out from every generation and reward pool. The gate
// itself is Patches/ThingSetMakerUtility_CanGenerate_Patch.cs.
//
// Rows are generated from UniqueWeaponDefs.All rather than hand-written: a weapon added later gets its
// row for free, and a weapon whose compat root did not load is simply absent. The rows an optional mod
// or DLC owns (ModIntegrations) sit under a tri-state group header for that mod, which is what stays
// visible, disabled and labelled with the requirement, while the mod is missing and its defs cannot
// draw a row of their own (the family's "Grouped def rows" pattern; this file is its reference).
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

    // Whether any of our weapons (optionally: any passing filter) can still be rolled at all. The warband
    // quest's whole reward is one of them, so it stops being offered when the answer is no for the
    // weapons it may roll, i.e. filter UniqueWeaponDefs.FitsTribal (QuestNode_Root_Warband.TestRunInt).
    public bool AnyWeaponEnabled(Predicate<ThingDef> filter = null)
    {
        return UniqueWeaponDefs.All.Any(d => !IsWeaponDisabled(d) && (filter == null || filter(d)));
    }

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

    // One checkbox per weapon we own, labelled with the weapon's own (already localized) def label:
    // the ungrouped ones first, then each integration's group.
    private void DrawWeaponsSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsWeapons".Translate());

        foreach (ThingDef weapon in UniqueWeaponDefs.All.Where(w => ModIntegrations.OwnerOfWeapon(w) == null))
        {
            DrawWeaponRow(listing, weapon);
        }
        foreach (ModIntegration integration in ModIntegrations.Grouped)
        {
            if (integration.weaponDefNames.Length == 0)
            {
                continue;
            }
            List<ThingDef> members = UniqueWeaponDefs.All.Where(integration.OwnsWeapon).ToList();
            if (!DrawGroupHeader(listing, integration, "UMW_IntegrationWeapons", members,
                    w => !IsWeaponDisabled((ThingDef)w), SetWeaponEnabled))
            {
                continue;
            }
            listing.Indent(GroupIndent);
            foreach (ThingDef weapon in members)
            {
                DrawWeaponRow(listing, weapon);
            }
            listing.Outdent(GroupIndent);
        }

        listing.Gap(SectionGap);
    }

    // The checkbox reads as "enabled" while the stored set holds the DISABLED ones, so the ref bool is a
    // local written back only on an actual change; that keeps the set free of entries for weapons the
    // player never touched.
    private void DrawWeaponRow(Listing_Standard listing, ThingDef weapon)
    {
        bool enabled = !IsWeaponDisabled(weapon);
        bool wasEnabled = enabled;
        ToggleRow(listing, weapon.LabelCap, "UMW_WeaponEnabledDesc".Translate(weapon.label), ref enabled);
        if (enabled != wasEnabled)
        {
            SetWeaponEnabled(weapon, enabled);
        }
    }

    // Also the group header's bulk setter.
    private void SetWeaponEnabled(Def weapon, bool enabled)
    {
        if (enabled)
        {
            disabledWeapons.Remove(weapon.defName);
        }
        else
        {
            disabledWeapons.Add(weapon.defName);
        }
    }
}
