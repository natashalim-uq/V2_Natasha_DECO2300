using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

// Tools > Build Go To Panel (guide step 15b): makes the landing spots and the Go To panel
// on the XR Origin, with Start / Record / Filters / Learn a Dance buttons wired to StationJump.
public static class GoToPanelBuilder
{
    [MenuItem("Tools/Build Go To Panel")]
    public static void Build()
    {
        XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin == null) { EditorUtility.DisplayDialog("Build Go To Panel", "Couldn't find the XR Origin in the scene.", "OK"); return; }

        // Landing spots
        GameObject spotsGroup = FindOrMake("Go To Spots", null);
        Vector3 start = new Vector3(0f, 0f, -0.8f);
        Transform spotStart = Spot(spotsGroup.transform, "Spot - Start", start, Vector3.forward);

        Transform spotRecord = null;
        GameObject tripod = GameObject.Find("Tripod Rig");
        if (tripod != null)
        {
            Vector3 t = Flat(tripod.transform.position);
            Vector3 away = Flat(start - t);
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            away.Normalize();
            spotRecord = Spot(spotsGroup.transform, "Spot - Record", t + away * 0.8f, -away);
        }

        Transform spotFilters = null;
        GameObject filterCanvas = GameObject.Find("Canvas - Filter");
        if (filterCanvas != null)
        {
            Vector3 c = Flat(filterCanvas.transform.position);
            Vector3 look = Flat(filterCanvas.transform.forward).normalized;   // you read a world canvas looking along its forward
            spotFilters = Spot(spotsGroup.transform, "Spot - Filters", c - look * 1.0f, look);
        }

        GameObject danceSpot = GameObject.Find("Dance Spot");

        // Panel on the XR Origin, at your front right
        Transform existing = origin.transform.Find("Canvas - Go To");
        GameObject panel = existing != null ? existing.gameObject : new GameObject("Canvas - Go To", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster), typeof(StationJump));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(panel, "Build Go To Panel");
            panel.transform.SetParent(origin.transform, false);
        }
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (origin.Camera != null) canvas.worldCamera = origin.Camera;
        var prt = (RectTransform)panel.transform;
        prt.localPosition = new Vector3(0.3f, 0.95f, 0.3f);
        prt.localRotation = Quaternion.Euler(30f, 30f, 0f);
        prt.localScale = Vector3.one * 0.001f;
        prt.sizeDelta = new Vector2(300, 260);

        var bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.1f, 0.1f, 0.16f, 0.8f);

        Transform title = panel.transform.Find("Title");
        if (title == null)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(panel.transform, false);
            title = go.transform;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = "Go to"; tmp.fontSize = 26; tmp.fontStyle = FontStyles.Bold; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
        Place(title, 0, 110, 260, 36);

        var jump = panel.GetComponent<StationJump>();
        if (jump == null) jump = panel.AddComponent<StationJump>();
        Undo.RecordObject(jump, "Build Go To Panel");
        jump.spots = new Transform[] { spotStart, spotRecord, spotFilters, danceSpot != null ? danceSpot.transform : null };

        Color c1 = new Color(0.75f, 0.65f, 1f);
        AddButton(panel.transform, "Start", 70, c1, jump, 0);
        AddButton(panel.transform, "Record", 20, c1, jump, 1);
        AddButton(panel.transform, "Filters", -30, c1, jump, 2);
        AddButton(panel.transform, "Learn a Dance", -80, c1, jump, 3);

        EditorUtility.SetDirty(jump);
        EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);
        Selection.activeGameObject = panel;

        string missing = (spotRecord == null ? " Tripod Rig" : "") + (spotFilters == null ? " Canvas - Filter" : "") + (danceSpot == null ? " Dance Spot" : "");
        Debug.Log("Build Go To Panel: done." + (missing.Length > 0 ? " Couldn't find:" + missing + ", so that button does nothing yet." : "") + " Press Cmd+S.");
    }

    static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    static GameObject FindOrMake(string name, Transform parent)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;
        go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build Go To Panel");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static Transform Spot(Transform group, string name, Vector3 pos, Vector3 facing)
    {
        Transform t = group.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Build Go To Panel");
            go.transform.SetParent(group, false);
            t = go.transform;
        }
        Undo.RecordObject(t, "Build Go To Panel");
        t.position = pos;
        t.rotation = Quaternion.LookRotation(facing, Vector3.up);
        return t;
    }

    static void AddButton(Transform parent, string label, float y, Color color, StationJump jump, int index)
    {
        string name = label + " btn";
        Transform t = parent.Find(name);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Build Go To Panel");
            go.transform.SetParent(parent, false);
            var txt = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var trt = (RectTransform)txt.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 22; tmp.color = new Color(0.1f, 0.1f, 0.1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }
        Place(go.transform, 0, y, 250, 42);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = color;
        var b = go.GetComponent<Button>();
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddIntPersistentListener(b.onClick, jump.JumpTo, index);
        EditorUtility.SetDirty(b);
    }

    static void Place(Transform t, float x, float y, float w, float h)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition3D = new Vector3(x, y, 0);
        rt.sizeDelta = new Vector2(w, h);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }
}
