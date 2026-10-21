using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

// Tools > Build Welcome Panel (guide step 15): a panel in front of the start that welcomes people,
// explains the three tasks, and hides itself when Let's go! is pressed.
public static class WelcomePanelBuilder
{
    const string Body =
        "Take a moment to look around the studio. There are three things to try:\n\n" +
        "<b>1. Record:</b> pick up the camera, put it on the tripod and press Record to film yourself.\n\n" +
        "<b>2. Filters:</b> at the mirror, flick through filters that change the whole room, then press Apply.\n\n" +
        "<b>3. Learn a Dance:</b> stand on the Dance Spot, pick Macarena or YMCA and pop the bubbles in time with the song.\n\n" +
        "Stuck? Press the <b>?</b> at any station for a hint.";

    [MenuItem("Tools/Build Welcome Panel")]
    public static void Build()
    {
        GameObject panel = GameObject.Find("Canvas - Welcome");
        if (panel == null)
        {
            panel = new GameObject("Canvas - Welcome", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(panel, "Build Welcome Panel");
        }
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin != null && origin.Camera != null) canvas.worldCamera = origin.Camera;
        if (panel.GetComponent<GraphicRaycaster>() == null) panel.AddComponent<GraphicRaycaster>();
        if (panel.GetComponent<TrackedDeviceGraphicRaycaster>() == null) panel.AddComponent<TrackedDeviceGraphicRaycaster>();

        var prt = (RectTransform)panel.transform;
        Undo.RecordObject(prt, "Build Welcome Panel");
        prt.position = new Vector3(0f, 1.4f, 0.3f);
        prt.rotation = Quaternion.identity;
        prt.localScale = Vector3.one * 0.0015f;   // about 1.1 m from the start: far enough for a controller's ray, big enough to read
        prt.sizeDelta = new Vector2(600, 460);

        var bg = Child(panel.transform, "Background", typeof(Image)).GetComponent<Image>();
        var bgrt = (RectTransform)bg.transform;
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bg.type = Image.Type.Sliced;
        bg.color = new Color32(25, 25, 40, 220);
        bg.transform.SetAsFirstSibling();

        var title = Text(panel.transform, "Welcome Title", 0, 175, 560, 70, 42, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.text = "Welcome to the Dance Studio!";

        var body = Text(panel.transform, "Welcome Text", 0, 10, 540, 260, 22, TextAlignmentOptions.TopLeft);
        body.text = Body;
        body.enableAutoSizing = true; body.fontSizeMin = 14; body.fontSizeMax = 22;

        // Let's go! button hides the whole panel
        Transform bt = panel.transform.Find("Start Btn");
        GameObject btn;
        if (bt != null) btn = bt.gameObject;
        else
        {
            btn = new GameObject("Start Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btn, "Build Welcome Panel");
            btn.transform.SetParent(panel.transform, false);
        }
        Place(btn.transform, 0, -180, 220, 60);
        var img = btn.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = new Color32(140, 215, 150, 255);
        var label = Text(btn.transform, "Text (TMP)", 0, 0, 220, 60, 26, TextAlignmentOptions.Center);
        label.text = "Let's go!";
        label.color = new Color(0.1f, 0.1f, 0.1f);
        var b = btn.GetComponent<Button>();
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddBoolPersistentListener(b.onClick, panel.SetActive, false);
        EditorUtility.SetDirty(b);

        EditorSceneManager.MarkSceneDirty(panel.scene);
        Selection.activeGameObject = panel;
        bool warmup = GameObject.Find("Warm-up Cube") != null || GameObject.Find("Warm-up Stand") != null;
        Debug.Log("Build Welcome Panel: done." + (warmup ? " You can delete Warm-up Stand and Warm-up Cube." : "") + " Press Cmd+S.");
    }

    static GameObject Child(Transform parent, string name, System.Type comp)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name, typeof(RectTransform), comp);
        Undo.RegisterCreatedObjectUndo(go, "Build Welcome Panel");
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI Text(Transform parent, string name, float x, float y, float w, float h, float size, TextAlignmentOptions align)
    {
        var tmp = Child(parent, name, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Place(tmp.transform, x, y, w, h);
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
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
