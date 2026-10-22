using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Add Invisible Wall: closes the room's open side (x = +5) with a collider you can't see,
// and adds a big invisible Safety Floor so nobody can fall out of the world.
// WHY: the room model has one open side, and testers could walk (or teleport) out of it. A collider
// with no mesh blocks movement without changing how the room looks.
// (Editor-only helper: lives in the Editor folder, runs from the Tools menu, not part of the game itself.)
public static class InvisibleWallBuilder
{
    [MenuItem("Tools/Add Invisible Wall")]
    public static void Add()
    {
        GameObject walls = GameObject.Find("Floors and Walls");
        // Reuse the wall if it's already there, so running the tool twice doesn't make two walls
        Transform existing = walls != null ? walls.transform.Find("Invisible Wall") : null;
        GameObject wall = existing != null ? existing.gameObject : new GameObject("Invisible Wall");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(wall, "Add Invisible Wall");   // so Cmd+Z can remove it
            if (walls != null) wall.transform.SetParent(walls.transform, false);
        }
        // Put it along the open side of the room (x = 5), centred on the room's length
        wall.transform.localPosition = new Vector3(5f, 1.75f, -1.25f);
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = Vector3.one;
        // Just a Box Collider, no MeshRenderer, so it's solid but invisible
        var box = wall.GetComponent<BoxCollider>();
        if (box == null) box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(0.1f, 3.5f, 7.5f);   // 10 cm thick, 3.5 m tall, 7.5 m long
        // Safety Floor: a big invisible floor under the room, so walking (or head-moving) past a wall never drops you
        Transform sfT = walls != null ? walls.transform.Find("Safety Floor") : null;
        GameObject safety = sfT != null ? sfT.gameObject : new GameObject("Safety Floor");
        if (sfT == null)
        {
            Undo.RegisterCreatedObjectUndo(safety, "Add Invisible Wall");
            if (walls != null) safety.transform.SetParent(walls.transform, false);
        }
        // Just below the real floor (-6 cm), so it only matters if you somehow go past the room
        safety.transform.localPosition = new Vector3(0f, -0.06f, -1.25f);
        safety.transform.localRotation = Quaternion.identity;
        safety.transform.localScale = Vector3.one;
        var sbox = safety.GetComponent<BoxCollider>();
        if (sbox == null) sbox = safety.AddComponent<BoxCollider>();
        sbox.size = new Vector3(60f, 0.1f, 60f);   // 60 x 60 m, much bigger than the room

        EditorSceneManager.MarkSceneDirty(wall.scene);
        Selection.activeGameObject = wall;   // select it in the Hierarchy so I can see where it went
        Debug.Log("Add Invisible Wall: done. Press Cmd+S to save.");
    }
}
