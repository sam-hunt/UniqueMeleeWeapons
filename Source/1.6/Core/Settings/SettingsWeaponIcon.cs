using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// The coloured weapon icon beside each row of the Weapons settings section: the weapon's own UI
// icon drawn through its mask with a sample accent (colour one, the red mask channel) and its
// default material's tint (colour two, green), so the row previews what a rolled unique can look
// like rather than the bare untinted diffuse.
//
// How it draws, and why this level and no lower. Vanilla builds every masked def icon the same way
// (BuildableDef.ResolveIcon, decompile-verified RimWorld 1.6 and 1.7): it takes the MaterialRequest
// behind the def's world material back out of MaterialPool, swaps the shader for the UI twin
// ShaderDatabase pairs with it (CutoutComplexUI; for the warcasket pair our mix shader's UI twin,
// or vanilla's if ShaderDatabase_LoadShader_Fallback_Patch swapped it), and caches the result as
// ThingDef.uiIconMaterial. Widgets.ThingIcon then draws that material with
// Widgets.DrawTextureFitted -> GenUI.DrawTextureWithMaterial, which is also what the info card,
// inventory and stockpile icons of every stuff-tinted item in the game go through. This file does
// exactly that and nothing more: re-request the def's uiIconMaterial with our two colours written
// into the request, let MaterialPool hand back (and cache) the material, and draw it through the
// same Widgets call. No RenderTexture, no GL, no Graphics.Blit, no hand-made Material: Unique
// Weapons Unbound rendered its preview through GL.Clear plus Graphics.Blit into a RenderTexture
// from inside a window draw, a path vanilla itself only exercises in the gravship capture, and it
// broke for some players on RimWorld 1.7's engine while never reproducing locally; its fix
// (UWU e4008b3) was to draw through Widgets.ThingIcon, the path this file stays on.
//
// Colour one on a stuffable weapon is the gold inlay (UMW_GoldInlay's forcedColor, the same
// ColorDef the trait forces in play), over colour two as the stuff tint its default material
// gives it (what Widgets.ThingIcon shows for any stuffable item): a gold-inlaid steel blade, the
// one look every stuffable unique can roll. A stuff-less unique (the warcasket pair) keeps the
// body colour its def declares, since ResolveIcon already put that into the request, and takes a
// deterministic accent from the pool CompUniqueWeapon rolls from (ColorType.Weapon,
// randomlyPickable; decompile-verified 1.7), hashed from the defName so it is stable across
// sessions.
//
// Scale: the def's own GenUI.IconDrawScale is NOT used as-is, because it folds in the world
// drawSize (the warcasket pair inherit 1.7 from VFE Pirates against vanilla melee's 1.2
// uiIconScale) and drew them far larger than the rest. Every icon is instead drawn at IconScale
// for a def at the vanilla longsword's draw scale, and the others normalised by the ratio of
// theirs to it, which brings the pair down to the same footprint from the same def data.
//
// The cache is keyed by defName and holds the def instance it was built from: a play-data reload
// replaces every def (and its uiIconMaterial) in place, so an entry built from an older instance
// is rebuilt on first draw rather than drawing a material whose textures were unloaded. The cost
// is one MaterialPool lookup per weapon per load.
public static class SettingsWeaponIcon
{
    private static readonly Dictionary<string, (ThingDef def, Material mat)> cache = new();

    // The draw scale of a weapon at the vanilla longsword's draw scale. The slot is IconSlotAspect
    // (2) times wider than tall and the texture is square, so DrawTextureFitted sizes it by the
    // slot's height and this scale then stretches the horizontal art out across the slot's width;
    // the art's own transparent margin keeps the blade inside the row.
    private const float IconScale = 1.6f;

    // Draws the weapon's icon fitted into `rect` at the given alpha (the row dims it while the
    // weapon is off). Falls back to Widgets.ThingIcon, which tints the whole icon with the stuff
    // colour through GUI.color, when the def has no UI material (its UI shader failed to load).
    public static void Draw(Rect rect, ThingDef weapon, float alpha)
    {
        if (weapon.uiIcon == null || weapon.uiIcon == BaseContent.BadTex)
        {
            return;
        }
        float scale = IconScale * GenUI.IconDrawScale(UMW_DefOf.MeleeWeapon_LongSword) / GenUI.IconDrawScale(weapon);
        Material mat = MaterialFor(weapon);
        if (mat == null)
        {
            Widgets.ThingIcon(rect, weapon, GenStuff.DefaultStuffFor(weapon), scale: scale, alpha: alpha);
            return;
        }
        // The material carries both colours; GUI.color would multiply the whole icon on top of them
        // (what ThingIcon sets it to for a material-drawn icon).
        Color prevColor = GUI.color;
        GUI.color = Color.white;
        Widgets.DrawTextureFitted(rect, weapon.uiIcon, scale,
            new Vector2(weapon.uiIcon.width, weapon.uiIcon.height), new Rect(0f, 0f, 1f, 1f),
            weapon.uiIconAngle, mat, alpha);
        GUI.color = prevColor;
    }

    private static Material MaterialFor(ThingDef weapon)
    {
        if (cache.TryGetValue(weapon.defName, out (ThingDef def, Material mat) entry) && entry.def == weapon)
        {
            return entry.mat;
        }
        Material mat = null;
        if (weapon.uiIconMaterial != null && MaterialPool.TryGetRequestForMat(weapon.uiIconMaterial, out MaterialRequest request))
        {
            if (weapon.MadeFromStuff)
            {
                request.color = UMW_DefOf.UMW_GoldInlay.forcedColor?.color ?? Color.white;
                request.colorTwo = weapon.GetColorForStuff(GenStuff.DefaultStuffFor(weapon));
            }
            else
            {
                request.color = AccentFor(weapon);
            }
            mat = MaterialPool.MatFrom(request);
        }
        cache[weapon.defName] = (weapon, mat);
        return mat;
    }

    // Sorted by defName before hashing so the pick does not depend on def load order.
    private static Color AccentFor(ThingDef weapon)
    {
        List<ColorDef> pool = DefDatabase<ColorDef>.AllDefs
            .Where(c => c.colorType == ColorType.Weapon && c.randomlyPickable)
            .OrderBy(c => c.defName)
            .ToList();
        if (pool.Count == 0)
        {
            return Color.white;
        }
        int index = GenText.StableStringHash(weapon.defName) % pool.Count;
        if (index < 0)
        {
            index += pool.Count;
        }
        return pool[index].color;
    }
}
