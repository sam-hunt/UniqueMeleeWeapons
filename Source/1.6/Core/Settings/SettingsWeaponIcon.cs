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
// Colour one is a deterministic pick per weapon from the same pool CompUniqueWeapon rolls from
// (ColorType.Weapon, randomlyPickable; decompile-verified 1.7), hashed from the defName so a
// weapon keeps its accent across sessions and the list reads varied rather than uniform. Colour
// two is the stuff tint the weapon's default material would give it (what Widgets.ThingIcon
// shows for any stuffable item); a stuff-less unique keeps the body colour its def declares,
// since ResolveIcon already put that into the request.
//
// The cache is keyed by defName and holds the def instance it was built from: a play-data reload
// replaces every def (and its uiIconMaterial) in place, so an entry built from an older instance
// is rebuilt on first draw rather than drawing a material whose textures were unloaded. The cost
// is one MaterialPool lookup per weapon per load.
public static class SettingsWeaponIcon
{
    private static readonly Dictionary<string, (ThingDef def, Material mat)> cache = new();

    // Draws the weapon's icon fitted into `rect` at the given alpha (the row dims it while the
    // weapon is off). Falls back to Widgets.ThingIcon, which tints the whole icon with the stuff
    // colour through GUI.color, when the def has no UI material (its UI shader failed to load).
    public static void Draw(Rect rect, ThingDef weapon, float alpha)
    {
        if (weapon.uiIcon == null || weapon.uiIcon == BaseContent.BadTex)
        {
            return;
        }
        Material mat = MaterialFor(weapon);
        if (mat == null)
        {
            Widgets.ThingIcon(rect, weapon, GenStuff.DefaultStuffFor(weapon), alpha: alpha);
            return;
        }
        // The material carries both colours; GUI.color would multiply the whole icon on top of them
        // (what ThingIcon sets it to for a material-drawn icon).
        Color prevColor = GUI.color;
        GUI.color = Color.white;
        Widgets.DrawTextureFitted(rect, weapon.uiIcon, GenUI.IconDrawScale(weapon),
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
            request.color = AccentFor(weapon);
            if (weapon.MadeFromStuff)
            {
                request.colorTwo = weapon.GetColorForStuff(GenStuff.DefaultStuffFor(weapon));
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
