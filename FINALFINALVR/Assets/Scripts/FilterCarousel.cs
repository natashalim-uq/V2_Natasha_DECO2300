using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Extra scene changes for one filter, e.g. "Stars": swap the sky and hide the walls
[System.Serializable]
public class FilterScene
{
    [Tooltip("Sky to switch to for this filter, e.g. a stars skybox. Leave empty to keep the normal sky.")]
    public Material skybox;
    [Tooltip("Objects that appear only with this filter, e.g. star particles")]
    public GameObject[] show;
    [Tooltip("Objects made invisible while this filter is on, e.g. the walls. Their colliders stay, so they still block the player.")]
    public GameObject[] hide;
}

public class FilterCarousel : MonoBehaviour
{
    // Filter icon GameObject array cycles through carousel, only one icon is visible at a time
    public GameObject[] filterIcons;

    [Header("Optional: name and live preview (Design Evaluation 2)")]
    [Tooltip("Shows the selected filter's name, e.g. 'Pink (1 of 3)'")]
    public TMP_Text filterNameLabel;

    [Tooltip("Name for each icon, in the same order. Leave empty to use the icon object names.")]
    public string[] filterNames;

    [Tooltip("Tinted the selected filter's colour while browsing: the mirror's preview overlay and your hand models")]
    public Renderer[] previewRenderers;

    [Tooltip("Shows 'Applied: Pink' when the Apply button is pressed")]
    public TMP_Text appliedLabel;

    [Header("Optional: filter the whole environment")]
    [Tooltip("The scene's Global Volume. Its Color Filter is set to the filter colour, so everything you see is tinted.")]
    public Volume environmentVolume;

    [Header("Optional: real filter looks (e.g. Instagram-style)")]
    [Tooltip("A second Global Volume, with a higher Priority than the main one. Its profile is swapped to the selected look.")]
    public Volume filterVolume;

    [Tooltip("One Volume Profile per filter, in the same order as the icons. Each can use any effects: Split Toning, Color Adjustments, Vignette...")]
    public VolumeProfile[] filterLooks;

    [Tooltip("Optional, one per filter in the same order as the icons: sky and objects to swap (leave an entry empty for filters that only change colour)")]
    public FilterScene[] filterScenes;

    private Material defaultSkybox;

    [Tooltip("Big centred name that pops up when you switch filter, then fades away (like Instagram)")]
    public TMP_Text flashNameLabel;
    public float flashSeconds = 2f;

    private Coroutine flashRoutine;
    private ColorAdjustments colorAdjustments;

    // The filter last confirmed with Apply (-1 = none). Close clears it.
    private int appliedIndex = -1;

    // Each preview renderer's colour before any filter, for going back to "no filter"
    private Color[] originalColors;

    // Tracks the index of the currently displayed filter icon
    private int currentIndex = 0;

    // Each preview renderer's own alpha, so a see-through overlay stays see-through
    private float[] previewAlphas;

