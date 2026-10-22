using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

// Tools > Wire Filter Carousel: fills in Filter Carousel on Canvas - Filter and wires the < > Apply Close buttons.
// "Wiring" = filling in the Inspector fields and setting each button's On Click () event.
// WHY: FilterCarousel has a lot of fields (6 icons, names, looks, scenes, labels, volumes). Dragging them
// all in by hand is slow and easy to get wrong, so this finds everything by name and fills it in,
// then reports anything it couldn't find.
// (Editor-only helper, run from the Tools menu.)
public static class FilterCarouselWiring
{
    [MenuItem("Tools/Wire Filter Carousel")]
    public static void Wire()
    {
        GameObject canvas = Find("Canvas - Filter");
        if (canvas == null) { EditorUtility.DisplayDialog("Wire Filter Carousel", "Couldn't find 'Canvas - Filter'.", "OK"); return; }

        var fc = canvas.GetComponent<FilterCarousel>();
        if (fc == null) fc = Undo.AddComponent<FilterCarousel>(canvas);
        Undo.RecordObject(fc, "Wire Filter Carousel");

        // The six filters, in carousel order. The icon objects' names; "Rio de Janerio" has a typo in the
        // scene, and the Water icon was originally called "City", so both spellings are tried.
        string[] icons = { "Original", "Rio de Janerio", "Clarendon", "Gingham", "Stars", "Water" };
        Transform filters = canvas.transform.Find("Filters");
        MakeOriginalIcon(filters);
        // .Select (LINQ) runs FindChild once per name and collects the results into an array
        fc.filterIcons = icons.Select(n => FindChild(filters, n, n == "Water" ? "City" : n == "Rio de Janerio" ? "Rio de Janeiro" : n)).ToArray();
        // The names players see (spelled correctly)
        fc.filterNames = new[] { "Original", "Rio de Janeiro", "Clarendon", "Gingham", "Stars", "Water" };
        fc.filterNameLabel = Text(canvas, "Filter Name");
        fc.appliedLabel = Text(canvas, "Applied Label");
        fc.flashNameLabel = Text(canvas, "Flash Name");
        fc.flashSeconds = 2f;

        GameObject preview = Find("Filter Preview");
        fc.previewRenderers = preview != null ? new[] { preview.GetComponent<Renderer>() } : new Renderer[0];

        // "?." means: only get the Volume if the object was found (otherwise just give null, no error)
        fc.environmentVolume = Find("Post Process Volume")?.GetComponent<Volume>();
        fc.filterVolume = Find("Filter Volume")?.GetComponent<Volume>();

        // One look per filter, same order as the icons. "Original" has none (null), so it shows the room as it is.
        fc.filterLooks = new[] { null, Profile("Rio de Janerio", "Rio de Janeiro"), Profile("Clarendon"), Profile("Gingham"), Profile("Stars"), Profile("Water") };

        // Scene changes: start all six empty, then fill in Stars (index 4) and Water (index 5)
        GameObject[] walls = { Find("Mirror Wall"), Find("Far Wall"), Find("Side Wall") };
        GameObject roomStars = Find("Room Stars");
        fc.filterScenes = new FilterScene[6];
        for (int i = 0; i < 6; i++) fc.filterScenes[i] = new FilterScene { show = new GameObject[0], hide = new GameObject[0] };
        // Stars: night sky, walls hidden (so you see the sky all around), room filled with star particles
        fc.filterScenes[4].skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported Assets/Real Stars Skybox/StarSkybox04/StarSkybox04.mat");
        fc.filterScenes[4].hide = walls.Where(w => w != null).ToArray();
        fc.filterScenes[4].show = roomStars != null ? new[] { roomStars } : new GameObject[0];
        // Water: show the rippling water floor
        GameObject water = Find("Water Floor");
        fc.filterScenes[5].show = water != null ? new[] { water } : new GameObject[0];
        EditorUtility.SetDirty(fc);

        // Wire the four buttons, and build a list of anything missing to report at the end
        string missing = "";
        missing += Hook(canvas, "< btn", fc.PreviousFilter);
        missing += Hook(canvas, "> btn", fc.NextFilter);
        missing += Hook(canvas, "Apply btn", fc.ConfirmSelection);
        missing += Hook(canvas, "Close btn", fc.ClosePanel);
        if (fc.filterIcons.Any(x => x == null)) missing += " a filter icon,";
        if (preview == null) missing += " Filter Preview,";
        if (fc.filterVolume == null) missing += " Filter Volume,";
        if (fc.filterLooks.Skip(1).Any(x => x == null)) missing += " a Volume Profile,";
        if (fc.filterScenes[4].skybox == null) missing += " the star skybox,";
        if (water == null) missing += " Water Floor,";

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("Wire Filter Carousel: done." + (missing.Length > 0 ? " Not found:" + missing.TrimEnd(',') + "." : "") + " Press Cmd+S to save.");
    }

    // A plain light-grey circle with "Aa" on it, first in the carousel, for "no filter"
    // (Instagram also starts its filter list with a "Normal" option, so users have a clear way back)
    static void MakeOriginalIcon(Transform filters)
    {
        if (filters == null || filters.Find("Original") != null) return;   // already made
        var go = new GameObject("Original", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Wire Filter Carousel");
        go.transform.SetParent(filters, false);
        go.transform.SetAsFirstSibling();   // first in the list
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition3D = Vector3.zero;
        rt.sizeDelta = new Vector2(200, 200);
        go.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;   // stretch to fill the icon
        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = "Aa";
        tmp.fontSize = 72;
        tmp.color = new Color(0.25f, 0.25f, 0.25f);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // Sets a button's On Click () to call one method. Removes any old listeners first, so running the tool
    // twice doesn't make the button call the method twice. Returns the button's name if it wasn't found.
    // "Persistent" listeners are the ones saved in the scene and shown in the Inspector.
    static string Hook(GameObject canvas, string name, UnityAction call)
    {
        Transform t = canvas.transform.Find(name);
        Button b = t != null ? t.GetComponent<Button>() : null;
        if (b == null) return " " + name + ",";
        Undo.RecordObject(b, "Wire Filter Carousel");
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddPersistentListener(b.onClick, call);
        EditorUtility.SetDirty(b);
        return "";
    }

    // Finds a text by name on the canvas
    static TMP_Text Text(GameObject canvas, string name)
    {
        Transform t = canvas.transform.Find(name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    // Finds a child using the first of several possible names that exists (for the typo / renamed icons)
    static GameObject FindChild(Transform parent, params string[] names)
    {
        if (parent == null) return null;
        foreach (var n in names) { var t = parent.Find(n); if (t != null) return t.gameObject; }
        return null;
    }

    // Loads a filter's Volume Profile from Assets/Filters, trying each spelling
    static VolumeProfile Profile(params string[] names)
    {
        foreach (var n in names)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Filters/" + n + ".asset");
            if (p != null) return p;
        }
        return null;
    }

    // Finds any object in the scene by name, including switched-off ones (GameObject.Find skips those)
    static GameObject Find(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }
}
