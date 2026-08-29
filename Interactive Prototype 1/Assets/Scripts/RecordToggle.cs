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
        // Flip the boolean: true becomes false, false becomes true
        isRecording = !isRecording;

        // Update the on-screen text based on the new state
        if (isRecording)
        {
            // Just turned recording ON
            statusText.SetText("Recording...");
        }
        else
        {
            // Just turned recording OFF
            statusText.SetText("Stopped");
        }
    }
}