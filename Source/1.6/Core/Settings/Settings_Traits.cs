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
// shares it), ungrouped ones first and then one group per optional-content owner from
// ModIntegrations, so the Royalty three sit under a "Royalty traits" header that shows disabled,
// with the requirement, while Royalty is absent. That header replaced the old allowUltratechTraits
// toggle; ExposeTraitSettings migrates a saved "off" into this set once.
//
// A weapon with no traits is broken, not merely plain: CompUniqueWeapon.PostPostMake names it from
// a RandomElement over its traits' adjectives, which errors on an empty list and leaves a null in
// the grammar (decompile-verified, RimWorld 1.7). The roll takes 1 to 3 traits from the candidates
// CanAddTrait admits, and the FIRST pick is limited to canGenerateAlone traits in the weapon's
// categories, so the invariant is: every unique weapon keeps at least one enabled canGenerateAlone
// trait across its categories. The window enforces it by holding the last such trait on (the
// family's "forced on" row state, suffixed with the weapon it is keeping alive), and
// ApplyTraitAvailability re-enables what a hand-edited config would strand, at every play-data
// load and window close.
public partial class UniqueMeleeWeaponsSettings
{
    // Trait labels are short and carry no icon, so three columns fit where the weapons take two;
    // the cells keep the small font and the plain row height.
    private const int TraitColumns = 3;

    // Stores the defNames the player switched OFF, so the empty default means "all enabled" and a
    // trait added later is on without a settings migration. Keyed by defName so the stored value
    // survives a session with the def absent (the Royalty three without Royalty).
    public HashSet<string> disabledTraits = new HashSet<string>();

    private void ExposeTraitSettings()
    {
        Scribe_Collections.Look(ref disabledTraits, "disabledTraits", LookMode.Value);
        // Scribe_Collections leaves the field null on an absent or empty entry, which is the default
        // state, nothing disabled, so restore the empty set rather than carrying a null.
        disabledTraits ??= new HashSet<string>();

        // One-way migration of the retired allowUltratechTraits toggle (a single bool, default on,
        // through 1.x): a saved "off" becomes the Royalty three in this set. Read only, never
        // written back, so the key disappears from the config on the next save.
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

    // The first unique weapon whose first pick would have NO enabled candidate left were `trait`
    // disabled too, or null if disabling it is safe. Only canGenerateAlone traits count, because only
    // they can be a first pick. Every ThingDef with the comp is checked, not just ours: a third-party
    // unique listing our categories rolls from the same candidates.
    private ThingDef StrandedBy(WeaponTraitDef trait)
    {
        if (!trait.canGenerateAlone)
        {
            return null;
        }
        foreach (ThingDef weapon in DefDatabase<ThingDef>.AllDefs)
        {
            CompProperties_UniqueWeapon props = weapon.GetCompProperties<CompProperties_UniqueWeapon>();
            if (props?.weaponCategories == null || !props.weaponCategories.Contains(trait.weaponCategory))
            {
                continue;
            }
            bool otherSurvivor = DefDatabase<WeaponTraitDef>.AllDefs.Any(t =>
                t != trait && t.canGenerateAlone && !IsTraitDisabled(t)
                && props.weaponCategories.Contains(t.weaponCategory));
            if (!otherSurvivor)
            {
                return weapon;
            }
        }
        return null;
    }

    // Backstop for the window's held-on rows: a config edited by hand (or a trait roster change
    // between versions) can strand a weapon, so re-enable every disabled first-pick candidate of a
    // stranded weapon's categories and say so. Idempotent; safe outside a game. Called from
    // UMW_Startup.Run (after defs load) and UniqueMeleeWeaponsMod.WriteSettings.
    public void ApplyTraitAvailability()
    {
        if (disabledTraits.Count == 0)
        {
            return;
        }
        foreach (ThingDef weapon in DefDatabase<ThingDef>.AllDefs)
        {
            CompProperties_UniqueWeapon props = weapon.GetCompProperties<CompProperties_UniqueWeapon>();
            if (props?.weaponCategories == null)
            {
                continue;
            }
            List<WeaponTraitDef> firstPicks = DefDatabase<WeaponTraitDef>.AllDefs
                .Where(t => t.canGenerateAlone && props.weaponCategories.Contains(t.weaponCategory))
                .ToList();
            if (firstPicks.Count == 0 || firstPicks.Any(t => !IsTraitDisabled(t)))
            {
                continue;
            }
            foreach (WeaponTraitDef trait in firstPicks)
            {
                disabledTraits.Remove(trait.defName);
            }
            Log.Warning($"[Unique Melee Weapons] Every trait {weapon.defName} could roll first was disabled in settings; re-enabled them so it can still be named.");
        }
    }

    // Under one group header per owner: Core first (everything no integration claims), then each
    // integration's group. Every trait cell is indented the same, whichever group it is in, and the
    // cells are paintable (click and drag to set a run of them; ToggleCell).
    private void DrawTraitsSection(Listing_Standard listing)
    {
        SectionHeader(listing, "UMW_SettingsTraits".Translate());

        List<WeaponTraitDef> traits = OurTraits();
        DrawTraitGroup(listing, ModIntegrations.Core,
            traits.Where(t => ModIntegrations.OwnerOfTrait(t) == null).ToList());
        foreach (ModIntegration integration in ModIntegrations.Grouped)
        {
            if (integration.traitDefNames.Length == 0)
            {
                continue;
            }
            DrawTraitGroup(listing, integration, traits.Where(integration.OwnsTrait).ToList());
        }

        listing.Gap(SectionGap);
    }

    private void DrawTraitGroup(Listing_Standard listing, ModIntegration integration, List<WeaponTraitDef> members)
    {
        if (!DrawGroupHeader(listing, integration, members, t => !IsTraitDisabled((WeaponTraitDef)t), SetTraitEnabled))
        {
            return;
        }
        DrawDefGrid(listing, members, TraitColumns, Text.LineHeight, GameFont.Small, DrawTraitCell);
        listing.Gap(GroupGap);
    }

    // The checkbox reads as "enabled" while the stored set holds the DISABLED ones, so the ref bool is
    // a local written back only on an actual change; that keeps the set free of entries for traits the
    // player never touched. A trait that is the last first-pick candidate for some weapon is held on,
    // suffixed with that weapon's name. The tooltip leads with the trait itself, rendered exactly as
    // its block on a weapon's info card (label, description, effect lines, stat lines; the one
    // renderer in CompUniqueWeapon_TraitStats_Patch), then the setting's explanation; built lazily,
    // only while hovered (ToggleCell's TipSignal). A held-on label with its suffix can outrun a
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
        ToggleCell(cell, label, tooltip, ref enabled, forced, paintable: true);
        if (enabled != wasEnabled)
        {
            SetTraitEnabled(trait, enabled);
        }
    }

    // Resolve() keeps the block's yellow label: adding a TaggedString to the string would yield a
    // TaggedString, and its implicit conversion back to string strips every tag.
    private static string TraitTooltip(WeaponTraitDef trait, string heldOnNote)
    {
        string tooltip = Patches.CompUniqueWeapon_TraitStats_Patch.TraitBlock(trait) + "\n\n"
            + "UMW_TraitEnabledDesc".Translate(trait.label).Resolve();
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
