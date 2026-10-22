using UnityEngine;

// Shows or hides a hint panel when its "?" button is pressed, and hides it again after a while.
// Wire the "?" button's On Click () to HintToggle > Toggle.
public class HintToggle : MonoBehaviour
{
    [Tooltip("The panel holding the hint text")]
    public GameObject hintPanel;

    [Tooltip("Hide the hint again after this many seconds (0 = stay until pressed again)")]
    public float autoHideSeconds = 10f;

    private float hideAt;

    private void Start()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (hintPanel == null) return;
        bool show = !hintPanel.activeSelf;
        hintPanel.SetActive(show);
        hideAt = Time.time + autoHideSeconds;
    }

    private void Update()
    {
        if (hintPanel != null && hintPanel.activeSelf && autoHideSeconds > 0f && Time.time > hideAt)
        {
            hintPanel.SetActive(false);
        }
    }
}
