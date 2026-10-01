// Batch-mode entry points that build the UMW shader bundle, one per RimWorld OS.
//
// Invoked by ../build.sh as
//   Unity.exe -batchmode -nographics -quit -projectPath <mirror> -executeMethod UMW.BuildAssetBundles.BuildWindows
// and writes <project>/Output/<suffix>/umw_shaders_<suffix>. RimWorld's ModAssetBundlesHandler loads
// only the file whose _win/_mac/_linux suffix matches the running OS, so the three bundles can sit
// side by side in 1.6/Mods/VanillaFactionsExpandedPirates/AssetBundles/.
//
// Which graphics APIs get compiled into the bundle is decided by the PlayerSettings of THIS project
// for the build target, not by anything RimWorld does, so they are set explicitly here rather than
// left to "Auto". A shader missing the API the player runs on is unsupported at load and
// ShaderDatabase falls back to plain Cutout (no mask, no colour two).
//
// Packages/manifest.json must keep com.unity.modules.assetbundle: without that built-in module the
// build still "succeeds" but writes the bundle without its AssetBundle container object, so the
// game's LoadAsset(path) finds nothing (the only trace is a log line saying the AssetBundle module
// is disabled in the build).
//
// The bundle is rejected (exit code 1) if either shader has a compile error, because
// BuildPipeline.BuildAssetBundles happily packs a broken (magenta) shader and returns success.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UMW
{
    public static class BuildAssetBundles
    {
        const string BundleName = "umw_shaders";

        // Asset paths double as the names ShaderDatabase asks the bundle for:
        // Assets/Data/<packageId>/Materials/<shaderPath>.shader, the second of the two forms
        // ContentFinder<Shader>.TryFindAssetInModBundles asks every mod bundle for (the first uses
        // the mod's folder name, which on the Workshop is a numeric id, so the packageId form is used).
        static readonly string[] ShaderAssets =
        {
            "Assets/Data/shunter.uniquemeleeweapons/Materials/UMW/CutoutComplexMix.shader",
            "Assets/Data/shunter.uniquemeleeweapons/Materials/UMW/CutoutComplexMixUI.shader",
        };

        // Per-OS graphics APIs, matching what the vanilla players and Vehicle Framework's bundles
        // carry (verified against their shipped bundles): Windows is D3D11 only (D3D12 reuses the same DXBC; there is no
        // separate compiled variant), Linux is OpenGLCore + Vulkan, Mac is Metal.
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "win",
            new[] { GraphicsDeviceType.Direct3D11 });

        public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "linux",
            new[] { GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.Vulkan });

        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "mac",
            new[] { GraphicsDeviceType.Metal });

        static void Build(BuildTarget target, string suffix, GraphicsDeviceType[] apis)
        {
            try
            {
                foreach (string path in ShaderAssets)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (shader == null)
                        throw new Exception("Shader asset not found: " + path);
                    var messages = ShaderUtil.GetShaderMessages(shader);
                    foreach (var m in messages)
                        Debug.Log($"[UMW] {path}: {m.severity} line {m.line}: {m.message} {m.messageDetails}");
                    if (ShaderUtil.ShaderHasError(shader))
                        throw new Exception("Shader has compile errors: " + path);
                    Debug.Log($"[UMW] {shader.name}: isSupported={shader.isSupported} passes={shader.passCount}");
                }

                PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                PlayerSettings.SetGraphicsAPIs(target, apis);
                Debug.Log($"[UMW] Graphics APIs for {target}: {string.Join(", ", PlayerSettings.GetGraphicsAPIs(target))}");

                string outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Output", suffix);
                Directory.CreateDirectory(outDir);
                var build = new AssetBundleBuild
                {
                    assetBundleName = BundleName + "_" + suffix,
                    assetNames = ShaderAssets,
                };
                var manifest = BuildPipeline.BuildAssetBundles(outDir, new[] { build },
                    BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle
                    | BuildAssetBundleOptions.StrictMode, target);
                if (manifest == null)
                    throw new Exception("BuildAssetBundles returned null (see the log above)");
                string bundlePath = Path.Combine(outDir, build.assetBundleName);
                Debug.Log($"[UMW] Built {bundlePath} ({new FileInfo(bundlePath).Length} bytes); bundles: {string.Join(", ", manifest.GetAllAssetBundles())}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[UMW] Bundle build FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
