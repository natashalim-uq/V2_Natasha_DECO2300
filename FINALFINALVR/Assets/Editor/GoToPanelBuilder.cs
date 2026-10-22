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
// WHY: in Design Evaluation 2, testers said there was too much walking. The panel is a child of the
// XR Origin (the player rig), so it travels with you and is always within reach. Each landing spot is
// worked out from where its station is, so if a station is moved, re-running the tool updates the spot.
// (Editor-only helper, run from the Tools menu.)
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

        // Record spot: 80 cm from the tripod, on the side facing the start, looking at the tripod
        Transform spotRecord = null;
        GameObject tripod = GameObject.Find("Tripod Rig");
        if (tripod != null)
        {
            Vector3 t = Flat(tripod.transform.position);
            Vector3 away = Flat(start - t);   // direction from the tripod toward the start
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;   // fallback if they're in the same place
            away.Normalize();   // make it 1 m long so "* 0.8f" means exactly 80 cm
            spotRecord = Spot(spotsGroup.transform, "Spot - Record", t + away * 0.8f, -away);
        }

        // Filters spot: 1 m in front of the filter panel, facing it
        Transform spotFilters = null;
        GameObject filterCanvas = GameObject.Find("Canvas - Filter");
        if (filterCanvas != null)
        {
            Vector3 c = Flat(filterCanvas.transform.position);
            Vector3 look = Flat(filterCanvas.transform.forward).normalized;   // you read a world canvas looking along its forward
            spotFilters = Spot(spotsGroup.transform, "Spot - Filters", c - look * 1.0f, look);
        }

        // Dance: the existing Dance Spot is used as the landing spot
        GameObject danceSpot = GameObject.Find("Dance Spot");

        // Panel on the XR Origin, at your front right
        Transform existing = origin.transform.Find("Canvas - Go To");
        GameObject panel = existing != null ? existing.gameObject : new GameObject("Canvas - Go To", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster), typeof(StationJump));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(panel, "Build Go To Panel");
            panel.transform.SetParent(origin.transform, false);
        }
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (origin.Camera != null) canvas.worldCamera = origin.Camera;
        var prt = (RectTransform)panel.transform;
        // 40 cm right, 1.05 m up, 55 cm ahead: near your right hand, below eye level so it doesn't block the view
        prt.localPosition = new Vector3(0.4f, 1.05f, 0.55f);
        prt.localRotation = Quaternion.Euler(20f, 35f, 0f);   // tilted up and turned toward you, like a tablet held at your side
        prt.localScale = Vector3.one * 0.001f;   // 1 unit = 1 mm, so 30 x 26 cm
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

        // Give StationJump the four spots, in the same order as the buttons below (0, 1, 2, 3)
        var jump = panel.GetComponent<StationJump>();
        if (jump == null) jump = panel.AddComponent<StationJump>();
        Undo.RecordObject(jump, "Build Go To Panel");
        jump.spots = new Transform[] { spotStart, spotRecord, spotFilters, danceSpot != null ? danceSpot.transform : null };

        // Four buttons stacked top to bottom, each calling JumpTo with its spot's number
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

    // Drops the height (y = 0), so all the maths happens on the floor
    static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    // Finds an object by name, or makes an empty one if it doesn't exist
    static GameObject FindOrMake(string name, Transform parent)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;
        go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build Go To Panel");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    // Finds or makes a landing spot, puts it at pos and turns its blue arrow to point along "facing"
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

    // Finds or creates one button, places it, and sets its On Click () to StationJump.JumpTo(index)
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
        Place(go.transform, 0, y, 250, 42);   // wide buttons (25 cm) so they're easy to hit with a finger
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = color;
        var b = go.GetComponent<Button>();
        // Clear old listeners so re-running the tool doesn't double them up
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddIntPersistentListener(b.onClick, jump.JumpTo, index);
        EditorUtility.SetDirty(b);
    }

    // Puts a UI element at (x, y) from the panel's centre, with size w x h
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
