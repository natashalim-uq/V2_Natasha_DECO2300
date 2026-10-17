using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Lights an object up while a hand is near it (or pointing at it), so people can see what they can grab.
// Can also show a "how to grab" prompt until the object has been picked up once.
// Add it next to an XR Grab Interactable (or XR Simple Interactable).
public class GrabHighlight : MonoBehaviour
{
    [Tooltip("Colour mixed into the object's own colour while a hand is near")]
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);

    [Range(0f, 1f)]
    public float highlightAmount = 0.5f;

    [Tooltip("Optional: a small 3D text such as 'Close your hand to pick me up'. Hidden after the first grab.")]
    public GameObject grabPrompt;

    private XRBaseInteractable interactable;
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isHighlighted;
    private bool hasBeenGrabbed;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        // Remember each part's own colour (skipping the prompt text) so it can be restored
        var all = GetComponentsInChildren<Renderer>();
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in all)
        {
            if (grabPrompt != null && r.transform.IsChildOf(grabPrompt.transform)) continue;
            list.Add(r);
        }
        renderers = list.ToArray();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = GetColor(renderers[i].material);
        }

        if (grabPrompt != null) grabPrompt.SetActive(false);
    }

    private void Update()
    {
        if (interactable == null) return;

        // A hand is near if anything other than a socket (the tripod) is hovering it
        bool handNear = false;
        foreach (var interactor in interactable.interactorsHovering)
        {
            if (!(interactor is XRSocketInteractor)) { handNear = true; break; }
        }

        // A hand is holding it if anything other than a socket has selected it
        bool heldByHand = false;
        foreach (var interactor in interactable.interactorsSelecting)
        {
            if (!(interactor is XRSocketInteractor)) { heldByHand = true; break; }
        }
        if (heldByHand) hasBeenGrabbed = true;

        bool shouldHighlight = handNear && !heldByHand;
        if (shouldHighlight != isHighlighted)
        {
            isHighlighted = shouldHighlight;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = isHighlighted
                    ? Color.Lerp(originalColors[i], highlightColor, highlightAmount)
                    : originalColors[i];
                SetColor(renderers[i].material, c);
            }
        }

        if (grabPrompt != null)
        {
            bool showPrompt = isHighlighted && !hasBeenGrabbed;
            if (grabPrompt.activeSelf != showPrompt) grabPrompt.SetActive(showPrompt);
        }
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
