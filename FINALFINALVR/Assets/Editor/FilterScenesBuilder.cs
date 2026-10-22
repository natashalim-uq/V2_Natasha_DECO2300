using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Tools > Build Water and Stars: makes Water Floor (with its material) and Room Stars, both switched off.
// These are the extra objects for the "Water" and "Stars" filters. They start switched off, and
// FilterCarousel switches them on only when that filter is chosen.
// (Editor-only helper, run from the Tools menu.)
public static class FilterScenesBuilder
{
    [MenuItem("Tools/Build Water and Stars")]
    public static void Build()
    {
        GameObject walls = GameObject.Find("Floors and Walls");

        // Water material
        // A see-through (transparent), very shiny blue material. Shiny (smoothness 0.95) so it reflects
        // like water; WaterFloor.cs then adds the moving ripples.
        const string matPath = "Assets/Materials/Water.mat";
        Material water = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (water == null)
        {
            water = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(water, matPath);
        }
        // These lines are what ticking "Surface Type: Transparent" in the Inspector does behind the scenes
        // (blend the colour with what's behind it using its alpha, and don't block things behind it)
        water.SetFloat("_Surface", 1f);
        water.SetFloat("_Blend", 0f);
        water.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        water.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        water.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_ZWrite", 0f);
        water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        water.SetOverrideTag("RenderType", "Transparent");
        water.renderQueue = (int)RenderQueue.Transparent;   // draw after solid objects
        water.SetColor("_BaseColor", new Color(40 / 255f, 130 / 255f, 170 / 255f, 150 / 255f));   // blue, about 60% solid
        water.SetFloat("_Metallic", 0f);
        water.SetFloat("_Smoothness", 0.95f);
        EditorUtility.SetDirty(water);

        // Water Floor
        GameObject floor = FindIncludingInactive("Water Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Water Floor";
            // Remove its collider: the real floor underneath already holds the player up
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            Undo.RegisterCreatedObjectUndo(floor, "Build Water and Stars");
            if (walls != null) floor.transform.SetParent(walls.transform, false);
        }
        // 2 cm above the real floor, so the two surfaces don't flicker against each other (z-fighting)
        floor.transform.localPosition = new Vector3(0f, 0.02f, -1.25f);
        floor.transform.localRotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(1f, 1f, 0.75f);
        floor.GetComponent<Renderer>().sharedMaterial = water;
        if (floor.GetComponent<WaterFloor>() == null) floor.AddComponent<WaterFloor>();
        floor.SetActive(false);   // off until the Water filter is picked

        // Room Stars
        // A particle system that fills the room with tiny still dots, so stars are around you, not only in the sky
        GameObject stars = FindIncludingInactive("Room Stars");
        if (stars == null)
        {
            stars = new GameObject("Room Stars");
            Undo.RegisterCreatedObjectUndo(stars, "Build Water and Stars");
            GameObject task = GameObject.Find("Task 2: Filters");
            if (task != null) stars.transform.SetParent(task.transform, true);
            stars.AddComponent<ParticleSystem>();
        }
        stars.transform.position = new Vector3(0f, 1.5f, -1.25f);   // centre of the room, at about head height
        stars.transform.rotation = Quaternion.identity;
        var ps = stars.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;        // the room is already full of stars when it's switched on (no fade-in)
        main.playOnAwake = true;
        main.startLifetime = 10f;   // each star lasts 10 s, then a new one appears somewhere else (gentle twinkle)
        main.startSpeed = 0f;       // stars don't move
        main.startSize = 0.02f;     // 2 cm dots
        main.maxParticles = 300;    // kept low so the Quest runs smoothly
        var emission = ps.emission;
        emission.rateOverTime = 30f;   // 30 per second x 10 s life = about 300 stars at once
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 3f, 4f);   // spread stars through a box roughly the size of the room
        var pr = stars.GetComponent<ParticleSystemRenderer>();
        if (pr.sharedMaterial == null)
        {
            const string starPath = "Assets/Materials/Room Stars.mat";
            Material starMat = AssetDatabase.LoadAssetAtPath<Material>(starPath);
            if (starMat == null)
            {
                // Unlit, so stars glow the same whatever the lighting is
                starMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(starMat, starPath);
            }
            pr.sharedMaterial = starMat;
        }
        stars.SetActive(false);   // off until the Stars filter is picked

        // Camera must clear to the skybox for the star sky to show
        // (if it cleared to a plain colour, swapping the skybox would have no visible effect)
        Camera cam = Camera.main;
        if (cam != null && cam.clearFlags != CameraClearFlags.Skybox)
        {
            Undo.RecordObject(cam, "Build Water and Stars");
            cam.clearFlags = CameraClearFlags.Skybox;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(floor.scene);
        Debug.Log("Build Water and Stars: made Water Floor and Room Stars (both switched off). Press Cmd+S to save.");
    }

    // GameObject.Find can't find switched-off objects, so this searches every object, including hidden ones.
    // scene.IsValid() skips prefab files that aren't actually in the scene.
    static GameObject FindIncludingInactive(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }
}
