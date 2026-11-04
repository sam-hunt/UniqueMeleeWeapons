using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Makes the pawn's stat breakdown list the primary weapon under "Relevant gear" when a weapon-thing
// stat OFFSET reaches the pawn through StatPart_GearStatOffset (Eltex-plated's psychic sensitivity
// and neural heat recovery offsets, Eltex-fitted's neural heat limit offset), the way it already
// does for the matching factors (Eltex-fitted's and Lead-lined's sensitivity factors).
//
// THE GAP (decompile-verified, identical on the pinned 1.6 reference and the 1.7 install).
// StatPart_GearStatOffset.TransformValue adds the weapon's Primary.GetStatValue(apparelStat) plus
// StatWorker.StatOffsetFromGear(Primary, apparelStat), but PawnWearingRelevantGear, the gate
// ExplanationPart runs before it lists any gear, tests the weapon by StatOffsetFromGear ALONE
// (def-level equippedStatOffsets and bladelink traits), never by its GetStatValue. So a wielder
// with no eltex apparel sees the offset in the total and no line explaining it, while wearing one
// eltex garment opens the gate and the weapon line prints (the line itself does use GetStatValue).
// StatPart_GearStatFactor's gate uses GetStatValue, so the factor lines were never affected.
// Vanilla's own bioferrite weapons have the same gap. The postfix opens the gate on the same test
// the line uses, nothing else: what prints is vanilla's line with vanilla's number, which is also
// the ONLY place Plated's quality-scaled value shows (the trait block prints the def's Good value;
// the weapon stats are showIfUndefined false).
//
// PERF. This is the explanation path only: PawnWearingRelevantGear is private and called from
// ExplanationPart alone, which StatWorker.GetExplanationUnfinalized runs when a stat card or a
// breakdown tooltip is built, a handful of calls per frame while one is open and none otherwise.
// It is NOT the value path (TransformValue), vanilla's tens-of-thousands-per-frame pipeline that
// 250c3c0 and 210ba8a learned to stay out of. The body runs only when vanilla said false and the
// part includes the weapon, and its one stat read is the weapon's own, per-thing cached.
//
// The two private fields are read through FieldRefAccess resolved once. GetInfoCardHyperlinks
// still lists apparel only (vanilla), accepted: the line is the information.
[HarmonyPatch(typeof(StatPart_GearStatOffset), "PawnWearingRelevantGear")]
public static class StatPart_GearStatOffset_WeaponRelevantGear_Patch
{
    private static readonly AccessTools.FieldRef<StatPart_GearStatOffset, StatDef> ApparelStat =
        AccessTools.FieldRefAccess<StatPart_GearStatOffset, StatDef>("apparelStat");

    private static readonly AccessTools.FieldRef<StatPart_GearStatOffset, bool> IncludeWeapon =
        AccessTools.FieldRefAccess<StatPart_GearStatOffset, bool>("includeWeapon");

    public static void Postfix(StatPart_GearStatOffset __instance, Pawn pawn, ref bool __result)
    {
        if (__result || pawn == null || !IncludeWeapon(__instance))
        {
            return;
        }
        ThingWithComps primary = pawn.equipment?.Primary;
        StatDef stat = ApparelStat(__instance);
        if (primary != null && stat != null && primary.GetStatValue(stat) != 0f)
        {
            __result = true;
        }
    }
}
