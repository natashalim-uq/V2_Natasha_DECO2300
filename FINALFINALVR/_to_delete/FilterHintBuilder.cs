using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tools > Build Filter Hint: adds a "?" button and a Hint panel to Canvas - Filter and wires them together.
public static class FilterHintBuilder
{
    const string HintText = "Tap the arrows with your finger to try filters. The room changes as a preview. Tap Apply to keep it, or Close to go back to the original room.";

    [MenuItem("Tools/Build Filter Hint")]
    public static void Build()
    {
        GameObject canvasGo = GameObject.Find("Canvas - Filter");
        if (canvasGo == null) { EditorUtility.DisplayDialog("Build Filter Hint", "Couldn't find 'Canvas - Filter'.", "OK"); return; }
        Transform canvas = canvasGo.transform;
        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Hint panel
        Transform hintT = canvas.Find("Hint");
        GameObject hint = hintT != null ? hintT.gameObject : Create("Hint", canvas, typeof(Image));
        Place(hint.transform, 0, 60, 360, 220);
        var bg = hint.GetComponent<Image>();
        bg.sprite = rounded; bg.type = Image.Type.Sliced;
        bg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

        Transform textT = hint.transform.Find("Hint Text");
        GameObject text = textT != null ? textT.gameObject : Create("Hint Text", hint.transform, typeof(TextMeshProUGUI));
        var trt = (RectTransform)text.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16, 16); trt.offsetMax = new Vector2(-16, -16);
        trt.localScale = Vector3.one; trt.localRotation = Quaternion.identity;
        var tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = HintText; tmp.fontSize = 22; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.textWrappingMode = TextWrappingModes.Normal;

        // "?" button
        Transform qT = canvas.Find("? btn");
        GameObject q = qT != null ? qT.gameObject : Create("? btn", canvas, typeof(Image), typeof(Button));
        Place(q.transform, 150, 330, 60, 60);
        var qImg = q.GetComponent<Image>();
        qImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        qImg.color = Color.white;
        Transform qLabelT = q.transform.Find("Text (TMP)");
        GameObject qLabel = qLabelT != null ? qLabelT.gameObject : Create("Text (TMP)", q.transform, typeof(TextMeshProUGUI));
        var lrt = (RectTransform)qLabel.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        lrt.localScale = Vector3.one; lrt.localRotation = Quaternion.identity;
        var qText = qLabel.GetComponent<TextMeshProUGUI>();
        qText.text = "?"; qText.fontSize = 40; qText.fontStyle = FontStyles.Bold;
        qText.color = new Color(0.2f, 0.2f, 0.2f); qText.alignment = TextAlignmentOptions.Center;

        var toggle = q.GetComponent<HintToggle>();
        if (toggle == null) toggle = Undo.AddComponent<HintToggle>(q);
        toggle.hintPanel = hint;
        var button = q.GetComponent<Button>();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, toggle.Toggle);

        // Order: Hint, then "?" (so it stays pressable), then Flash Name last (draws on top)
        hint.transform.SetAsLastSibling();
        q.transform.SetAsLastSibling();
        Transform flash = canvas.Find("Flash Name");
        if (flash != null) flash.SetAsLastSibling();

        hint.SetActive(false);
        EditorUtility.SetDirty(toggle); EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("Build Filter Hint: done. Press Cmd+S to save.");
    }

    static GameObject Create(string name, Transform parent, params System.Type[] components)
    {
        var types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        components.CopyTo(types, 1);
        var go = new GameObject(name, types);
        Undo.RegisterCreatedObjectUndo(go, "Build Filter Hint");
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
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
