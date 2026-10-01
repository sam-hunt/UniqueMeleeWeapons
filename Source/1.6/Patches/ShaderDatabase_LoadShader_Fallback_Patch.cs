using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Guards the mod's own shaders (UMW/CutoutComplexMix and its UI twin, shipped in the asset
// bundles under 1.6/Mods/VanillaFactionsExpandedPirates/AssetBundles/ and named by the
// ShaderTypeDefs beside them in that compat root's Defs/ShaderTypeDefs/) so that when they
// cannot be used the warcasket uniques fall back to vanilla CutoutComplex, which still applies
// both mask channels, and never to vanilla's default fallback, plain Cutout, which drops the
// mask and colour two entirely. This file is the full rationale; the ShaderTypeDefs and
// CLAUDE.md carry only pointers here.
//
// Scope: only the two VFE Pirates warcasket uniques draw with the mix shader (their masks have
// feathered red|green edges, which vanilla's stacked tints render lighter than painted); every
// other unique keeps vanilla CutoutComplex, where hard-edged masks render bit-identically. The
// shader therefore ships only from the VFE Pirates compat load root, and this patch is applied
// only while that mod is active (see Apply): without it, no bundle is loaded, no ShaderTypeDef
// names our paths, and the game runs with no detour on ShaderDatabase at all.
//
// What the prefix touches: nothing unless one of the two paths in the call is ours. Every other
// LoadShader call (vanilla's, other mods', and Unique Weapons Unbound's or any tag-carrying
// third-party weapon's CutoutComplex) returns at the first branch with its arguments untouched.
// The cache eviction removes only our two keys.
//
// Why vanilla's own fallback is unacceptable (decompile-verified, RimWorld 1.6):
// ShaderDatabase.TryLoadShader looks a path up in Resources, then in every running mod's
// bundles (ContentFinder<Shader>.TryFindAssetInModBundles), caches the result in the private
// static `lookup` dictionary, and on a miss warns and substitutes ShaderDatabase.DefaultShader
// (= Cutout). It never checks Shader.isSupported, so a bundle that lacks the running graphics
// API (a platform we could not build for, or a player forcing an API such as -force-glcore on
// Windows) would load a shader Unity cannot run. A masked unique drawn with Cutout shows no
// colour two, so "the weapon lost its paint" is what the player sees.
//
// Two ways the load can fail:
//  1. Missing or unsupported: the OS bundle is absent (ModAssetBundlesHandler loads only the
//     _win/_mac/_linux file matching the OS), the shader is not in it, or the compiled
//     programs do not cover the active graphics device. Probed here with the same finder
//     vanilla uses plus Shader.isSupported.
//  2. Stale after a play-data reload: a mid-session language change runs ClearAllPlayData,
//     whose ModAssetBundlesHandler.ClearDestroy unloads every mod bundle with
//     Unload(unloadAllLoadedObjects: true), destroying the Shader objects, but the `lookup`
//     cache is never cleared. The reload rebuilds every ThingDef's graphic through fresh
//     ShaderTypeDef instances, TryLoadShader finds the key already present, and the destroyed
//     object fails its `== null` check: the weapons silently drop to Cutout with a "Could not
//     load shader" warning. Vanilla's own shaders never hit this because they live in
//     Resources, which survive the reload. Evicting our two keys before every load makes the
//     original re-probe the freshly loaded bundle; the cost is one LoadAsset per shader per load.
//
// Why a prefix on the 4-argument LoadShader(shaderPath, uiShaderPath, out, out) and not a
// def-write in UMW_Startup.Run: ShaderTypeDef.Shader resolves lazily through this overload,
// and every ThingDef's graphic and uiIconMaterial is built in PostLoad delegates that run
// before StaticConstructorOnStartupUtility.CallAll (see
// StaticConstructorOnStartupUtility_CallAll_Patch), so by the time our startup runs the
// materials already hold whatever shader this method returned. The prefix fires on the main
// thread, after the bundles are in (mod content is the first ExecuteWhenFinished delegate of
// the load), for each of our two ShaderTypeDefs, once per play-data load.
//
// Both paths swap together. ShaderDatabase pairs a map shader with its UI twin in `uiLookup`
// keyed by the loaded Shader object; if only the UI path were rewritten, the mix map shader
// would be paired with vanilla CutoutComplexUI (a visible map/info-card mismatch), and if
// only the map path were, vanilla CutoutComplex would get no UI twin and Widgets.ThingIcon
// would fall back to an untinted GUI.DrawTexture. So any failure of either shader rewrites
// every one of our paths in the call to its vanilla counterpart, and the original method then
// resolves and pairs those exactly as it does for an ordinary CutoutComplex weapon.
//
// Patch timing: no [HarmonyPatch] attribute, so PatchAll skips it; Apply runs from the Mod
// constructor right after PatchAll, gated on ModsConfig.IsActive. That is safe and early
// enough on both counts. The active mod list is fixed for the life of the process (the game
// restarts to change it) and is already final when Mod subclasses are constructed:
// LoadedModManager.LoadAllActiveMods runs InitializeMods (which evaluates LoadFolders'
// IfModActive through the same ModsConfig.IsActive) before CreateModClasses. And the first
// 4-argument LoadShader call for a ShaderTypeDef happens in def PostLoad, well after every Mod
// constructor. It cannot wait for UMW_Startup.Run like the carried-weapon patch, for the
// materials-before-CallAll reason above. ShaderDatabase is a vanilla type whose static
// initializer only loads vanilla shaders from Resources on the main thread and touches no
// defs, so triggering it from the constructor is harmless (the foreign-cctor hazard in
// CLAUDE.md does not apply).
//
// The probe and warning fire once per load in practice: only UMW_CutoutComplexMix is named by
// a graphicData.shaderType, so only its ShaderTypeDef.Shader is ever resolved (it names both
// paths in one call); UMW_CutoutComplexMixUI exists for addressability and is not read.
public static class ShaderDatabase_LoadShader_Fallback_Patch
{
    // Must equal the IfModActive id of the VFE Pirates compat roots in LoadFolders.xml: the
    // shader's bundles and ShaderTypeDefs load from those roots, and this patch exists only
    // for them.
    public const string VfepPackageId = "OskarPotocki.VFE.Pirates";

