using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tools > Fix Filter Panel: lays out Canvas - Filter and makes the filter icons circles.
// WHY: the filter panel's pieces were overlapping and the icons were squares. Placing them in code
// gives exact, tidy positions (in canvas units) and circular icons like Instagram's filter picker.
// (Editor-only helper, run from the Tools menu.)
public static class FilterPanelFixer
{
    [MenuItem("Tools/Fix Filter Panel")]
    public static void Fix()
    {
        GameObject canvasGo = GameObject.Find("Canvas - Filter");
        if (canvasGo == null) { EditorUtility.DisplayDialog("Fix Filter Panel", "Couldn't find 'Canvas - Filter' in the open scene.", "OK"); return; }
        Transform canvas = canvasGo.transform;

        // Round window: Filters masks its children to a circle.
        // A Mask only shows the parts of its children that sit inside its own image. Using Unity's
        // built-in round "Knob" sprite as that image cuts every icon into a circle.
        Transform filters = canvas.Find("Filters");
        if (filters != null)
        {
            Place(filters, 0, 200, 200, 200);
            Image mask = filters.GetComponent<Image>();
            if (mask == null) mask = Undo.AddComponent<Image>(filters.gameObject);
            Undo.RecordObject(mask, "Fix Filter Panel");
            mask.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            mask.color = Color.white;
            Mask m = filters.GetComponent<Mask>();
            if (m == null) m = Undo.AddComponent<Mask>(filters.gameObject);
            Undo.RecordObject(m, "Fix Filter Panel");
            m.showMaskGraphic = false;   // the white circle itself isn't shown, it only does the cutting

            // Size every icon to fill the circle. Picture icons keep their shape (taller ones are made
            // taller, not squashed); the circle then crops off the extra.
            foreach (Transform icon in filters)
            {
                float h = 200f;
                Image img = icon.GetComponent<Image>();
                if (img != null)
                {
                    Undo.RecordObject(img, "Fix Filter Panel");
                    img.preserveAspect = false;
                    if (img.sprite != null && img.sprite.name != "UISprite" && img.sprite.rect.width > 0)
                        h = Mathf.Max(200f, 200f * img.sprite.rect.height / img.sprite.rect.width);
                }
                Place(icon, 0, 0, 200, h);
            }
        }

        // Lay out the rest of the panel, top to bottom: name, "Applied" label, < > arrows, Apply / Close
        Text(canvas, "Filter Name", 0, 50, 380, 60, 36);
        Text(canvas, "Applied Label", 0, -15, 380, 50, 28);
        Place(canvas.Find("< btn"), -110, -110, 80, 60);
        Place(canvas.Find("> btn"), 110, -110, 80, 60);
        Place(canvas.Find("Apply btn"), -85, -200, 130, 60);
        Place(canvas.Find("Close btn"), 85, -200, 130, 60);
        // Apply is green (go / confirm) and Close is grey, so they're easy to tell apart (Design Evaluation 1)
        Tint(canvas.Find("Apply btn"), new Color(0.35f, 0.8f, 0.45f));
        Tint(canvas.Find("Close btn"), new Color(0.7f, 0.7f, 0.7f));

        // The big flashing filter name sits over the icon, and is moved last in the list so it's drawn on top
        Transform flash = canvas.Find("Flash Name");
        Text(canvas, "Flash Name", 0, 200, 380, 120, 60);
        if (flash != null) { Undo.SetTransformParent(flash, canvas, "Fix Filter Panel"); flash.SetAsLastSibling(); }

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("Fix Filter Panel: done. Press Cmd+S to save the scene.");
    }

    // Puts a UI element at (x, y) from the canvas centre with size w x h.
    // Anchors and pivot in the middle mean the numbers are measured from the centre.
    static void Place(Transform t, float x, float y, float w, float h)
    {
        if (t == null) return;
        RectTransform rt = t as RectTransform;
        if (rt == null) return;
        Undo.RecordObject(rt, "Fix Filter Panel");
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition3D = new Vector3(x, y, 0);
        rt.sizeDelta = new Vector2(w, h);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    // Places a text and sets it to centred, at the given font size
    static void Text(Transform canvas, string name, float x, float y, float w, float h, float size)
    {
        Transform t = canvas.Find(name);
        Place(t, x, y, w, h);
        if (t == null) return;
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return;
        Undo.RecordObject(tmp, "Fix Filter Panel");
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = size;
    }

    // Changes a button's background colour
    static void Tint(Transform t, Color c)
    {
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img == null) return;
        Undo.RecordObject(img, "Fix Filter Panel");
        img.color = c;
    }
}
