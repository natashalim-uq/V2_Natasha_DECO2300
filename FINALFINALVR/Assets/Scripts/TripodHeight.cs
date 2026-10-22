using UnityEngine;

// Stretches the tripod model up or down. Hook a UI Slider (Min 0.5, Max 2, Value 1)
// On Value Changed -> TripodHeight.SetHeight (Dynamic float).
// WHY: people are different heights, so the tripod needs to be adjustable for the camera
// to film them properly. A slider is easier to use in VR than grabbing and stretching the legs.
public class TripodHeight : MonoBehaviour
{
    [Tooltip("The tripod model to stretch. Leave empty to use this object.")]
    public Transform tripodModel;

    // The tripod's size when the scene started. Every height is worked out from this,
    // so moving the slider back to 1 always gives the original size (no drifting).
    Vector3 startScale;

    // Awake runs once when the object loads (before Start)
    void Awake()
    {
        if (tripodModel == null) tripodModel = transform;   // fall back to this object if nothing was dragged in
        startScale = tripodModel.localScale;
    }

    // multiplier: 1 = original height, 0.5 = half, 2 = double
    // "Dynamic float" means the slider passes its current value straight into this method
    public void SetHeight(float multiplier)
    {
        // Only the Y (height) changes. X and Z stay the same so the legs don't get fatter or thinner.
        tripodModel.localScale = new Vector3(startScale.x, startScale.y * multiplier, startScale.z);
    }
}
