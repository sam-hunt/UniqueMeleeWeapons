using System.Reflection;
using System.Xml;
using Verse;

namespace UniqueMeleeWeapons;

// XML patch gate on one of this mod's own boolean settings: applies <match> when the named setting
// is true, <nomatch> when false. Same shape as vanilla's PatchOperationFindMod, so the wrapped
// operations read the same way in the patch file.
//
// Works because RimWorld instantiates Mod subclasses (LoadedModManager.CreateModClasses, which is
// where GetSettings loads the settings file) BEFORE it applies XML patches (ApplyPatches), so by
// the time any PatchOperation runs, UniqueMeleeWeaponsMod.Settings holds the player's saved
// values. The consequence is that a setting gating a patch takes effect on the next load, never
// live: its settings row must say so.
//
// Used to let a player switch a whole compat root off (today: the Vanilla Textures Expanded spear
// compat, 1.6/Mods/VanillaTexturesExpanded/Patches/Spear_Unique_VTE.xml) without touching their mod
// list, so a def ends up carrying exactly its shipped values plus nothing - which is also what lets
// PawnRenderUtility_DrawCarriedWeapon_Patch see no consumer and stay unapplied.
//
// `setting` names a public bool field on UniqueMeleeWeaponsSettings (the field, not a Keyed key);
// a misspelt or non-bool name is a loud error and applies neither branch.
public class PatchOperation_UMWSetting : PatchOperation
{
    public string setting;
    public PatchOperation match;
    public PatchOperation nomatch;

    protected override bool ApplyWorker(XmlDocument xml)
    {
        FieldInfo field = string.IsNullOrEmpty(setting)
            ? null
            : typeof(UniqueMeleeWeaponsSettings).GetField(setting, BindingFlags.Public | BindingFlags.Instance);
        if (field == null || field.FieldType != typeof(bool) || UniqueMeleeWeaponsMod.Settings == null)
        {
            Log.Error($"[Unique Melee Weapons] PatchOperation_UMWSetting: no public bool setting named '{setting}'.");
            return false;
        }

        bool enabled = (bool)field.GetValue(UniqueMeleeWeaponsMod.Settings);
        if (enabled)
        {
            return match == null || match.Apply(xml);
        }
        return nomatch == null || nomatch.Apply(xml);
    }

    public override string ToString() => $"{base.ToString()}({setting})";
}
