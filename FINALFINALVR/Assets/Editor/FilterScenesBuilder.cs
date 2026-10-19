using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Tools > Build Water and Stars: makes Water Floor (with its material) and Room Stars, both switched off.
public static class FilterScenesBuilder
{
    [MenuItem("Tools/Build Water and Stars")]
    public static void Build()
    {
        GameObject walls = GameObject.Find("Floors and Walls");

        // Water material
        const string matPath = "Assets/Materials/Water.mat";
        Material water = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (water == null)
        {
            water = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(water, matPath);
        }
        water.SetFloat("_Surface", 1f);
        water.SetFloat("_Blend", 0f);
        water.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        water.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        water.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_ZWrite", 0f);
        water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        water.SetOverrideTag("RenderType", "Transparent");
        water.renderQueue = (int)RenderQueue.Transparent;
        water.SetColor("_BaseColor", new Color(40 / 255f, 130 / 255f, 170 / 255f, 150 / 255f));
        water.SetFloat("_Metallic", 0f);
        water.SetFloat("_Smoothness", 0.95f);
        EditorUtility.SetDirty(water);

        // Water Floor
        GameObject floor = FindIncludingInactive("Water Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Water Floor";
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            Undo.RegisterCreatedObjectUndo(floor, "Build Water and Stars");
            if (walls != null) floor.transform.SetParent(walls.transform, false);
        }
        floor.transform.localPosition = new Vector3(0f, 0.02f, -1.25f);
        floor.transform.localRotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(1f, 1f, 0.75f);
        floor.GetComponent<Renderer>().sharedMaterial = water;
        if (floor.GetComponent<WaterFloor>() == null) floor.AddComponent<WaterFloor>();
        floor.SetActive(false);

        // Room Stars
        GameObject stars = FindIncludingInactive("Room Stars");
        if (stars == null)
        {
            stars = new GameObject("Room Stars");
            Undo.RegisterCreatedObjectUndo(stars, "Build Water and Stars");
            GameObject task = GameObject.Find("Task 2: Filters");
            if (task != null) stars.transform.SetParent(task.transform, true);
            stars.AddComponent<ParticleSystem>();
        }
        stars.transform.position = new Vector3(0f, 1.5f, -1.25f);
        stars.transform.rotation = Quaternion.identity;
        var ps = stars.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.startLifetime = 10f;
        main.startSpeed = 0f;
        main.startSize = 0.02f;
        main.maxParticles = 300;
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 3f, 4f);
        var pr = stars.GetComponent<ParticleSystemRenderer>();
        if (pr.sharedMaterial == null)
        {
            const string starPath = "Assets/Materials/Room Stars.mat";
            Material starMat = AssetDatabase.LoadAssetAtPath<Material>(starPath);
            if (starMat == null)
            {
                starMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(starMat, starPath);
            }
            pr.sharedMaterial = starMat;
        }
        stars.SetActive(false);

        // Camera must clear to the skybox for the star sky to show
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

    static GameObject FindIncludingInactive(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }
}
