using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Tools > Fix Panel Buttons: gives every world-space canvas in the scene the same setup as the
// Filter and Record panels (Graphic Raycaster + Tracked Device Graphic Raycaster + Event Camera),
// so buttons work with hands in the headset and with the simulator's controllers on the laptop.
public static class PanelButtonsFixer
{
    [MenuItem("Tools/Fix Panel Buttons")]
    public static void Fix()
    {
        Camera cam = Camera.main;
        int fixedCount = 0;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.WorldSpace) continue;
            if (EditorUtility.IsPersistent(canvas)) continue;
            bool changed = false;
            GameObject go = canvas.gameObject;
            if (go.GetComponent<GraphicRaycaster>() == null) { Undo.AddComponent<GraphicRaycaster>(go); changed = true; }
            if (go.GetComponent<TrackedDeviceGraphicRaycaster>() == null) { Undo.AddComponent<TrackedDeviceGraphicRaycaster>(go); changed = true; }
            if (cam != null && canvas.worldCamera == null)
            {
                Undo.RecordObject(canvas, "Fix Panel Buttons");
                canvas.worldCamera = cam;
                changed = true;
            }
            if (changed) { fixedCount++; Debug.Log("Fix Panel Buttons: fixed " + go.name, go); }
        }
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("Fix Panel Buttons: done, " + fixedCount + " panel(s) changed. Press Cmd+S.");
    }
}
