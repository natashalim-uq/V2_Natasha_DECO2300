using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

// Tools > Build Dance Panel: finishes Canvas - Dance (texts) and builds Canvas - Dance Buttons
// beside the Dance Spot, with the eight buttons wired to Dance Manager.
public static class DancePanelBuilder
{
    [MenuItem("Tools/Build Dance Panel")]
    public static void Build()
    {
        GameObject task = GameObject.Find("Task 3: Dance");
        DanceManager dm = task != null ? task.GetComponent<DanceManager>() : null;
        if (dm == null) { EditorUtility.DisplayDialog("Build Dance Panel", "Add Dance Manager to 'Task 3: Dance' first (step 14d).", "OK"); return; }
        Undo.RecordObject(dm, "Build Dance Panel");

        // Texts on the big wall canvas
        GameObject board = GameObject.Find("Canvas - Dance");
        if (board != null)
        {
            var rt = (RectTransform)board.transform;
            float h = rt.sizeDelta.y, w = rt.sizeDelta.x;
            dm.songText = MakeText(board.transform, "Song Text", 0, h * 0.3f, w * 0.9f, h * 0.2f, h * 0.14f, false);
            dm.stageText = MakeText(board.transform, "Stage Text", 0, h * 0.1f, w * 0.9f, h * 0.2f, h * 0.12f, false);
            dm.scoreText = MakeText(board.transform, "Score Text", 0, -h * 0.08f, w * 0.9f, h * 0.15f, h * 0.09f, true);
            dm.historyText = MakeText(board.transform, "History Text", 0, -h * 0.32f, w * 0.9f, h * 0.3f, h * 0.06f, true);
        }

        // Small button canvas beside the Dance Spot, within arm's reach
        Transform spot = dm.danceSpot != null ? dm.danceSpot : GameObject.Find("Dance Spot")?.transform;
        Transform existing = task.transform.Find("Canvas - Dance Buttons");
        GameObject panel = existing != null ? existing.gameObject : new GameObject("Canvas - Dance Buttons", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(panel, "Build Dance Panel");
            panel.transform.SetParent(task.transform, false);
        }
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null) canvas.worldCamera = Camera.main;
        var prt = (RectTransform)panel.transform;
        prt.sizeDelta = new Vector2(360, 230);
        prt.localScale = Vector3.one * 0.001f;
        if (spot != null)
        {
            Vector3 pos = spot.position + spot.right * 0.6f + spot.forward * 0.25f + Vector3.up * 1.1f;
            Vector3 eye = spot.position + Vector3.up * 1.5f;
            prt.position = pos;
            prt.rotation = Quaternion.LookRotation(pos - eye, Vector3.up);
        }

        // Background so the buttons are easy to see
        var bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        Color pick = new Color(0.75f, 0.65f, 1f), go = new Color(0.55f, 0.85f, 0.6f), stop = new Color(1f, 0.55f, 0.55f), small = new Color(0.85f, 0.85f, 0.85f);
        AddIntButton(panel.transform, "Macarena", -85, 80, 160, 50, pick, dm.SelectSong, 0);
        AddIntButton(panel.transform, "YMCA", 85, 80, 160, 50, pick, dm.SelectSong, 1);
        AddButton(panel.transform, "Practice (0.5x)", -115, 15, 105, 50, go, dm.StartPractice);
        AddButton(panel.transform, "Full speed", 0, 15, 105, 50, go, dm.StartFullSpeed);
        AddButton(panel.transform, "Mastery", 115, 15, 105, 50, go, dm.StartMastery);
        AddButton(panel.transform, "Stop", -115, -60, 105, 50, stop, dm.StopDance);
        AddButton(panel.transform, "Earlier", 0, -60, 105, 50, small, dm.NudgeEarlier);
        AddButton(panel.transform, "Later", 115, -60, 105, 50, small, dm.NudgeLater);

        EditorUtility.SetDirty(dm);
        EditorSceneManager.MarkSceneDirty(task.scene);
        Selection.activeGameObject = panel;
        Debug.Log("Build Dance Panel: done. Move 'Canvas - Dance Buttons' if you'd like it elsewhere, then press Cmd+S.");
    }

    static TMP_Text MakeText(Transform parent, string name, float x, float y, float w, float h, float size, bool place)
    {
        Transform t = parent.Find(name);
        bool created = t == null;
        if (created)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Build Dance Panel");
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return null;
        if (created || place)
        {
            if (!created) Undo.RecordObject(t, "Build Dance Panel");
            Place(t, x, y, w, h);
            if (created)
            {
                tmp.fontSize = size;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.text = name == "Score Text" ? "Hit 0   Missed 0" : "";
            }
        }
        return tmp;
    }

    static Button MakeButton(Transform parent, string label, float x, float y, float w, float h, Color color)
    {
        string name = label + " btn";
        Transform t = parent.Find(name);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Build Dance Panel");
            go.transform.SetParent(parent, false);
            var txt = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var trt = (RectTransform)txt.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 18; tmp.color = new Color(0.1f, 0.1f, 0.1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }
        Place(go.transform, x, y, w, h);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = color;
        var b = go.GetComponent<Button>();
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        return b;
    }

    static void AddButton(Transform parent, string label, float x, float y, float w, float h, Color color, UnityAction call)
    {
        var b = MakeButton(parent, label, x, y, w, h, color);
        UnityEventTools.AddPersistentListener(b.onClick, call);
        EditorUtility.SetDirty(b);
    }

    static void AddIntButton(Transform parent, string label, float x, float y, float w, float h, Color color, UnityAction<int> call, int value)
    {
        var b = MakeButton(parent, label, x, y, w, h, color);
        UnityEventTools.AddIntPersistentListener(b.onClick, call, value);
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
