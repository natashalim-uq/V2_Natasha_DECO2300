using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Extra scene changes for one filter, e.g. "Stars": swap the sky and hide the walls
// [System.Serializable] so it shows up (and saves) in the Inspector as a little group of fields
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

// The filter panel on the mirror: < and > browse filters like Instagram's carousel, Apply confirms one,
// Close goes back to the normal room. While browsing, the whole room changes so you see a live preview.
// WHY: Design Evaluation 1 asked for a live preview and clearer Apply/Close buttons; Design Evaluation 2
// said it was unclear which filter was selected and wanted the filter on the whole environment
// (and on yourself), so there's a name label, a big name flash, and the hands are tinted too.
// The script uses several layers, each optional (if a field is empty that layer is skipped):
//   1. Simple tint: one colour over everything (Color Adjustments on the main Global Volume)
//   2. Real looks: a full Volume Profile per filter (Split Toning, Vignette...) like real Instagram filters
//   3. Scene changes: swap the sky / show or hide objects (Stars, Water)
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

    // The normal sky, remembered at the start so it can be put back
    private Material defaultSkybox;

    [Tooltip("Big centred name that pops up when you switch filter, then fades away (like Instagram)")]
    public TMP_Text flashNameLabel;
    public float flashSeconds = 2f;

    // The running fade animation, kept so it can be restarted if you switch filter again quickly
    private Coroutine flashRoutine;
    // The colour-tint effect inside the main Global Volume
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
        // Remember the original colour of everything that gets tinted, so Close can undo it
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
            // (TryGet returns true and fills colorAdjustments if it's found)
            VolumeProfile profile = environmentVolume.profile;
            if (!profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;
            // "overrideState = true" means: this volume controls the Color Filter setting
            colorAdjustments.colorFilter.overrideState = true;
        }

        // Real looks: start with no look showing
        // (weight 0 = the volume has no effect, 1 = full effect)
        if (filterVolume != null) filterVolume.weight = 0f;

        if (flashNameLabel != null) flashNameLabel.gameObject.SetActive(false);
        // Nothing is applied yet, so the applied label starts empty
        if (appliedLabel != null) appliedLabel.SetText("");
        defaultSkybox = RenderSettings.skybox;
        ApplyScene(-1);   // -1 = no filter: normal sky, walls visible

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
        // (The room already shows the filter from browsing, so Apply just confirms it and gives feedback)
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
        if (colorAdjustments != null) colorAdjustments.colorFilter.value = Color.white;   // white = no tint
        if (filterVolume != null) filterVolume.weight = 0f;
        ApplyScene(-1);
        // Put the hands and mirror overlay back to their own colours
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] != null) SetColor(previewRenderers[i].material, originalColors[i]);
        }
    }

    // Shows only the icon at currentIndex, hides all others, and updates the label and preview
    // preview = false is used at the start, so the room isn't tinted before anyone touches the panel
    private void ShowCurrent(bool preview = true)
    {
        for (int i = 0; i < filterIcons.Length; i++)
        {
            // SetActive(true) only for the matching index, false for everything else
            filterIcons[i].SetActive(i == currentIndex);
        }

        if (filterNameLabel != null)
        {
            // +1 because people count from 1, but arrays count from 0
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
    // (Undoing everything first means you can jump from any filter to any other without leftovers)
    private void ApplyScene(int index)
    {
        if (filterScenes == null || filterScenes.Length == 0) return;

        // Step 1: reset. Hide every "show" object and make every "hide" object visible again.
        foreach (var fs in filterScenes)
        {
            if (fs == null) continue;
            if (fs.show != null) foreach (var go in fs.show) if (go != null) go.SetActive(false);
            if (fs.hide != null) foreach (var go in fs.hide) SetVisible(go, true);
        }

        // Step 2: apply the chosen filter's changes
        Material sky = defaultSkybox;
        if (index >= 0 && index < filterScenes.Length && filterScenes[index] != null)
        {
            var fs = filterScenes[index];
            if (fs.show != null) foreach (var go in fs.show) if (go != null) go.SetActive(true);
            if (fs.hide != null) foreach (var go in fs.hide) SetVisible(go, false);
            if (fs.skybox != null) sky = fs.skybox;
        }

        // Only change the sky if it's different (updating the lighting is a bit costly)
        if (RenderSettings.skybox != sky)
        {
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();   // refresh the lighting so objects match the new sky
        }
    }

    // Shows or hides an object's meshes but leaves its colliders on, so hidden walls still stop the player
    // (SetActive(false) would remove the collider too, and people could walk out of the room)
    private static void SetVisible(GameObject go, bool visible)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
    }

    private void FlashName()
    {
        if (flashNameLabel == null) return;
        // If a fade is already running (you pressed > quickly), stop it and start fresh
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashNameRoutine());
    }

    // Shows the name, holds it, then fades it out over half a second
    // (A coroutine, so it can wait over several frames without freezing the game)
    private IEnumerator FlashNameRoutine()
    {
        flashNameLabel.SetText(CurrentName());
        flashNameLabel.gameObject.SetActive(true);
        flashNameLabel.alpha = 1f;
        yield return new WaitForSeconds(flashSeconds);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            flashNameLabel.alpha = 1f - t / 0.5f;
            yield return null;   // wait one frame
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
        // One Volume, many profiles: swapping the profile is quicker than having six volumes in the scene
        filterVolume.sharedProfile = filterLooks[index];
        filterVolume.weight = 1f;
    }

    // The filter's colour is the colour of its icon Image
    // (So changing a filter's colour is as easy as recolouring its icon in the editor)
    private Color FilterColor(int index)
    {
        var image = filterIcons[index].GetComponent<Image>();
        return image != null ? image.color : Color.white;
    }

    // Tints the environment, the mirror overlay and the hands
    private void ApplyColor(Color filterColor)
    {
        // If real looks are set up, they handle the room's colour, so skip the simple tint
        // (otherwise the two would stack and look wrong)
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
            c.a = previewAlphas[i];   // keep each object's own see-through-ness
            SetColor(previewRenderers[i].material, c);
        }
    }

    // The name to show: from filterNames if one was typed in, otherwise the icon object's name
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
