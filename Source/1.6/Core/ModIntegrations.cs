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
// of its section, which is wrong only cosmetically. The list lengths are also the counts a group
// header shows while its mod is missing (DrawGroupHeader), so a stale or missing entry miscounts
// there, again only cosmetically.
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

    // The Keyed key of the group header's tooltip, with the coloured name as {0}. The default
    // ("everything from X in this section") is right for an owner whose rows ARE its content; an
    // owner the rows are only named after, or that is this mod itself, overrides it.
    public readonly string groupDescKey;

    private readonly bool isExpansion;
    private readonly string fallbackNameKey;
    private readonly Color? pickedColor;
    private readonly bool plain;

    private ModIntegration(string packageId, bool isExpansion, string fallbackNameKey, Color? pickedColor,
        bool plain, string[] weaponDefNames, string[] traitDefNames, string groupDescKey)
    {
        this.packageId = packageId;
        this.isExpansion = isExpansion;
        this.fallbackNameKey = fallbackNameKey;
        this.pickedColor = pickedColor;
        this.plain = plain;
        this.weaponDefNames = weaponDefNames ?? Array.Empty<string>();
        this.traitDefNames = traitDefNames ?? Array.Empty<string>();
        this.groupDescKey = groupDescKey ?? "UMW_IntegrationGroupDesc";
    }

    // `plain` keeps the name in the window's default text colour, with no colour tag at all (the
    // headers that are not an optional integration: Core, and this mod itself); it overrides the
    // ExpansionDef colour an expansion would otherwise fall back to.
    public static ModIntegration Expansion(string packageId, Color? pickedColor = null, bool plain = false,
        string[] weaponDefNames = null, string[] traitDefNames = null, string groupDescKey = null)
    {
        return new ModIntegration(packageId, true, null, pickedColor, plain, weaponDefNames, traitDefNames,
            groupDescKey);
    }

    public static ModIntegration Mod(string packageId, string fallbackNameKey, Color? color, bool plain = false,
        string[] weaponDefNames = null, string[] traitDefNames = null, string groupDescKey = null)
    {
        return new ModIntegration(packageId, false, fallbackNameKey, color, plain, weaponDefNames, traitDefNames,
            groupDescKey);
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
            if (installed != null)
            {
                return installed.Name;
            }
            // No fallback key means this mod itself (ModIntegrations.Self), which is always installed;
            // the ContentPack name is the same About.xml name, kept as a belt-and-braces default.
            return fallbackNameKey != null ? fallbackNameKey.Translate().Resolve() : UniqueMeleeWeaponsMod.ContentPack.Name;
        }
    }

    // Null for a plain integration: its name takes the default text colour, untagged.
    public Color? Color => plain ? null : pickedColor ?? ExpansionDef?.primaryColor ?? UnityEngine.Color.white;

    // The name with its colour tag, as a resolved string (the bare name for a plain integration).
    // Callers pass it as a Keyed argument and must keep the result a TaggedString they Resolve(), or
    // a string they Colorize last: the implicit TaggedString-to-string conversion is
    // RawText.StripTags() and drops the tag (family "Engine traps").
    public string ColoredName => Color is Color color ? Name.Colorize(color) : Name;

    public bool OwnsWeapon(Def def) => def != null && Array.IndexOf(weaponDefNames, def.defName) >= 0;

    public bool OwnsTrait(Def def) => def != null && Array.IndexOf(traitDefNames, def.defName) >= 0;
}

public static class ModIntegrations
{
    // The base game, as the group header every WEAPON with no optional dependency sits under (they
    // are unique variants of Core's weapons, as Royalty's are of Royalty's), so every def row has the
    // same indentation whether or not it has one. Owns no listed defs: its members are whatever no
    // other integration claims, which also catches a third-party tagged unique. Always active. Plain:
    // it is not an optional integration, so its name takes the default text colour (a colour per
    // header was tried and read as too many; the family table records both plain names).
    public static readonly ModIntegration Core = ModIntegration.Expansion(
        ModContentPack.CoreModPackageId, plain: true);

    // This mod itself, as the group header the TRAITS of our own design sit under (Core ships no
    // weapon trait, so "Core" would mislabel them): like Core it owns no listed defs and takes
    // whatever no other integration claims. The name is our About.xml's through the same ModLister
    // lookup the optional mods use (always installed, so the Keyed fallback is never needed), the
    // packageId our own content pack's (set before anything can touch this class: the Mod ctor
    // stores it before GetSettings runs ExposeData). Plain, like Core: a yellow from the preview art
    // was tried and, beside Odyssey's and Royalty's colours, made the section read as too many.
    public static readonly ModIntegration Self = ModIntegration.Mod(
        UniqueMeleeWeaponsMod.ContentPack.PackageId, null, null, plain: true,
        groupDescKey: "UMW_OwnTraitsGroupDesc");

    // Royalty: the 1.6/Mods/Royalty root (the unique axe and warhammer, six traits: five ultratech
    // plus the period-neutral lead-lined). The war merchant's scoped ban names the five by DefOf
    // (Patches/CompUniqueWeapon_DisabledTraits_Patch.cs); the legacy allowUltratechTraits migration
    // takes this whole list, lead-lined included (Settings_Traits.cs). Colour hand-picked over the
    // ExpansionDef's badge fill, a dull khaki.
    public static readonly ModIntegration Royalty = ModIntegration.Expansion(
        ModContentPack.RoyaltyModPackageId, pickedColor: ColorLibrary.Gold,
        weaponDefNames: new[] { "UMW_Axe_Unique", "UMW_Warhammer_Unique" },
        traitDefNames: new[]
        {
            "UMW_Monomolecular", "UMW_PlasmaCored", "UMW_ZeusHeaded",
            "UMW_EltexPlated", "UMW_EltexFitted", "UMW_LeadLined",
        });

    // Odyssey is a hard dependency, so its group is always drawn and never locked. It owns the six
    // traits that are melee ports of its own ranged ones (1.6/Defs/WeaponTraitDefs/Melee/, the ones
    // whose header says "port of Odyssey's"), which the Traits section groups under its name so the
    // player can tell them from our designs; the group tooltip says they are ports, not Odyssey's
    // content. Also the name and colour for passing mentions (the earthshake cooldown tooltip). Its
    // ExpansionDef colour is white, hence the hand pick.
    public static readonly ModIntegration Odyssey = ModIntegration.Expansion(
        ModContentPack.OdysseyModPackageId, pickedColor: ColorLibrary.LightPurple,
        traitDefNames: new[] { "UMW_Cumbersome", "UMW_GoldInlay", "UMW_JadeInlay", "UMW_Lightweight",
            "UMW_Ornamental", "UMW_Ugly" },
        groupDescKey: "UMW_OdysseyTraitsGroupDesc");

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

    // The integrations that own def rows, in the order their groups are drawn; a section skips the
    // ones that own nothing of its kind (Odyssey owns traits only).
    public static readonly IReadOnlyList<ModIntegration> Grouped = new[] { Odyssey, Royalty, VfePirates };

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