    private void Start()
    {
        previewAlphas = new float[previewRenderers.Length];
        originalColors = new Color[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            originalColors[i] = previewRenderers[i] != null ? GetColor(previewRenderers[i].material) : Color.white;
            previewAlphas[i] = originalColors[i].a;
        }

        if (environmentVolume != null)
        {
            // Use the volume's Color Adjustments, adding one if it doesn't have it yet
            VolumeProfile profile = environmentVolume.profile;
            if (!profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;
            colorAdjustments.colorFilter.overrideState = true;
        }

        // Real looks: start with no look showing
        if (filterVolume != null) filterVolume.weight = 0f;

        if (flashNameLabel != null) flashNameLabel.gameObject.SetActive(false);
        // Nothing is applied yet, so the applied label starts empty
        if (appliedLabel != null) appliedLabel.SetText("");
        defaultSkybox = RenderSettings.skybox;
        ApplyScene(-1);

        // On scene start, make sure only the first filter icon is visible (no tint until the arrows are used)
        ShowCurrent(false);
    }

    // Wire the Right Arrow button's On Click() to this
    public void NextFilter()
    {
        currentIndex++;

        // Wrap back to the beginning on last icon
        if (currentIndex >= filterIcons.Length)
        {
            currentIndex = 0;
        }

        ShowCurrent(); // Refresh which icon is visible
    }

    // Wire the Left Arrow button's On Click() to this
    public void PreviousFilter()
    {
        currentIndex--; // Move backward one filter

        // Wrap around to end on the last icon
        if (currentIndex < 0)
        {
            currentIndex = filterIcons.Length - 1;
        }

        ShowCurrent(); // Refresh which icon is visible
    }

    // Wire your Apply (confirm) button to this
    public void ConfirmSelection()
    {
        // Log name of the selected filter in the Console, and show it on the panel
        Debug.Log("Selected filter: " + CurrentName());
        appliedIndex = currentIndex;
        if (appliedLabel != null) appliedLabel.SetText("Applied: " + CurrentName());
    }

    // Wire the Close button to this. Turns the filter off and goes back to the original room.
    public void ClosePanel()
    {
        // Back to the original room. The panel stays on the mirror so a filter can be picked again.
        appliedIndex = -1;
        if (appliedLabel != null) appliedLabel.SetText("");
        if (colorAdjustments != null) colorAdjustments.colorFilter.value = Color.white;
        if (filterVolume != null) filterVolume.weight = 0f;
        ApplyScene(-1);
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] != null) SetColor(previewRenderers[i].material, originalColors[i]);
        }
    }

    // Shows only the icon at currentIndex, hides all others, and updates the label and preview
    private void ShowCurrent(bool preview = true)
    {
        for (int i = 0; i < filterIcons.Length; i++)
        {
            // SetActive(true) only for the matching index, false for everything else
            filterIcons[i].SetActive(i == currentIndex);
        }

        if (filterNameLabel != null)
        {
            filterNameLabel.SetText(CurrentName() + "  (" + (currentIndex + 1) + " of " + filterIcons.Length + ")");
        }

        // Live preview while browsing
        if (preview)
        {
            ApplyColor(FilterColor(currentIndex));
            ApplyLook(currentIndex);
            ApplyScene(currentIndex);
            FlashName();
        }
    }

    // Undoes every filter's sky and object changes, then applies this one's (-1 = none)
    private void ApplyScene(int index)
    {
        if (filterScenes == null || filterScenes.Length == 0) return;

        foreach (var fs in filterScenes)
        {
            if (fs == null) continue;
            if (fs.show != null) foreach (var go in fs.show) if (go != null) go.SetActive(false);
            if (fs.hide != null) foreach (var go in fs.hide) SetVisible(go, true);
        }

        Material sky = defaultSkybox;
        if (index >= 0 && index < filterScenes.Length && filterScenes[index] != null)
        {
            var fs = filterScenes[index];
            if (fs.show != null) foreach (var go in fs.show) if (go != null) go.SetActive(true);
            if (fs.hide != null) foreach (var go in fs.hide) SetVisible(go, false);
            if (fs.skybox != null) sky = fs.skybox;
        }

        if (RenderSettings.skybox != sky)
        {
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }
    }

    // Shows or hides an object's meshes but leaves its colliders on, so hidden walls still stop the player
    private static void SetVisible(GameObject go, bool visible)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
    }

    private void FlashName()
    {
        if (flashNameLabel == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashNameRoutine());
    }

    // Shows the name, holds it, then fades it out over half a second
    private IEnumerator FlashNameRoutine()
    {
        flashNameLabel.SetText(CurrentName());
        flashNameLabel.gameObject.SetActive(true);
        flashNameLabel.alpha = 1f;
        yield return new WaitForSeconds(flashSeconds);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            flashNameLabel.alpha = 1f - t / 0.5f;
            yield return null;
        }
        flashNameLabel.gameObject.SetActive(false);
    }

    // Swaps the filter volume to this filter's look. An empty slot (e.g. "Original") turns the look off.
    private void ApplyLook(int index)
    {
        if (filterVolume == null || filterLooks == null || filterLooks.Length == 0) return;
        if (index < 0 || index >= filterLooks.Length || filterLooks[index] == null)
        {
            // No look for this entry (e.g. "Original"): show the room as it is
            filterVolume.weight = 0f;
            return;
        }
        filterVolume.sharedProfile = filterLooks[index];
        filterVolume.weight = 1f;
    }

    // The filter's colour is the colour of its icon Image
    private Color FilterColor(int index)
    {
        var image = filterIcons[index].GetComponent<Image>();
        return image != null ? image.color : Color.white;
    }

    // Tints the environment, the mirror overlay and the hands
    private void ApplyColor(Color filterColor)
    {
        bool usingLooks = filterVolume != null && filterLooks != null && filterLooks.Length > 0;
        if (colorAdjustments != null && !usingLooks)
        {
            Color environmentColor = filterColor;
            environmentColor.a = 1f;
            colorAdjustments.colorFilter.value = environmentColor;
        }

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] == null) continue;
            Color c = filterColor;
            c.a = previewAlphas[i];
            SetColor(previewRenderers[i].material, c);
        }
    }

    private string CurrentName()
    {
        if (filterNames != null && currentIndex < filterNames.Length && !string.IsNullOrEmpty(filterNames[currentIndex]))
        {
            return filterNames[currentIndex];
        }
        return filterIcons[currentIndex].name;
    }

    // URP materials call their colour _BaseColor, older ones _Color
    private static Color GetColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.color;
        return Color.white;
    }

    private static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.color = c;
    }
}
