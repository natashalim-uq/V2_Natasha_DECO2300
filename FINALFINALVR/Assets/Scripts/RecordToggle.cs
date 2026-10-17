using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordToggle : MonoBehaviour
{
    public TMP_Text statusText;

    [Tooltip("Optional: the CameraRecorder on the camera's lens, so Record saves a real video")]
    public CameraRecorder recorder;

    [Header("Optional: make recording obvious (Design Evaluations 1 and 2)")]
    [Tooltip("The Record button's own label, switches between 'Record' and 'Stop'")]
    public TMP_Text buttonLabel;

    [Tooltip("The Record button's Image, turns red while recording")]
    public Image buttonImage;

    [Tooltip("A red dot that blinks while recording")]
    public GameObject recordingDot;

    [Tooltip("Plays the start and stop sounds")]
    public AudioSource audioSource;
    public AudioClip startSound;
    public AudioClip stopSound;

    public Color idleColor = Color.white;
    public Color recordingColor = new Color(0.9f, 0.15f, 0.15f);

    // Tracks whether we are currently "recording" or not
    private bool isRecording = false;

    private void Start()
    {
        if (recordingDot != null) recordingDot.SetActive(false);
        if (buttonImage != null) buttonImage.color = idleColor;
    }

    // Method linked to a Button's OnClick() event in the Inspector
    // Each click flips the recording state and updates the UI accordingly
    public void ToggleRecording()
    {
        // Flip the boolean: true becomes false, false becomes true
        isRecording = !isRecording;

        if (isRecording)
        {
            // Just turned recording ON
            statusText.SetText("Recording...");
            statusText.color = recordingColor;
            if (buttonLabel != null) buttonLabel.SetText("Stop");
            if (buttonImage != null) buttonImage.color = recordingColor;
            if (audioSource != null && startSound != null) audioSource.PlayOneShot(startSound);
            if (recorder != null) recorder.StartRecording();
        }
        else
        {
            // Just turned recording OFF
            statusText.SetText("Stopped");
            if (recorder != null && recorder.StopRecording() != null)
            {
                statusText.SetText("Saved " + recorder.LastDuration.ToString("0") + " s video");
            }
            statusText.color = idleColor;
            if (buttonLabel != null) buttonLabel.SetText("Record");
            if (buttonImage != null) buttonImage.color = idleColor;
            if (recordingDot != null) recordingDot.SetActive(false);
            if (audioSource != null && stopSound != null) audioSource.PlayOneShot(stopSound);
        }
    }

    private void Update()
    {
        // Blink the red dot twice a second while recording
        if (isRecording && recordingDot != null)
        {
            recordingDot.SetActive(Mathf.Repeat(Time.time, 1f) < 0.5f);
        }
    }
}
