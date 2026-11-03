using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// The optional mods and DLCs this mod integrates with, as the settings window sees them: for each,
// how to tell whether it is active, what to call it, what colour its name takes, and which of our
// defs it owns. Everything here exists so the window can draw a row ABOUT an integration while the
// integration is absent (family rule: a gated row that offers content is never hidden), which no
// def can do for itself because an absent def has no label and a present def cannot say which load
// root shipped it (every compat root shares this mod's ContentPack).
//
// The def lists are therefore static. They must agree with the ThingDefs/WeaponTraitDefs under the
// matching 1.6/Mods/<Name>/ root (1.6/Mods/CLAUDE.md, "Package ids live in several places that must
// agree"); a def listed here but absent while its mod is active simply draws no row, so a stale
// entry fails soft. A def under a compat root that is NOT listed here falls into the ungrouped rows
// of its section, which is wrong only cosmetically.
//
// Names and colours follow the family's settings rules (RimworldModTemplate,
// Source/<version>/Settings/CLAUDE.md, "Mod and DLC colours"). A DLC's name comes from its
// ExpansionDef: Core ships every ExpansionDef whether or not the DLC is owned, so the lookup resolves
// with the DLC absent and the label is vanilla-localized. The lookup is our own, case-insensitive
// one: ModLister.GetExpansionWithIdentifier compares linkedMod ("Ludeon.RimWorld.Royalty") to its
// argument exactly, so the lowercase packageId constants ModContentPack publishes never match it
// (decompile-verified, RimWorld 1.7). A mod's name comes from its About.xml when
// it is installed (active or not) and from a Keyed fallback when it is not installed at all, since
// nothing else in the game then knows the name. Activity is the suffix-ignoring ModLister test the
// root CLAUDE.md mandates, never ModsConfig.IsActive; for a DLC it is the same test on its packageId.
public sealed class ModIntegration
{
    public readonly string packageId;
    public readonly string[] weaponDefNames;
    public readonly string[] traitDefNames;

    private readonly bool isExpansion;
    private readonly string fallbackNameKey;
    private readonly Color? pickedColor;

    private ModIntegration(string packageId, bool isExpansion, string fallbackNameKey, Color? pickedColor,
        string[] weaponDefNames, string[] traitDefNames)
    {
        this.packageId = packageId;
        this.isExpansion = isExpansion;
        this.fallbackNameKey = fallbackNameKey;
        this.pickedColor = pickedColor;
        this.weaponDefNames = weaponDefNames ?? Array.Empty<string>();
        this.traitDefNames = traitDefNames ?? Array.Empty<string>();
    }

    public static ModIntegration Expansion(string packageId, Color? pickedColor = null,
        string[] weaponDefNames = null, string[] traitDefNames = null)
    {
        return new ModIntegration(packageId, true, null, pickedColor, weaponDefNames, traitDefNames);
    }

    public static ModIntegration Mod(string packageId, string fallbackNameKey, Color color,
        string[] weaponDefNames = null, string[] traitDefNames = null)
    {
        return new ModIntegration(packageId, false, fallbackNameKey, color, weaponDefNames, traitDefNames);
    }

    public bool IsActive => ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null;

