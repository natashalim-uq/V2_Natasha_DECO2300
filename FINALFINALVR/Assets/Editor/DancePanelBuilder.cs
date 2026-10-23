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
// WHY: the buttons are placed beside the Dance Spot at about waist height, within easy reach of the right hand
// and angled toward the right shoulder, so they can be pressed without walking away from the spot (ergonomics).
// Updated after headset testing: the first version was at full arm's stretch and angled toward the eyes, so
// a finger met it side-on and presses were often missed. It still sits just outside where the dance bubbles
// appear, so dancing doesn't press buttons by accident.
// Building it in code means the eight buttons are evenly laid out and correctly wired every time.
// (Editor-only helper, run from the Tools menu.)
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
        // Sizes are fractions of the board's own size (h * 0.3 etc.), so it works whatever size the board is
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
        // Reuse the panel if it exists, otherwise make a new one with everything a VR-clickable canvas needs
        GameObject panel = existing != null ? existing.gameObject : new GameObject("Canvas - Dance Buttons", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(panel, "Build Dance Panel");
            panel.transform.SetParent(task.transform, false);
        }
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;   // floats in the room instead of being stuck to the screen
        if (Camera.main != null) canvas.worldCamera = Camera.main;
        var prt = (RectTransform)panel.transform;
        prt.sizeDelta = new Vector2(400, 260);
        prt.localScale = Vector3.one * 0.001f;   // 1 canvas unit = 1 mm, so the panel is 40 x 26 cm
        if (spot != null)
        {
            // 50 cm to the right, 15 cm forward, 1.05 m up from the Dance Spot: about 45 cm from the right
            // shoulder, a relaxed reach (the dance bubbles stay within 40 cm of the middle, so it's clear of them)
            Vector3 pos = spot.position + spot.right * 0.5f + spot.forward * 0.15f + Vector3.up * 1.05f;
            Vector3 shoulder = spot.position + spot.right * 0.2f + Vector3.up * 1.35f;   // roughly where the right shoulder is
            prt.position = pos;
            // Face away from the shoulder (how a world canvas reads correctly). A finger reaching from the
            // shoulder then pushes straight into the buttons instead of brushing across them.
            prt.rotation = Quaternion.LookRotation(pos - shoulder, Vector3.up);
        }

        // Background so the buttons are easy to see
        var bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");   // Unity's rounded box
        bg.type = Image.Type.Sliced;   // stretch the middle, keep the corners round
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        // Colour groups so the buttons are easy to tell apart: purple = pick a song, green = start a stage,
        // red = stop, grey = small timing adjustments
        Color pick = new Color(0.75f, 0.65f, 1f), go = new Color(0.55f, 0.85f, 0.6f), stop = new Color(1f, 0.55f, 0.55f), small = new Color(0.85f, 0.85f, 0.85f);
        // Row 1: songs (these pass a number, 0 or 1, to SelectSong)
        // Buttons are 6 cm tall with 2 cm gaps: big enough for a fingertip, with room so you don't hit the neighbour
        AddIntButton(panel.transform, "Macarena", -92, 85, 170, 60, pick, dm.SelectSong, 0);
        AddIntButton(panel.transform, "YMCA", 92, 85, 170, 60, pick, dm.SelectSong, 1);
        // Row 2: stages
        AddButton(panel.transform, "Practice (0.5x)", -128, 5, 112, 60, go, dm.StartPractice);
        AddButton(panel.transform, "Full speed", 0, 5, 112, 60, go, dm.StartFullSpeed);
        AddButton(panel.transform, "Mastery", 128, 5, 112, 60, go, dm.StartMastery);
        // Row 3: stop and timing nudges
        AddButton(panel.transform, "Stop", -128, -75, 112, 60, stop, dm.StopDance);
        AddButton(panel.transform, "Earlier", 0, -75, 112, 60, small, dm.NudgeEarlier);
        AddButton(panel.transform, "Later", 128, -75, 112, 60, small, dm.NudgeLater);

        EditorUtility.SetDirty(dm);
        EditorSceneManager.MarkSceneDirty(task.scene);
        Selection.activeGameObject = panel;
        Debug.Log("Build Dance Panel: done. Move 'Canvas - Dance Buttons' if you'd like it elsewhere, then press Cmd+S.");
    }

    // Finds or creates a text on the board. place = true moves an existing text into position too;
    // false only positions it if it's new (so texts I'd already arranged by hand aren't moved).
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

    // Finds or creates one button (a coloured Image + Button + a text label), places it, and clears
    // its old On Click () listeners so they can be set fresh
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
            // Stretch the label to fill the whole button
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 18; tmp.color = new Color(0.1f, 0.1f, 0.1f);   // dark text on light buttons for contrast
            tmp.alignment = TextAlignmentOptions.Center;
        }
        Place(go.transform, x, y, w, h);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = color;
        var b = go.GetComponent<Button>();
        // Remove old listeners (counting backwards so removing one doesn't skip the next)
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        return b;
    }

    // A button that calls a method with no value, e.g. StartPractice()
    static void AddButton(Transform parent, string label, float x, float y, float w, float h, Color color, UnityAction call)
    {
        var b = MakeButton(parent, label, x, y, w, h, color);
        UnityEventTools.AddPersistentListener(b.onClick, call);
        EditorUtility.SetDirty(b);
    }

    // A button that calls a method with a number, e.g. SelectSong(1)
    static void AddIntButton(Transform parent, string label, float x, float y, float w, float h, Color color, UnityAction<int> call, int value)
    {
        var b = MakeButton(parent, label, x, y, w, h, color);
        UnityEventTools.AddIntPersistentListener(b.onClick, call, value);
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
