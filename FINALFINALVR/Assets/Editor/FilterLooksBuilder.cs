using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Tools > Fill Filter Looks: sets up the five filter Volume Profiles in Assets/Filters.
// A Volume Profile is a saved set of post-processing effects (colour changes applied to the whole
// image after it's drawn). Each filter is one profile; FilterCarousel swaps between them.
// WHY a script: each filter has several effects with exact colour numbers. Setting them by code makes
// them consistent and quick to redo, instead of clicking through dozens of Inspector fields.
// The colour values were picked to roughly copy the feel of the real Instagram filters of the same name.
// (Editor-only helper, run from the Tools menu.)
public static class FilterLooksBuilder
{
    const string Folder = "Assets/Filters/";

    [MenuItem("Tools/Fill Filter Looks")]
    public static void Fill()
    {
        // Rio de Janeiro: purple shadows, warm orange highlights, a bit more colour, purple edges
        // Split Toning = tint the dark parts one colour and the bright parts another
        var rio = Load("Rio de Janerio", "Rio de Janeiro");   // two spellings, because the asset was named with a typo
        var st = Get<SplitToning>(rio);
        st.shadows.Override(C(110, 60, 170));
        st.highlights.Override(C(240, 130, 70));
        st.balance.Override(-20f);
        var ca = Get<ColorAdjustments>(rio);
        ca.colorFilter.Override(C(255, 220, 235));
        ca.saturation.Override(20f);
        var vg = Get<Vignette>(rio);   // Vignette = darker edges around the view
        vg.color.Override(C(80, 40, 140));
        vg.intensity.Override(0.3f);
        Save(rio);

        // Clarendon: cool blue-teal shadows, bright warm highlights, punchier contrast and colour
        var cla = Load("Clarendon");
        st = Get<SplitToning>(cla);
        st.shadows.Override(C(60, 140, 170));
        st.highlights.Override(C(255, 240, 200));
        ca = Get<ColorAdjustments>(cla);
        ca.contrast.Override(20f);
        ca.saturation.Override(25f);
        ca.postExposure.Override(0.2f);   // slightly brighter
        Save(cla);

        // Gingham: soft and faded (less contrast and colour), slightly warm
        var gin = Load("Gingham");
        ca = Get<ColorAdjustments>(gin);
        ca.contrast.Override(-25f);
        ca.saturation.Override(-30f);
        ca.colorFilter.Override(C(245, 235, 220));
        Get<WhiteBalance>(gin).temperature.Override(10f);   // + = warmer (more yellow)
        Save(gin);

        // Stars: darker and slightly blue, like night time
        var sta = Load("Stars");
        ca = Get<ColorAdjustments>(sta);
        ca.postExposure.Override(-0.5f);
        ca.colorFilter.Override(C(200, 210, 255));
        Save(sta);

        // Water: blue tint and cooler, like being underwater
        var wat = Load("Water");
        ca = Get<ColorAdjustments>(wat);
        ca.colorFilter.Override(C(150, 205, 255));
        ca.saturation.Override(-10f);
        Get<WhiteBalance>(wat).temperature.Override(-30f);   // - = cooler (more blue)
        Save(wat);

        AssetDatabase.SaveAssets();
        Debug.Log("Fill Filter Looks: all five profiles are set up in Assets/Filters.");
    }

    // Short helper: makes a colour from 0-255 numbers (like a colour picker), since Unity wants 0-1
    static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

    // Loads the profile with the first name that exists; if none exist, creates a new profile file
    static VolumeProfile Load(params string[] names)
    {
        foreach (var n in names)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder + n + ".asset");
            if (p != null) return p;
        }
        var created = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(created, Folder + names[0] + ".asset");
        return created;
    }

    // Gets an effect (e.g. Vignette) from the profile, adding it if the profile doesn't have it yet.
    // <T> means it works for any effect type, so one method covers SplitToning, Vignette, etc.
    static T Get<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (!p.TryGet(out T c))
        {
            c = p.Add<T>(false);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            // Save the new effect inside the profile file, otherwise it would be lost when Unity restarts
            AssetDatabase.AddObjectToAsset(c, p);
        }
        c.active = true;
        return c;
    }

    // Marks the profile and its effects as changed, so SaveAssets writes them to disk
    static void Save(VolumeProfile p)
    {
        foreach (var c in p.components) EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(p);
    }
}