    private ExpansionDef ExpansionDef
    {
        get
        {
            if (!isExpansion)
            {
                return null;
            }
            foreach (ExpansionDef expansion in ModLister.AllExpansions)
            {
                if (string.Equals(expansion.linkedMod, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    return expansion;
                }
            }
            return null;
        }
    }

    // The plain, uncoloured display name. Coloured() is what settings text should inject.
    public string Name
    {
        get
        {
            if (ExpansionDef is ExpansionDef expansion)
            {
                return expansion.label;
            }
            ModMetaData installed = ModLister.GetModWithIdentifier(packageId, ignorePostfix: true);
            return installed?.Name ?? fallbackNameKey.Translate().Resolve();
        }
    }

    public Color Color => pickedColor ?? ExpansionDef?.primaryColor ?? Color.white;

    // The name with its colour tag, as a resolved string. Callers pass it as a Keyed argument and
    // must keep the result a TaggedString they Resolve(), or a string they Colorize last: the
    // implicit TaggedString-to-string conversion is RawText.StripTags() and drops the tag (family
    // "Engine traps").
    public string ColoredName => Name.Colorize(Color);

    public bool OwnsWeapon(Def def) => def != null && Array.IndexOf(weaponDefNames, def.defName) >= 0;

    public bool OwnsTrait(Def def) => def != null && Array.IndexOf(traitDefNames, def.defName) >= 0;
}

public static class ModIntegrations
{
    // The base game, as the group header every def with no optional dependency sits under, so every
    // def row has the same indentation whether or not it has one. Owns no listed defs: its members
    // are whatever no other integration claims, which also catches a third-party tagged unique.
    // Always active. Colour hand-picked (its ExpansionDef has none).
    public static readonly ModIntegration Core = ModIntegration.Expansion(
        ModContentPack.CoreModPackageId, pickedColor: ColorLibrary.LightBrown);

    // Royalty: the 1.6/Mods/Royalty root (the unique axe and warhammer, the three ultratech traits).
    // The trait list is also what the war merchant's scoped ban and the legacy allowUltratechTraits
    // migration name (Patches/CompUniqueWeapon_DisabledTraits_Patch.cs, Settings_Traits.cs). Colour
    // hand-picked over the ExpansionDef's badge fill, a dull khaki.
    public static readonly ModIntegration Royalty = ModIntegration.Expansion(
        ModContentPack.RoyaltyModPackageId, pickedColor: ColorLibrary.Gold,
        weaponDefNames: new[] { "UMW_Axe_Unique", "UMW_Warhammer_Unique" },
        traitDefNames: new[] { "UMW_Monomolecular", "UMW_PlasmaCored", "UMW_ZeusHeaded" });

    // Odyssey is a hard dependency, listed only for its name and colour in passing mentions
    // (the earthshake cooldown tooltip). Its ExpansionDef colour is white, hence the hand pick.
    public static readonly ModIntegration Odyssey = ModIntegration.Expansion(
        ModContentPack.OdysseyModPackageId, pickedColor: ColorLibrary.LightPurple);

    // VFE Pirates: the 1.6/Mods/VanillaFactionsExpandedPirates root (the two warcasket uniques).
    public static readonly ModIntegration VfePirates = ModIntegration.Mod(
        Patches.ShaderDatabase_LoadShader_Fallback_Patch.VfepPackageId,
        "UMW_ModNameVfePirates", ColorLibrary.Rose,
        weaponDefNames: new[] { "UMW_WarcasketBroadsword_Unique", "UMW_WarcasketGravityHammer_Unique" });

    // VTE owns no defs of ours: its root is a pose patch with an opt-out row in the Compatibility
    // section, which hides with the mod (Settings_Compat.cs says why that is the sanctioned
    // exception). Listed for the name and colour that row injects.
    public static readonly ModIntegration VanillaTexturesExpanded = ModIntegration.Mod(
        UniqueMeleeWeaponsSettings.VtePackageId, "UMW_ModNameVte", ColorLibrary.LightOrange);

    // The integrations that own def rows, in the order their groups are drawn.
    public static readonly IReadOnlyList<ModIntegration> Grouped = new[] { Royalty, VfePirates };

    public static ModIntegration OwnerOfWeapon(Def def)
    {
        foreach (ModIntegration integration in Grouped)
        {
            if (integration.OwnsWeapon(def))
            {
                return integration;
            }
        }
        return null;
    }

    public static ModIntegration OwnerOfTrait(Def def)
    {
        foreach (ModIntegration integration in Grouped)
        {
            if (integration.OwnsTrait(def))
            {
                return integration;
            }
        }
        return null;
    }
}
