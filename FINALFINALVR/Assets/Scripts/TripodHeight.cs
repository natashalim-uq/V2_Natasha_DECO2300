using UnityEngine;

// Stretches the tripod model up or down. Hook a UI Slider (Min 0.5, Max 2, Value 1)
// On Value Changed -> TripodHeight.SetHeight (Dynamic float).
public class TripodHeight : MonoBehaviour
{
    [Tooltip("The tripod model to stretch. Leave empty to use this object.")]
    public Transform tripodModel;

    Vector3 startScale;

    void Awake()
    {
        if (tripodModel == null) tripodModel = transform;
        startScale = tripodModel.localScale;
    }

    // multiplier: 1 = original height, 0.5 = half, 2 = double
    public void SetHeight(float multiplier)
    {
        tripodModel.localScale = new Vector3(startScale.x, startScale.y * multiplier, startScale.z);
    }
}
