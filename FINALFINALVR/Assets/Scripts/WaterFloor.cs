using UnityEngine;

// Makes a flat plane look like rippling water: builds a tiling wave normal map
// and scrolls it so the reflections move.
[RequireComponent(typeof(Renderer))]
public class WaterFloor : MonoBehaviour
{
    [Tooltip("How fast the ripples drift")]
    public Vector2 scrollSpeed = new Vector2(0.02f, 0.015f);
    [Tooltip("How many times the ripple pattern repeats across the plane")]
    public float tiling = 6f;
    [Tooltip("Ripple strength")]
    [Range(0f, 2f)] public float waveStrength = 0.6f;

    private Material mat;
    private Vector2 offset;

    void Awake()
    {
        mat = GetComponent<Renderer>().material;
        mat.SetTexture("_BumpMap", MakeNormalMap(256));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.mainTextureScale = Vector2.one * tiling;
    }

    void Update()
    {
        offset += scrollSpeed * Time.deltaTime;
        mat.mainTextureOffset = offset;
    }

    private Texture2D MakeNormalMap(int size)
    {
        // Sum of sine waves with whole-number frequencies, so the texture tiles seamlessly.
        Vector3[] waves = { new Vector3(1, 2, 0.3f), new Vector3(3, -1, 1.7f), new Vector3(-2, 3, 4.1f), new Vector3(5, 4, 2.2f), new Vector3(-4, 6, 5.6f) };
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (float)x / size, v = (float)y / size, dx = 0f, dy = 0f;
            foreach (var w in waves)
            {
                float k = 1f / Mathf.Sqrt(w.x * w.x + w.y * w.y);
                float c = Mathf.Cos(2f * Mathf.PI * (w.x * u + w.y * v) + w.z) * k;
                dx += w.x * c; dy += w.y * c;
            }
            Vector3 n = new Vector3(-dx * waveStrength * 0.3f, -dy * waveStrength * 0.3f, 1f).normalized;
            float r = n.x * 0.5f + 0.5f, g = n.y * 0.5f + 0.5f;
            px[y * size + x] = new Color(r, g, n.z * 0.5f + 0.5f, r); // works for both RGB and AG normal encodings
        }
        tex.SetPixels(px);
        tex.Apply(true);
        return tex;
    }
}
