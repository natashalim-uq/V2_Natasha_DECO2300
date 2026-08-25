using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CameraScreenButton : MonoBehaviour
{
    public GameObject recordingUI;

    private void Awake()
    {
        GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>().selectEntered.AddListener(OnScreenPressed);
    }

    private void OnScreenPressed(SelectEnterEventArgs args)
    {
        recordingUI.SetActive(true);
    }
}
