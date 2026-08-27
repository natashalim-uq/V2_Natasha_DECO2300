using UnityEngine;
using TMPro;

public class RecordToggle : MonoBehaviour
{
    public TMP_Text statusText; // drag your Text object here in the Inspector
    private bool isRecording = false;

    // Call this from the Button's On Click ()
    public void ToggleRecording()
    {
        isRecording = !isRecording;

        if (isRecording)
        {
            statusText.SetText("Recording...");
        }
        else
        {
            statusText.SetText("Stopped");
        }
    }
}