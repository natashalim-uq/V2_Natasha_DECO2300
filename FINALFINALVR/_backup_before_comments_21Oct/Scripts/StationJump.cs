using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Jumps the player to a station when a button on the Go To panel is pressed.
// Each spot is an empty GameObject on the floor whose blue Z arrow points at the station.
// Wire each button's On Click () to StationJump > JumpTo with that spot's number.
public class StationJump : MonoBehaviour
{
    [Tooltip("Where each button takes you, in the same order as the buttons (e.g. Start, Record, Filters, Dance)")]
    public Transform[] spots;

    private TeleportationProvider teleport;

    private void Start()
    {
        teleport = FindFirstObjectByType<TeleportationProvider>();
        if (teleport == null) Debug.LogWarning("StationJump: no Teleportation Provider found in the XR Origin.");
    }

    public void JumpTo(int index)
    {
        if (teleport == null || spots == null || index < 0 || index >= spots.Length || spots[index] == null) return;
        Transform spot = spots[index];
        teleport.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = spot.position,
            destinationRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(spot.forward, Vector3.up), Vector3.up),
            matchOrientation = MatchOrientation.TargetUpAndForward,
        });
        Debug.Log("StationJump: went to " + spot.name);
    }
}
