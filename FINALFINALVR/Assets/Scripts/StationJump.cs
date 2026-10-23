using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Jumps the player to a station when a button on the Go To panel is pressed.
// Each spot is an empty GameObject on the floor whose blue Z arrow points at the station.
// Wire each button's On Click () to StationJump > JumpTo with that spot's number.
// WHY: in Design Evaluation 2, testers said there was too much walking between stations.
// One button per station gets them there instantly, already facing the right way.
public class StationJump : MonoBehaviour
{
    [Tooltip("Where each button takes you, in the same order as the buttons (e.g. Start, Record, Filters, Dance)")]
    public Transform[] spots;

    [Tooltip("The recording camera's lens. The Record button puts you just behind it, looking the way it films. Left empty, it looks for an object called \"Lens Camera\".")]
    public Transform recordCamera;
    [Tooltip("Which spot number is the Record one (0 = Start, 1 = Record, ...)")]
    public int recordSpotIndex = 1;
    [Tooltip("How far behind the camera you land, in metres")]
    public float distanceBehindCamera = 0.35f;

    // The XR Interaction Toolkit's teleport system, which actually moves the player
    private TeleportationProvider teleport;

    private void Start()
    {
        // Find the teleport system once at the start, instead of searching every time a button is pressed
        teleport = FindFirstObjectByType<TeleportationProvider>();
        if (teleport == null) Debug.LogWarning("StationJump: no Teleportation Provider found in the XR Origin.");
        if (recordCamera == null)
        {
            GameObject lens = GameObject.Find("Lens Camera");
            if (lens != null) recordCamera = lens.transform;
        }
    }

    // index = which spot (0 = Start, 1 = Record, 2 = Filters, 3 = Dance)
    public void JumpTo(int index)
    {
        // Record: go to wherever the camera is right now (it can be picked up and moved),
        // standing just behind it and facing the same way it films, so its screen is in front of you
        // WHY: natasha wanted the Record button to take you to the camera, not to the tripod.
        if (index == recordSpotIndex && recordCamera != null && teleport != null)
        {
            Vector3 look = Vector3.ProjectOnPlane(recordCamera.forward, Vector3.up);
            // If the camera is pointing straight up or down there's no sensible "behind", so use the normal spot
            if (look.sqrMagnitude > 0.01f)
            {
                look.Normalize();
                Vector3 stand = recordCamera.position - look * distanceBehindCamera;
                stand.y = spots != null && index < spots.Length && spots[index] != null ? spots[index].position.y : 0f;   // keep your feet on the floor
                teleport.QueueTeleportRequest(new TeleportRequest
                {
                    destinationPosition = stand,
                    destinationRotation = Quaternion.LookRotation(look, Vector3.up),
                    matchOrientation = MatchOrientation.TargetUpAndForward,
                });
                Debug.Log("StationJump: went to the camera");
                return;
            }
        }

        // Safety checks: stop quietly if something is missing or the number is out of range,
        // rather than crashing the app with an error
        if (teleport == null || spots == null || index < 0 || index >= spots.Length || spots[index] == null) return;
        Transform spot = spots[index];

        // Use the toolkit's own teleport (instead of moving the player directly) so it works the same
        // way as normal teleporting and plays nicely with the rest of the XR rig
        teleport.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = spot.position,
            // Face the way the spot's blue arrow points. ProjectOnPlane flattens the direction
            // so the player never ends up tilted looking up or down.
            destinationRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(spot.forward, Vector3.up), Vector3.up),
            matchOrientation = MatchOrientation.TargetUpAndForward,
        });
        Debug.Log("StationJump: went to " + spot.name);
    }
}
