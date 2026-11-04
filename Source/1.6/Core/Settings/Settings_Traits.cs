using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// "Traits" settings section: a per-trait opt-out from the trait roll. The gate itself is
// Patches/CompUniqueWeapon_DisabledTraits_Patch.cs, a veto in CompUniqueWeapon.CanAddTrait; it is
// deliberately NOT a commonality write (that file's header: a candidate set whose weights sum to
// zero hands the roll a null trait, and vanilla's ConfigErrors rejects a commonality of zero).
//
// Rows are generated from every WeaponTraitDef this mod's ContentPack shipped (every compat root
// shares it), under one group header per owner from ModIntegrations: our own designs first under
// this mod's name (the unclaimed rest; Core ships no weapon trait, so unlike the Weapons section
// there is no Core group), then the six melee ports of Odyssey's ranged traits under Odyssey, then
// the Royalty six under a header that shows disabled, with the requirement, while Royalty is
// absent. That header replaced the old allowUltratechTraits toggle; ExposeTraitSettings migrates a
// saved "off" into this set once.
//
// A weapon with no traits is broken, not merely plain: CompUniqueWeapon.PostPostMake names it from
// a RandomElement over its traits' adjectives, which errors on an empty list and leaves a null in
// the grammar (decompile-verified, RimWorld 1.7). The roll takes 1 to 3 traits from the candidates
// CanAddTrait admits, and the FIRST pick is limited to canGenerateAlone traits in the weapon's
// categories, so the invariant is: every unique weapon keeps at least one enabled canGenerateAlone
// trait across its categories. The window enforces it by holding the last such trait on (the
// family's "forced on" row state, suffixed with the weapon it is keeping alive), and
// ApplyTraitAvailability re-enables what a hand-edited config would strand, at every play-data
// load and window close. Both read the first-pick cache below, never the DefDatabase directly.
//
// One roll-time veto the window cannot see: the war merchant's scoped ultratech ban
// (Patches/CompUniqueWeapon_DisabledTraits_Patch.cs). A player who disables every first pick of a
// weapon but an ultratech one satisfies the invariant here and would still hand that merchant's
// roll an empty set, so the generator asks HasEnabledFirstPick with the ban as an extra veto and
// lifts the ban for that one roll when the answer is no.
public partial class UniqueMeleeWeaponsSettings
{
    // Trait labels are short and carry no icon, so three columns fit where the weapons take two,
    // at the plain row height.
    private const int TraitColumns = 3;

    // Stores the defNames the player switched OFF, so the empty default means "all enabled" and a
    // trait added later is on without a settings migration. Keyed by defName so the stored value
    // survives a session with the def absent (the Royalty six without Royalty).
    public HashSet<string> disabledTraits = new HashSet<string>();

