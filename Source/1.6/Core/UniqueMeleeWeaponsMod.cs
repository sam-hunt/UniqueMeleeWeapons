using HarmonyLib;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

/// <summary>
/// Mod entry point. Wires up settings and applies all Harmony patches at startup.
/// Add patch classes under the UniqueMeleeWeapons.Patches namespace; <c>PatchAll</c>
/// discovers them automatically via their <c>[HarmonyPatch]</c> attributes.
/// </summary>
public class UniqueMeleeWeaponsMod : Mod
{
    public static UniqueMeleeWeaponsSettings Settings { get; private set; }

    public UniqueMeleeWeaponsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<UniqueMeleeWeaponsSettings>();
        var harmony = new Harmony("shunter.uniquemeleeweapons");
        harmony.PatchAll();
        Log.Message($"[Unique Melee Weapons] Initialized with {harmony.GetPatchedMethods().EnumerableCount()} patches.");
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory() => "Unique Melee Weapons";
}
