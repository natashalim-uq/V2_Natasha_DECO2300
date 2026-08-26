using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CameraScreenButton : MonoBehaviour
{
    public GameObject recordingUI;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    private void Awake()
    {
        // Try to get XR interactable if it exists
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        if (interactable != null)
        {
            interactable.activated.AddListener(OnXRActivated);
        }
    }

    // XR interaction (right-click in simulator, poke/touch in VR)
    private void OnXRActivated(ActivateEventArgs args)
    {
        ShowUI();
    }

    // Mouse click interaction (left-click on collider)
    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown fired");
        ShowUI();
    }

    private void ShowUI()
    {
        Debug.Log("ShowUI called, recordingUI is " + (recordingUI != null ? "assigned" : "NULL"));
        if (recordingUI != null)
        {
            recordingUI.SetActive(true);
        }
    }
}