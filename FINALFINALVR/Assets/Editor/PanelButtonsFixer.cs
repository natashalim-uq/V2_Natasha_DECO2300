using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Tools > Fix Panel Buttons: gives every world-space canvas in the scene the same setup as the
// Filter and Record panels (Graphic Raycaster + Tracked Device Graphic Raycaster + Event Camera),
// so buttons work with hands in the headset and with the simulator's controllers on the laptop.
// WHY: an Editor script is a helper that runs in the Unity editor only (it's in the Editor folder, so it
// is NOT in the Quest build). Some panels' buttons didn't react in VR because a component was missing;
// this fixes every panel at once instead of checking each one by hand.
public static class PanelButtonsFixer
{
    // [MenuItem] adds this method to Unity's top menu bar as Tools > Fix Panel Buttons
    [MenuItem("Tools/Fix Panel Buttons")]
    public static void Fix()
    {
        Camera cam = Camera.main;
        int fixedCount = 0;
        // Look at every Canvas in the scene, including hidden ones
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Only top-level canvases that float in the 3D world (not screen overlays or nested canvases)
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.WorldSpace) continue;
            // Skip canvases inside prefab/asset files, only change ones in the scene
            if (EditorUtility.IsPersistent(canvas)) continue;
            bool changed = false;
            GameObject go = canvas.gameObject;
            // Graphic Raycaster: lets a normal mouse/pointer click the buttons.
            // Tracked Device Graphic Raycaster: lets VR hands and controller rays click them.
            // Undo.AddComponent (rather than AddComponent) means Cmd+Z can undo it.
            if (go.GetComponent<GraphicRaycaster>() == null) { Undo.AddComponent<GraphicRaycaster>(go); changed = true; }
            if (go.GetComponent<TrackedDeviceGraphicRaycaster>() == null) { Undo.AddComponent<TrackedDeviceGraphicRaycaster>(go); changed = true; }
            // A world-space canvas needs an Event Camera to work out what you're pointing at
            if (cam != null && canvas.worldCamera == null)
            {
                Undo.RecordObject(canvas, "Fix Panel Buttons");
                canvas.worldCamera = cam;
                changed = true;
            }
            if (changed) { fixedCount++; Debug.Log("Fix Panel Buttons: fixed " + go.name, go); }
        }
        // Mark the scene as changed so Unity asks to save it
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("Fix Panel Buttons: done, " + fixedCount + " panel(s) changed. Press Cmd+S.");
    }
}
