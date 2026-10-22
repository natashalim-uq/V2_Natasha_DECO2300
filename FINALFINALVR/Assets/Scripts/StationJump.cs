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

    // The XR Interaction Toolkit's teleport system, which actually moves the player
    private TeleportationProvider teleport;

    private void Start()
    {
        // Find the teleport system once at the start, instead of searching every time a button is pressed
        teleport = FindFirstObjectByType<TeleportationProvider>();
        if (teleport == null) Debug.LogWarning("StationJump: no Teleportation Provider found in the XR Origin.");
    }

    // index = which spot (0 = Start, 1 = Record, 2 = Filters, 3 = Dance)
    public void JumpTo(int index)
    {
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
