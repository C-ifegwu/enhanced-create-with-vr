using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Handlers for the Start UI and the in-project UI of the VR Room scene.
/// Buttons call these methods through their onClick events.
/// </summary>
public class VRRoomMenu : MonoBehaviour
{
    [Tooltip("Start UI shown when the scene begins.")]
    public GameObject startPanel;
    [Tooltip("In-project UI that is available while playing.")]
    public GameObject hudPanel;
    [Tooltip("Teleportation provider on the XR rig.")]
    public TeleportationProvider teleportProvider;
    [Tooltip("Navigator that fades and loads scenes.")]
    public SceneNavigator navigator;
    [Tooltip("Scene name of the Create with VR room.")]
    public string courseScene = "Chibueze_Room";
    [Tooltip("Parent of every object that Reset should put back (grabbables and the door).")]
    public Transform resettableRoot;

    private InputAction hudAction;
    private Camera head;

    private readonly List<Rigidbody> bodies = new List<Rigidbody>();
    private readonly List<Pose> poses = new List<Pose>();

    private void Awake()
    {
        if (resettableRoot != null)
            foreach (var rb in resettableRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                bodies.Add(rb);
                poses.Add(new Pose(rb.transform.position, rb.transform.rotation));
            }
        if (startPanel != null) startPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // Left controller Y button shows or hides the in-project UI in front of the player.
        hudAction = new InputAction("Toggle HUD", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        hudAction.performed += OnHudButton;
        hudAction.Enable();
    }

    private void OnDisable()
    {
        if (hudAction == null) return;
        hudAction.performed -= OnHudButton;
        hudAction.Dispose();
        hudAction = null;
    }

    private void OnHudButton(InputAction.CallbackContext ctx) { ToggleHud(); }

    /// <summary>Shows or hides the in-project UI. When shown it appears in front of the player.</summary>
    public void ToggleHud()
    {
        if (hudPanel == null || (startPanel != null && startPanel.activeSelf)) return;
        bool show = !hudPanel.activeSelf;
        hudPanel.SetActive(show);
        if (show) PlaceHudInFront();
    }

    private void PlaceHudInFront()
    {
        if (head == null) head = Camera.main;
        if (head == null) return;
        Vector3 flat = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
        hudPanel.transform.position = head.transform.position + flat * 1.0f + Vector3.down * 0.1f;
        hudPanel.transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
    }

    /// <summary>Hides the Start UI and shows the in-project UI.</summary>
    public void StartExperience()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (hudPanel != null) { hudPanel.SetActive(true); PlaceHudInFront(); }
    }

    /// <summary>Teleports the player to the given anchor, keeping the XR rig upright.</summary>
    /// <param name="anchor">Transform of the destination.</param>
    public void TeleportTo(Transform anchor)
    {
        if (teleportProvider == null || anchor == null) return;
        var request = new TeleportRequest
        {
            requestTime = Time.time,
            matchOrientation = MatchOrientation.TargetUpAndForward,
            destinationPosition = anchor.position,
            destinationRotation = anchor.rotation
        };
        teleportProvider.QueueTeleportRequest(request);
    }

    /// <summary>Releases every held object and returns objects and the door to their start poses.</summary>
    public void ResetObjects()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            var rb = bodies[i];
            if (rb == null) continue;
            if (rb.TryGetComponent<XRGrabInteractable>(out var grab) && grab.interactionManager != null && grab.isSelected)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.position = poses[i].position;
            rb.rotation = poses[i].rotation;
            rb.transform.SetPositionAndRotation(poses[i].position, poses[i].rotation);
        }
    }

    /// <summary>Fades out and loads the Create with VR room.</summary>
    public void GoToCourseScene()
    {
        if (navigator != null) navigator.GoToScene(courseScene);
    }
}
