using UnityEngine;

// Shows or hides a hint panel when its "?" button is pressed, and hides it again after a while.
// Wire the "?" button's On Click () to HintToggle > Toggle.
// WHY: in Design Evaluation 2, beginners needed help with the controls, but showing instructions
// all the time would clutter the view.
// A "?" button means help is there only when someone asks for it.
public class HintToggle : MonoBehaviour
{
    [Tooltip("The panel holding the hint text")]
    public GameObject hintPanel;

    [Tooltip("Hide the hint again after this many seconds (0 = stay until pressed again)")]
    public float autoHideSeconds = 10f;

    // The time (in seconds since the app started) when the hint should disappear
    private float hideAt;

    private void Start()
    {
        // Hints start hidden, so the station looks clean until someone asks for help
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    // Called by the "?" button. Public so it shows up in the button's On Click () list.
    public void Toggle()
    {
        if (hintPanel == null) return;           // nothing to show, so stop here instead of erroring
        bool show = !hintPanel.activeSelf;        // if it's showing, hide it; if it's hidden, show it
        hintPanel.SetActive(show);
        hideAt = Time.time + autoHideSeconds;     // start the auto-hide countdown from now
    }

    // Update runs every frame. Here it just checks whether the countdown has run out.
    private void Update()
    {
        // Only hide it if: it exists, it's showing, auto-hide is turned on (> 0), and the time has passed
        if (hintPanel != null && hintPanel.activeSelf && autoHideSeconds > 0f && Time.time > hideAt)
        {
            hintPanel.SetActive(false);
        }
    }
}
