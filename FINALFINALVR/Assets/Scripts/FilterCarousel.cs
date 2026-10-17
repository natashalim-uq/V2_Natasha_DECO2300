using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    private ColorAdjustments colorAdjustments;

    // The filter last confirmed with Apply (-1 = none yet). Close goes back to this.
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

    // Wire the Close button to this. Anything previewed but not applied is undone, then the panel hides.
    public void ClosePanel()
    {
        if (appliedIndex >= 0)
        {
            ApplyColor(FilterColor(appliedIndex));
        }
        else
        {
            // Nothing applied yet: back to no filter
            if (colorAdjustments != null) colorAdjustments.colorFilter.value = Color.white;
            for (int i = 0; i < previewRenderers.Length; i++)
            {
                if (previewRenderers[i] != null) SetColor(previewRenderers[i].material, originalColors[i]);
            }
        }
        gameObject.SetActive(false);
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
        if (preview) ApplyColor(FilterColor(currentIndex));
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
        if (colorAdjustments != null)
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
