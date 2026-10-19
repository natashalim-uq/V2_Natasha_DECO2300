using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Tools > Fill Filter Looks: sets up the five filter Volume Profiles in Assets/Filters.
public static class FilterLooksBuilder
{
    const string Folder = "Assets/Filters/";

    [MenuItem("Tools/Fill Filter Looks")]
    public static void Fill()
    {
        // Rio de Janeiro
        var rio = Load("Rio de Janerio", "Rio de Janeiro");
        var st = Get<SplitToning>(rio);
        st.shadows.Override(C(110, 60, 170));
        st.highlights.Override(C(240, 130, 70));
        st.balance.Override(-20f);
        var ca = Get<ColorAdjustments>(rio);
        ca.colorFilter.Override(C(255, 220, 235));
        ca.saturation.Override(20f);
        var vg = Get<Vignette>(rio);
        vg.color.Override(C(80, 40, 140));
        vg.intensity.Override(0.3f);
        Save(rio);

        // Clarendon
        var cla = Load("Clarendon");
        st = Get<SplitToning>(cla);
        st.shadows.Override(C(60, 140, 170));
        st.highlights.Override(C(255, 240, 200));
        ca = Get<ColorAdjustments>(cla);
        ca.contrast.Override(20f);
        ca.saturation.Override(25f);
        ca.postExposure.Override(0.2f);
        Save(cla);

        // Gingham
        var gin = Load("Gingham");
        ca = Get<ColorAdjustments>(gin);
        ca.contrast.Override(-25f);
        ca.saturation.Override(-30f);
        ca.colorFilter.Override(C(245, 235, 220));
        Get<WhiteBalance>(gin).temperature.Override(10f);
        Save(gin);

        // Stars
        var sta = Load("Stars");
        ca = Get<ColorAdjustments>(sta);
        ca.postExposure.Override(-0.5f);
        ca.colorFilter.Override(C(200, 210, 255));
        Save(sta);

        // Water
        var wat = Load("Water");
        ca = Get<ColorAdjustments>(wat);
        ca.colorFilter.Override(C(150, 205, 255));
        ca.saturation.Override(-10f);
        Get<WhiteBalance>(wat).temperature.Override(-30f);
        Save(wat);

        AssetDatabase.SaveAssets();
        Debug.Log("Fill Filter Looks: all five profiles are set up in Assets/Filters.");
    }

    static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

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

    static T Get<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (!p.TryGet(out T c))
        {
            c = p.Add<T>(false);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, p);
        }
        c.active = true;
        return c;
    }

    static void Save(VolumeProfile p)
    {
        foreach (var c in p.components) EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(p);
    }
}
