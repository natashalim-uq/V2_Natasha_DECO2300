using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Add Invisible Wall: closes the room's open side (x = +5) with a collider you can't see,
// and adds a big invisible Safety Floor so nobody can fall out of the world.
public static class InvisibleWallBuilder
{
    [MenuItem("Tools/Add Invisible Wall")]
    public static void Add()
    {
        GameObject walls = GameObject.Find("Floors and Walls");
        Transform existing = walls != null ? walls.transform.Find("Invisible Wall") : null;
        GameObject wall = existing != null ? existing.gameObject : new GameObject("Invisible Wall");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(wall, "Add Invisible Wall");
            if (walls != null) wall.transform.SetParent(walls.transform, false);
        }
        wall.transform.localPosition = new Vector3(5f, 1.75f, -1.25f);
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = Vector3.one;
        var box = wall.GetComponent<BoxCollider>();
        if (box == null) box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(0.1f, 3.5f, 7.5f);
        // Safety Floor: a big invisible floor under the room, so walking (or head-moving) past a wall never drops you
        Transform sfT = walls != null ? walls.transform.Find("Safety Floor") : null;
        GameObject safety = sfT != null ? sfT.gameObject : new GameObject("Safety Floor");
        if (sfT == null)
        {
            Undo.RegisterCreatedObjectUndo(safety, "Add Invisible Wall");
            if (walls != null) safety.transform.SetParent(walls.transform, false);
        }
        safety.transform.localPosition = new Vector3(0f, -0.06f, -1.25f);
        safety.transform.localRotation = Quaternion.identity;
        safety.transform.localScale = Vector3.one;
        var sbox = safety.GetComponent<BoxCollider>();
        if (sbox == null) sbox = safety.AddComponent<BoxCollider>();
        sbox.size = new Vector3(60f, 0.1f, 60f);

        EditorSceneManager.MarkSceneDirty(wall.scene);
        Selection.activeGameObject = wall;
        Debug.Log("Add Invisible Wall: done. Press Cmd+S to save.");
    }
}
