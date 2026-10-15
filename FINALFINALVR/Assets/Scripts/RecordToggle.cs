using UnityEngine;
using TMPro;

public class RecordToggle : MonoBehaviour
{
    public TMP_Text statusText;

    // Tracks whether we are currently "recording" or not
    private bool isRecording = false;

    // Method linked to a Button's OnClick() event in the Inspector
    // Each click flips the recording state and updates the UI text accordingly
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