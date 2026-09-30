using HarmonyLib;
using RimWorld;

namespace UniqueMeleeWeapons.Patches;

// Lifts CompArt's quality bar (Props.minQualityForArtistic, Excellent on every weapon def) for
// weapons whose rolled traits carry a ForcedArtExtension, so art initialization succeeds at any
// quality. Trait order is safe at generation: CompUniqueWeapon.PostPostMake rolls traits BEFORE it
// calls SetQuality -> InitializeArt, so the scan already sees the trait (decompile-verified).
// A def with no CompQuality (the VFEP warcasket pair) never comes through here: vanilla's getter
// returns true whenever TryGetQuality fails, so __result is already true and the postfix early-outs.
//
// This getter only decides whether InitializeArtInternal populates or NULLS the art — display
// never consults it: CompInspectStringExtra, GetDescriptionPart and ITab_Art.IsVisible all gate on
// Active. That asymmetry is what makes removal behave: art forced onto a sub-Excellent weapon
// stays visible after the trait is gone (Active still true), and InitializeArtInternal can't wipe
// it later because it early-outs on a non-empty title.
//
// Perf: every CanShowArt caller is a one-shot event (InitializeArtInternal, JustCreatedBy,
// TaleData_Thing.MakeFrom) except JoyGiver_ViewArt's recreation scan, which rejects factionless
// things — all items — before reaching CanShowArt; the __result early-out keeps the remaining
// sub-Excellent-sculpture case to one failed GetComp.
[HarmonyPatch(typeof(CompArt), nameof(CompArt.CanShowArt), MethodType.Getter)]
public static class CompArt_ForcedArt_Patch
{
    public static void Postfix(CompArt __instance, ref bool __result)
    {
        if (!__result)
        {
            __result = ForcedArtUtility.HasForcedArtTrait(__instance.parent);
        }
    }
}
