using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

/// <summary>
/// Puts the player on the start anchor when a scene loads, whenever the headset is recentred, and whenever the
/// player's head leaves the room (for example after walking a few metres in the previous scene). Buttons can also
/// call <see cref="Recenter"/>.
/// </summary>
public class RoomSpawn : MonoBehaviour
{
    [Tooltip("XR Origin of the rig in this scene.")]
    public XROrigin origin;
    [Tooltip("Where the player's head should be after a recenter. The anchor's forward is the viewing direction.")]
    public Transform anchor;
    [Tooltip("Half size of the walkable room (metres) around the anchor. Zero disables the out-of-room guard.")]
    public Vector2 roomHalfExtents = new Vector2(3.2f, 2.2f);
    [Tooltip("Seconds the head may stay outside the room before the player is brought back.")]
    public float outsideGrace = 1.0f;

    private float outsideTime;
    private readonly System.Collections.Generic.List<XRInputSubsystem> subsystems = new System.Collections.Generic.List<XRInputSubsystem>();

    private void OnEnable()
    {
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var s in subsystems) s.trackingOriginUpdated += OnTrackingOriginUpdated;
    }

    private void OnDisable()
    {
        foreach (var s in subsystems) if (s != null) s.trackingOriginUpdated -= OnTrackingOriginUpdated;
        subsystems.Clear();
    }

    private void Start() { Recenter(); }

    private void OnTrackingOriginUpdated(XRInputSubsystem subsystem) { Recenter(); }

    private void Update()
    {
        if (origin == null || anchor == null || roomHalfExtents == Vector2.zero || origin.Camera == null) return;
        Vector3 d = origin.Camera.transform.position - anchor.position;
        bool outside = Mathf.Abs(d.x) > roomHalfExtents.x || Mathf.Abs(d.z) > roomHalfExtents.y;
        outsideTime = outside ? outsideTime + Time.unscaledDeltaTime : 0f;
        if (outsideTime > outsideGrace) { outsideTime = 0f; Recenter(); }
    }

    /// <summary>Moves the rig so the player's head is above the anchor and looks along the anchor's forward.</summary>
    public void Recenter()
    {
        if (origin == null || anchor == null || origin.Camera == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        origin.MatchOriginUpCameraForward(Vector3.up, forward.normalized);
        Vector3 cam = origin.Camera.transform.position;
        origin.MoveCameraToWorldLocation(new Vector3(anchor.position.x, cam.y, anchor.position.z));
    }
}
