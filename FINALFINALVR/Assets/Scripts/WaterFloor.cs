using UnityEngine;

// Makes a flat plane look like rippling water: builds a tiling wave normal map
// and scrolls it so the reflections move.
// WHY: this is for the "Water" filter. Making the ripple texture in code means no image file
// to import, and moving a texture is far cheaper for the Quest than moving real waves (vertices).
// [RequireComponent] makes Unity add a Renderer automatically, because this script can't work without one
[RequireComponent(typeof(Renderer))]
public class WaterFloor : MonoBehaviour
{
    [Tooltip("How fast the ripples drift")]
    public Vector2 scrollSpeed = new Vector2(0.02f, 0.015f);   // slightly different x and y so it drifts diagonally
    [Tooltip("How many times the ripple pattern repeats across the plane")]
    public float tiling = 6f;
    [Tooltip("Ripple strength")]
    [Range(0f, 2f)] public float waveStrength = 0.6f;

    private Material mat;
    private Vector2 offset;   // how far the texture has scrolled so far

    void Awake()
    {
        mat = GetComponent<Renderer>().material;
        // A "normal map" tells the lighting which way each tiny bit of the surface is facing.
        // Bumpy normals make smooth, flat geometry reflect light like it has ripples.
        mat.SetTexture("_BumpMap", MakeNormalMap(256));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");   // tells the URP shader to actually use the normal map
        mat.mainTextureScale = Vector2.one * tiling;
    }

    void Update()
    {
        // Move the texture a little each frame. Time.deltaTime keeps the speed the same
        // no matter the frame rate.
        offset += scrollSpeed * Time.deltaTime;
        mat.mainTextureOffset = offset;
    }

    private Texture2D MakeNormalMap(int size)
    {
        // Sum of sine waves with whole-number frequencies, so the texture tiles seamlessly.
        // Each Vector3 is one wave: x and y = direction/how many ripples across, z = a phase shift
        // so the waves don't all line up and look artificial.
        Vector3[] waves = { new Vector3(1, 2, 0.3f), new Vector3(3, -1, 1.7f), new Vector3(-2, 3, 4.1f), new Vector3(5, 4, 2.2f), new Vector3(-4, 6, 5.6f) };
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        tex.wrapMode = TextureWrapMode.Repeat;   // repeat the pattern instead of stretching it
        var px = new Color[size * size];
        // Go through every pixel of the texture
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // u, v = position on the texture from 0 to 1. dx, dy = how steep the surface is here.
            float u = (float)x / size, v = (float)y / size, dx = 0f, dy = 0f;
            foreach (var w in waves)
            {
                // k scales down high-frequency waves so small ripples aren't as strong as big ones
                float k = 1f / Mathf.Sqrt(w.x * w.x + w.y * w.y);
                // Cos is the slope of a sine wave, which is what the normal needs
                float c = Mathf.Cos(2f * Mathf.PI * (w.x * u + w.y * v) + w.z) * k;
                dx += w.x * c; dy += w.y * c;
            }
            // Turn the slope into a direction (the "normal") pointing mostly up (z = 1)
            Vector3 n = new Vector3(-dx * waveStrength * 0.3f, -dy * waveStrength * 0.3f, 1f).normalized;
            // Directions go from -1 to 1 but colours go from 0 to 1, so squash: * 0.5 + 0.5
            float r = n.x * 0.5f + 0.5f, g = n.y * 0.5f + 0.5f;
            px[y * size + x] = new Color(r, g, n.z * 0.5f + 0.5f, r); // works for both RGB and AG normal encodings
        }
        tex.SetPixels(px);
        tex.Apply(true);   // send the pixels to the graphics card (true = also make mipmaps for distance)
        return tex;
    }
}