    private void ExposeTraitSettings()
    {
        Scribe_Collections.Look(ref disabledTraits, "disabledTraits", LookMode.Value);
        // Scribe_Collections leaves the field null on an absent or empty entry, which is the default
        // state, nothing disabled, so restore the empty set rather than carrying a null.
        disabledTraits ??= new HashSet<string>();

        // One-way migration of the retired allowUltratechTraits toggle (a single bool, default on,
        // through 1.x): a saved "off" becomes the whole Royalty trait group in this set, the five
        // ultratech traits and the period-neutral lead-lined alike, by decision (no special handling
        // for a retired key; the row re-enables it). Read only, never written back, so the key
        // disappears from the config on the next save.
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            bool legacyAllowUltratech = true;
            Scribe_Values.Look(ref legacyAllowUltratech, "allowUltratechTraits", true);
            if (!legacyAllowUltratech)
            {
                disabledTraits.UnionWith(ModIntegrations.Royalty.traitDefNames);
            }
        }
    }

    private void ResetTraitSettings()
    {
        disabledTraits.Clear();
    }

    // Count first: the veto asks this about every candidate trait of every roll, and the common case
    // is an empty set.
    public bool IsTraitDisabled(WeaponTraitDef def)
    {
        return disabledTraits.Count > 0 && def != null && disabledTraits.Contains(def.defName);
    }

    // The traits that get a row: ours, by ContentPack, in label order per language. Rebuilt per draw
    // rather than cached because the DefDatabase instances are replaced on an in-process reload
    // (the same reason UniqueWeaponDefs rebuilds per load), and the window is not a hot path.
    private static List<WeaponTraitDef> OurTraits()
    {
        return DefDatabase<WeaponTraitDef>.AllDefs
            .Where(t => t.modContentPack == UniqueMeleeWeaponsMod.ContentPack)
            .OrderBy(t => t.label)
            .ToList();
    }

    // The first-pick candidates of every unique weapon: each ThingDef carrying CompUniqueWeapon (not
    // just ours: a third-party unique listing our categories rolls from the same candidates) with
    // the canGenerateAlone traits across its categories, the only traits the roll's FIRST pick can
    // take. Derived from the DefDatabase once per play-data load, from UMW_Startup.Run, on the same
    // cadence as UniqueWeaponDefs (an in-process reload replaces every def instance), never per
    // draw: StrandedBy used to run this derivation for every drawn trait cell on every frame, a
    // full ThingDef scan each, which a large mod list turns into visible stutter while the window
    // is open. Against the cache the window's checks are a few short list walks. Replaced whole on
    // rebuild (a generation on a worker thread may be reading it through HasEnabledFirstPick).
    private static List<FirstPickSet> firstPickSets = new List<FirstPickSet>();

    private sealed class FirstPickSet
    {
        public ThingDef weapon;
        public List<WeaponTraitDef> candidates;
    }

    public static void RebuildFirstPickCache()
    {
        List<FirstPickSet> sets = new List<FirstPickSet>();
        foreach (ThingDef weapon in DefDatabase<ThingDef>.AllDefs)
        {
            CompProperties_UniqueWeapon props = weapon.GetCompProperties<CompProperties_UniqueWeapon>();
            if (props?.weaponCategories == null)
            {
                continue;
            }
            sets.Add(new FirstPickSet
            {
                weapon = weapon,
                candidates = DefDatabase<WeaponTraitDef>.AllDefs
                    .Where(t => t.canGenerateAlone && props.weaponCategories.Contains(t.weaponCategory))
                    .ToList(),
            });
        }
        firstPickSets = sets;
    }

    // The first unique weapon whose first pick would have NO enabled candidate left were `trait`
    // disabled too, or null if disabling it is safe.
    private ThingDef StrandedBy(WeaponTraitDef trait)
    {
        if (!trait.canGenerateAlone)
        {
            return null;
        }
        foreach (FirstPickSet set in firstPickSets)
        {
            if (!set.candidates.Contains(trait))
            {
                continue;
            }
            bool otherSurvivor = false;
            foreach (WeaponTraitDef other in set.candidates)
            {
                if (other != trait && !IsTraitDisabled(other))
                {
                    otherSurvivor = true;
                    break;
                }
            }
            if (!otherSurvivor)
            {
                return set.weapon;
            }
        }
        return null;
    }

    // Whether `weapon` still has a first-pick candidate that is neither disabled here nor vetoed by
    // `alsoVetoed` (a roll-time veto the window does not model; the war merchant's ultratech ban).
    // True for a weapon the cache does not know: nothing here to guard.
    public bool HasEnabledFirstPick(ThingDef weapon, Func<WeaponTraitDef, bool> alsoVetoed)
    {
        foreach (FirstPickSet set in firstPickSets)
        {
            if (set.weapon != weapon)
            {
                continue;
            }
            foreach (WeaponTraitDef trait in set.candidates)
            {
                if (!IsTraitDisabled(trait) && !alsoVetoed(trait))
                {
                    return true;
                }
            }
            return false;
        }
        return true;
    }

    // Backstop for the window's held-on rows: a config edited by hand (or a trait roster change
    // between versions) can strand a weapon, so re-enable every disabled first-pick candidate of a
    // stranded weapon's categories and say so. Idempotent; safe outside a game. Called from
    // UMW_Startup.Run (after RebuildFirstPickCache) and UniqueMeleeWeaponsMod.WriteSettings.
    public void ApplyTraitAvailability()
    {
        if (disabledTraits.Count == 0)
        {
            return;
        }
        foreach (FirstPickSet set in firstPickSets)
        {
            if (set.candidates.Count == 0 || set.candidates.Any(t => !IsTraitDisabled(t)))
            {
                continue;
            }
            foreach (WeaponTraitDef trait in set.candidates)
            {
                disabledTraits.Remove(trait.defName);
            }
            Log.Warning($"[Unique Melee Weapons] Every trait {set.weapon.defName} could roll first was disabled in settings; re-enabled them so it can still be named.");
        }
    }

    // Under one group header per owner: this mod first (everything no integration claims: our own
    // designs), then each integration's group that owns traits (Odyssey's ports, Royalty's three).
    // Every trait cell is indented the same, whichever group it is in, and the cells are paintable
    // (click and drag to set a run of them; ToggleCell).
    private void DrawTraitsSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsTraits".Translate(), tooltip: "UMW_SettingsTraitsDesc".Translate());

        List<WeaponTraitDef> traits = OurTraits();
        DrawTraitGroup(listing, ModIntegrations.Self,
            traits.Where(t => ModIntegrations.OwnerOfTrait(t) == null).ToList());
        foreach (ModIntegration integration in ModIntegrations.Grouped)
        {
            if (integration.traitDefNames.Length == 0)
            {
                continue;
            }
            DrawTraitGroup(listing, integration, traits.Where(integration.OwnsTrait).ToList());
        }

        EndSection(listing);
    }

    private void DrawTraitGroup(Listing_Standard listing, ModIntegration integration, List<WeaponTraitDef> members)
    {
        if (!DrawGroupHeader(listing, integration, members, integration.traitDefNames.Length,
            t => !IsTraitDisabled((WeaponTraitDef)t), SetTraitEnabled))
        {
            return;
        }
        DrawDefGrid(listing, members, TraitColumns, Text.LineHeight, DrawTraitCell);
        listing.Gap(GroupGap);
    }

    // The checkbox reads as "enabled" while the stored set holds the DISABLED ones, so the ref bool is
    // a local written back only on an actual change; that keeps the set free of entries for traits the
    // player never touched. A trait that is the last first-pick candidate for some weapon is held on,
    // suffixed with that weapon's name. The tooltip leads with the setting's explanation, then the
    // trait itself, rendered exactly as its block on a weapon's info card (label, description, effect
    // lines, stat lines; the one renderer in CompUniqueWeapon_TraitStats_Patch); built lazily, only
    // while hovered (ToggleCell's TipSignal). A held-on label with its suffix can outrun a
    // three-column cell, where ToggleCell clamps it, so the tooltip of a held-on row ends with the
    // same suffix, which also tells the player why its box is greyed.
    private void DrawTraitCell(Rect cell, WeaponTraitDef trait)
    {
        bool enabled = !IsTraitDisabled(trait);
        bool wasEnabled = enabled;
        string label = trait.LabelCap;
        bool? forced = null;
        string heldOnNote = null;
        if (enabled && StrandedBy(trait) is ThingDef stranded)
        {
            string suffix = "UMW_LastTraitSuffix".Translate(stranded.label);
            label += suffix;
            heldOnNote = suffix.Trim();
            forced = true;
        }
        TipSignal tooltip = new TipSignal(() => TraitTooltip(trait, heldOnNote), trait.GetHashCode());
        ToggleCell(cell, label, tooltip, ref enabled, forced, paintable: true, boxLeft: true);
        if (enabled != wasEnabled)
        {
            SetTraitEnabled(trait, enabled);
        }
    }

    // The explanation's injected trait name takes the block's yellow, tying the two together.
    // Resolve() keeps the colour: adding a TaggedString to the string would yield a TaggedString, and
    // its implicit conversion back to string strips every tag.
    private static string TraitTooltip(WeaponTraitDef trait, string heldOnNote)
    {
        string tooltip = "UMW_TraitEnabledDesc".Translate(trait.label.Colorize(ColorLibrary.Yellow)).Resolve() + "\n\n"
            + Patches.CompUniqueWeapon_TraitStats_Patch.TraitBlock(trait);
        return heldOnNote == null ? tooltip : tooltip + "\n\n" + heldOnNote;
    }

    // Also the group header's bulk setter. Disabling is refused for a trait some weapon needs, so a
    // bulk "off" on a group can legitimately leave the header partial.
    private void SetTraitEnabled(Def def, bool enabled)
    {
        WeaponTraitDef trait = (WeaponTraitDef)def;
        if (enabled)
        {
            disabledTraits.Remove(trait.defName);
        }
        else if (StrandedBy(trait) == null)
        {
            disabledTraits.Add(trait.defName);
        }
    }
}
