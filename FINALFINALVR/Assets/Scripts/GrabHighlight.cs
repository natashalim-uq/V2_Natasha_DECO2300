using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Lights an object up while a hand is near it (or pointing at it), so people can see what they can grab.
// Can also show a "how to grab" prompt until the object has been picked up once.
// Add it next to an XR Grab Interactable (or XR Simple Interactable).
// WHY: in Design Evaluations 1 and 2, beginners weren't sure what they could grab or how.
// Highlighting on hover is a common VR cue (affordance) that says "you can interact with this".
public class GrabHighlight : MonoBehaviour
{
    [Tooltip("Colour mixed into the object's own colour while a hand is near")]
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);   // warm yellow

    // 0 = no change, 1 = fully the highlight colour. 0.5 keeps the object recognisable.
    [Range(0f, 1f)]
    public float highlightAmount = 0.5f;

    [Tooltip("Optional: a small 3D text such as 'Close your hand to pick me up'. Hidden after the first grab.")]
    public GameObject grabPrompt;

    // The XR Toolkit component that knows which hands are hovering over / holding this object
    private XRBaseInteractable interactable;
    // Every visible part of the object, and each part's colour before highlighting
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isHighlighted;
    // Once someone has grabbed it, they know how, so the prompt never needs to show again
    private bool hasBeenGrabbed;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        // Remember each part's own colour (skipping the prompt text) so it can be restored
        // The object is made of several meshes (body, lens, strap...), so collect them all
        var all = GetComponentsInChildren<Renderer>();
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in all)
        {
            // Don't tint the prompt text, only the object itself
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

    // Checked every frame, because hands move constantly
    private void Update()
    {
        if (interactable == null) return;

        // A hand is near if anything other than a socket (the tripod) is hovering it
        // (The tripod's snap point is also an "interactor", so without this check the camera
        // would glow forever while it sits on the tripod.)
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

        // Glow when a hand is close but NOT already holding it (once it's in your hand, you don't need the hint)
        bool shouldHighlight = handNear && !heldByHand;
        // Only change colours when the state actually changes, not every frame (saves work)
        if (shouldHighlight != isHighlighted)
        {
            isHighlighted = shouldHighlight;
            for (int i = 0; i < renderers.Length; i++)
            {
                // Lerp blends two colours: halfway between the original and the yellow highlight
                Color c = isHighlighted
                    ? Color.Lerp(originalColors[i], highlightColor, highlightAmount)
                    : originalColors[i];
                SetColor(renderers[i].material, c);
            }
        }

        // Show the "how to grab" text only while it's glowing and before the first grab
        if (grabPrompt != null)
        {
            bool showPrompt = isHighlighted && !hasBeenGrabbed;
            if (grabPrompt.activeSelf != showPrompt) grabPrompt.SetActive(showPrompt);
        }
    }

    // URP materials call their colour _BaseColor, older ones _Color
    // (so these two helpers work with whatever material the imported model came with)
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