    // Must match <shaderPath>/<uiShaderPath> in Defs/ShaderTypeDefs/CutoutComplexMix.xml and
    // CutoutComplexMixUI.xml, and the asset paths built into the bundles by
    // Source/UnityShaders (Assets/Data/shunter.uniquemeleeweapons/Materials/<path>.shader).
    private const string MixPath = "UMW/CutoutComplexMix";
    private const string MixUIPath = "UMW/CutoutComplexMixUI";

    // Vanilla's Resources paths for the pair we degrade to (ShaderDatabase's own field initialisers).
    private const string VanillaMapPath = "Map/CutoutComplex";
    private const string VanillaUIPath = "Map/CutoutComplexUI";

    private static readonly FieldInfo LookupField = AccessTools.Field(typeof(ShaderDatabase), "lookup");
    private static bool warnedNoLookup;

    // Called once, from the Mod constructor (timing in the header). Returns without patching
    // when VFE Pirates is not active, since nothing then names our shader paths.
    public static void Apply(Harmony harmony)
    {
        if (!ModsConfig.IsActive(VfepPackageId))
        {
            return;
        }
        var original = AccessTools.Method(typeof(ShaderDatabase), nameof(ShaderDatabase.LoadShader),
            new[] { typeof(string), typeof(string), typeof(Shader).MakeByRefType(), typeof(Shader).MakeByRefType() });
        harmony.Patch(original, prefix: new HarmonyMethod(typeof(ShaderDatabase_LoadShader_Fallback_Patch), nameof(Prefix)));
        Log.Message("[Unique Melee Weapons] VFE Pirates active; patched ShaderDatabase.LoadShader to guard the mix shader.");
    }

    public static void Prefix(ref string shaderPath, ref string uiShaderPath)
    {
        bool mapIsOurs = IsOurs(shaderPath);
        bool uiIsOurs = IsOurs(uiShaderPath);
        if (!mapIsOurs && !uiIsOurs)
        {
            return;
        }

        // (2) Drop any cached entry for our paths so the original re-probes the live bundle.
        // Resolved by name, so a vanilla rename would silently bring the reload bug back:
        // say so once rather than skip quietly.
        if (LookupField?.GetValue(null) is Dictionary<string, Shader> lookup)
        {
            lookup.Remove(MixPath);
            lookup.Remove(MixUIPath);
        }
        else if (!warnedNoLookup)
        {
            warnedNoLookup = true;
            Log.Warning("[Unique Melee Weapons] ShaderDatabase.lookup not found; unique weapons may lose their " +
                        "mask after a mid-session language change until the game is restarted.");
        }

        // (1) Probe every path the call names; one failure degrades the whole call.
        string failed = (mapIsOurs && !IsUsable(shaderPath)) ? shaderPath
            : (uiIsOurs && !IsUsable(uiShaderPath)) ? uiShaderPath
            : null;
        if (failed == null)
        {
            return;
        }

        Log.Warning($"[Unique Melee Weapons] Shader {failed} is missing from the mod's asset bundle for " +
                    $"this OS or unsupported on this graphics device ({SystemInfo.graphicsDeviceType}). " +
                    "Unique Melee Weapons will use vanilla CutoutComplex instead. Feel free to report this " +
                    "along with your OS and graphics device.");
        if (mapIsOurs) shaderPath = VanillaFor(shaderPath);
        if (uiIsOurs) uiShaderPath = VanillaFor(uiShaderPath);
    }

    private static bool IsOurs(string path) => path == MixPath || path == MixUIPath;

    private static string VanillaFor(string path) => path == MixPath ? VanillaMapPath : VanillaUIPath;

    private static bool IsUsable(string path)
    {
        // Plain C# null test (?.) is fine here even though Shader is a UnityEngine.Object: the lookup
        // loads straight from the live bundles (no cache, decompile-verified), so a non-null result is
        // never a destroyed object — those live only in ShaderDatabase's cache, evicted above.
        Shader shader = ContentFinder<Shader>.TryFindAssetInModBundles(path);
        return shader?.isSupported == true;
    }
}
