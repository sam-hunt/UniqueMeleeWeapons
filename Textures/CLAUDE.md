# Art and mask rules (`Textures/`, `Mods/*/Textures/`, `Source/UnityShaders/`)

Textures follow Odyssey: per-weapon folder, variants `Unique<Weapon><Letter>.png` plus a matching
`_m` mask; the def's `texPath` names the folder. Art is not version-scoped, so it lives at the repo
root, and a DLC-gated weapon's art goes under `Mods/<DLC>/Textures/` (`1.6/Mods/CLAUDE.md`).

- **Stuffable uniques are double-masked** (`Source/1.6/Things/UniqueMeleeWeapon.cs`): mask **red**
  is colour one (the unique accent, supplied by vanilla), mask **green** is colour two (the
  material tint, or a trait-forced body colour). This is the load-bearing trick of the mod;
  Odyssey's ranged uniques are not stuffable and don't need it. **Art rule:** the weapon silhouette
  must be all red/green with **no black** (black means "not painted" and would ignore the material
  entirely), and the diffuse must stay light and neutral so the multiply yields a clean tint. With
  only two channels, a forced body colour *replaces* the material tint: one body-colour trait per
  weapon, gated by its exclusion token; it can still co-occur with a colour-one inlay. A
  **non-stuffable** unique (the VFEP warcasket pair) has no material tint, so its body placeholder
  is the def's `graphicData.colorTwo`, which a forced body colour still replaces.
- **A mask edge must sit in dark ink or on a pixel-exact colour edge, never along an anti-aliased
  colour change.** `CutoutComplex` is a per-texel multiply, so a texel on the wrong side of a tint
  edge renders darker or brighter than both neighbours and the mask's staircase reads as a torn,
  dashed seam. Ink hides it (Odyssey keeps ~80% of its tint edges there). Feathering the mask can't
  fix it: no single mask value makes a multiply reproduce a blend for every colour, so the fix is
  in the art. Masks that follow the art rule above are safe by construction; it bites wherever
  untinted art sits beside tinted art, as on the warcasket pair.
- **Compression:** 1.6 compresses to DXT5 at load, storing each 4x4 block as one straight colour
  ramp, so red, green and black sharing a block come out wrong. 1.7 compresses to BC7 by default
  (set in `RimWorld.dll`'s bootstrap, 1.7.4979), which shrinks the artifact but does not remove it.
  Art must still satisfy 1.6. The deploy whitelists only PNG/JPEG; add a `.dds` glob to the
  csproj's Textures line only if a mask ever needs BC7 or lossless storage.
- **The mix shader.** The uniques draw with vanilla `CutoutComplex`, except the warcasket pair,
  whose feathered red|green mask edges draw with our `UMW_CutoutComplexMix`: vanilla's shader with
  the two tints **mixed** instead of **stacked**, bit-identical on pure red, green and black texels
  (a feathered red|green edge is a different class from the anti-alias problem above: the mix
  shader renders it exactly). Its ShaderTypeDefs, bundles and only two consumers live in the VFEP
  compat root; source is `Source/UnityShaders/` (bundles are committed binaries, rebuilt by
  `build.sh`, root `CLAUDE.md`). Hard red|green edges stay the safer default for new art, because
  the fallback below stacks again.
- **The mix shader must fail to vanilla `CutoutComplex`, never `Cutout`** (vanilla's own fallback,
  which drops the mask and colour two).
  `Source/1.6/Patches/ShaderDatabase_LoadShader_Fallback_Patch.cs` is the full rationale: the ways
  the load fails (including a stale cache after a mid-session language change), why it hooks the
  loader rather than `UMW_Startup`, and why the map and UI paths swap together. The shader paths
  live in three places that move together: the ShaderTypeDefs, that patch's constants, and the
  Unity project's asset paths (`Assets/Data/<packageId>/Materials/<path>.shader`).
