using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Small objective board: counts how many sockets are filled and shows the angle of the physics door.
/// Fires <see cref="onAllSocketsFilled"/> once when every socket holds its object.
/// </summary>
public class PuzzleTracker : MonoBehaviour
{
    [Tooltip("Sockets that must be filled.")]
    public XRSocketInteractor[] sockets;
    [Tooltip("Hinge joint of the physics door, used to show how far it is open.")]
    public HingeJoint doorHinge;
    [Tooltip("Text that shows the objectives.")]
    public TMP_Text label;
    [Tooltip("Sound played when an object snaps into a socket.")]
    public AudioSource snapSound;
    [Tooltip("Invoked once when all sockets are filled.")]
    public UnityEvent onAllSocketsFilled = new UnityEvent();

    private bool completed;
    private Quaternion closedRotation;
    private bool hasClosedRotation;

    private void OnEnable()
    {
        if (doorHinge != null && !hasClosedRotation) { closedRotation = doorHinge.transform.localRotation; hasClosedRotation = true; }
        foreach (var s in sockets)
        {
            if (s == null) continue;
            s.selectEntered.AddListener(OnSocketChanged);
            s.selectExited.AddListener(OnSocketChanged);
        }
        Refresh();
    }

    private void OnDisable()
    {
        foreach (var s in sockets)
        {
            if (s == null) continue;
            s.selectEntered.RemoveListener(OnSocketChanged);
            s.selectExited.RemoveListener(OnSocketChanged);
        }
    }

    private void Update()
    {
        if (doorHinge != null) Refresh();
    }

    private void OnSocketChanged(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (snapSound != null && snapSound.clip != null) snapSound.Play();
        Refresh();
    }

    private void OnSocketChanged(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        Refresh();
    }

    private int CountFilled()
    {
        int n = 0;
        foreach (var s in sockets) if (s != null && s.hasSelection) n++;
        return n;
    }

    private void Refresh()
    {
        int filled = CountFilled();
        if (!completed && sockets.Length > 0 && filled == sockets.Length)
        {
            completed = true;
            onAllSocketsFilled.Invoke();
        }
        if (filled < sockets.Length) completed = false;

        if (label == null) return;
        string door = "Door: closed";
        if (doorHinge != null)
        {
            // The drive measures the angle in world space, so it stays right while a hand holds the door
            // (the grab temporarily detaches the leaf from its parent, which changes its local rotation).
            var drive = doorHinge.GetComponent<DoorHandleDrive>();
            float a = drive != null ? Mathf.Abs(drive.CurrentAngle)
                    : hasClosedRotation ? Quaternion.Angle(closedRotation, doorHinge.transform.localRotation) : 0f;
            door = a < 3f ? "Door: closed" : $"Door: open {Mathf.RoundToInt(a)}°";
        }
        label.text = $"Objectives\nSockets filled: {filled}/{sockets.Length}\n{door}" + (completed ? "\nAll objects placed!" : string.Empty);
    }
}
