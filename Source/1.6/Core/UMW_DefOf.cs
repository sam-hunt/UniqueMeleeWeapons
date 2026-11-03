using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Static handles for this mod's own defs that C# needs to reference by identity. Populated by
// RimWorld at startup (fields match defName). Only add defs the code actually looks up.
[DefOf]
public static class UMW_DefOf
{
    // The blood-stained Melee trait — looked up by ThoughtWorker_BloodStainedWeapon
    // to decide whether the wielder's primary weapon carries it.
    public static WeaponTraitDef UMW_BloodStained;

    // The storied Melee trait — looked up by ThoughtWorker_StoriedWeapon
    // to decide whether the wielder's primary weapon carries it.
    public static WeaponTraitDef UMW_Storied;

    // Extra unique-name grammar built on the weapon's material — injected into the
    // naming request by NameGenerator_StuffAdjective_Patch, never referenced from XML.
    public static RulePackDef UMW_NamerStuffAdjectives;

    // The parried-blow battle-log grammar — passed to CreateCombatLog by
    // Verb_MeleeAttackDamage_Parry_Patch, never referenced from XML.
    public static RulePackDef UMW_Combat_Parry;

    // Piledriver's active ability. Looked up by UniqueMeleeWeaponsSettings.ApplyAbilityTuning, which
    // writes the configured cooldown and radius onto the live def; the XML holds only the shipped
    // default. Nothing else in C# references it.
    public static AbilityDef UMW_Earthshake;

    // Storied's active ability and the timed buff it grants. Same reason as UMW_Earthshake:
    // ApplyAbilityTuning writes the configured cooldown, radius and buff duration onto the live defs.
    public static AbilityDef UMW_RallyingCry;
    public static HediffDef UMW_Rallied;

    // The three Royalty-tech traits, looked up by CompUniqueWeapon_DisabledTraits_Patch for the war
    // merchant's scoped ban (ModIntegrations.Royalty lists the same three by defName for the settings
    // window's Royalty trait group). Each def is itself MayRequire-gated
    // on Royalty, so the DefOf fields carry the matching attribute: without the DLC the defs never
    // enter the DefDatabase and these stay null (which the patch treats as "nothing to exclude").
    [MayRequireRoyalty] public static WeaponTraitDef UMW_Monomolecular;
    [MayRequireRoyalty] public static WeaponTraitDef UMW_PlasmaCored;
    [MayRequireRoyalty] public static WeaponTraitDef UMW_ZeusHeaded;

    // Core's longsword, referenced for its LABEL only: the exclude-wood setting's description names
    // a wooden longsword as its example reward, and injecting the def label keeps that example in
    // the player's language for free. Core-owned, so no MayRequire; it is absent from vanilla's
    // ThingDefOf, which is why the handle lives here.
    public static ThingDef MeleeWeapon_LongSword;

    // Core's ritual speech-lines fleck, spawned over each ally a rallying cry reaches
    // (CompAbilityEffect_RallyAllies). Vanilla's own, referenced by identity rather than copied — unlike
    // the shout SoundDefs, a FleckDef needs no cloning to be usable. Core-owned, so no MayRequire; it is
    // absent from vanilla's FleckDefOf, which is why the handle lives here.
    public static FleckDef SpeechLines;

    static UMW_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(UMW_DefOf));
    }
}
