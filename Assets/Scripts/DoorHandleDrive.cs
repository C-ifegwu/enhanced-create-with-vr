using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Moves the physics door while a hand holds a handle.
/// The hinge joint stays in charge of the physics (limits, collisions, inertia). The hand does not drag the leaf
/// around directly, because a hand that moves far from the door would ask the solver for impossible speeds and
/// tear the joint apart. Instead, the angle of the hand around the hinge becomes a target, and the hinge motor
/// turns the leaf towards it with a limited speed and force. The leaf can never leave its hinge, it cannot be
/// pushed past its limits, and it still collides with the world.
/// </summary>
[RequireComponent(typeof(HingeJoint), typeof(XRGrabInteractable))]
public class DoorHandleDrive : MonoBehaviour
{
    [Tooltip("Fastest the door turns while held, in degrees per second.")]
    public float maxSpeed = 140f;
    [Tooltip("How strongly the door chases the hand (speed per degree of error).")]
    public float gain = 10f;
    [Tooltip("Maximum motor torque.")]
    public float motorForce = 150f;
    [Tooltip("Flip when the door turns the opposite way to the hand.")]
    public bool invert = false;

    public static System.Text.StringBuilder debug = new System.Text.StringBuilder(); static int dbgN;
    Quaternion closedRotation;
    public Vector3 ClosedEuler => closedRotation.eulerAngles;
    public float CurrentAngle => DoorAngle();
    HingeJoint hinge;
    XRGrabInteractable grab;
    IXRSelectInteractor holder;
    Vector3 refDir;       // direction from hinge to the hand at grab time, flattened
    float handAngleAtGrab, doorAngleAtGrab;

    void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        closedRotation = transform.rotation;
        grab = GetComponent<XRGrabInteractable>();
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.maxAngularVelocity = 6f; rb.solverIterations = 20; rb.solverVelocityIterations = 8; }
        grab.trackPosition = false;     // the hinge motor moves the door, not the hand directly
        grab.trackRotation = false;
        grab.throwOnDetach = false;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    Vector3 Axis => (closedRotation * hinge.axis).normalized;

    // HingeJoint.angle returns NaN for a joint that rests exactly at its start angle, so the door angle is
    // measured from the transform instead: signed rotation about the hinge axis, relative to the closed pose.
    float DoorAngle()
    {
        var rel = transform.rotation * Quaternion.Inverse(closedRotation);
        rel.ToAngleAxis(out float a, out Vector3 ax);
        if (float.IsNaN(a) || float.IsNaN(ax.x)) return 0f;
        if (a > 180f) a -= 360f;
        return a * Mathf.Sign(Vector3.Dot(ax, Axis) == 0f ? 1f : Vector3.Dot(ax, Axis));
    }
    Vector3 Pivot => transform.TransformPoint(hinge.anchor);

    Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Axis);

    float HandAngle(Vector3 handWorld)
    {
        float a = Vector3.SignedAngle(refDir, Flat(handWorld - Pivot), Axis);
        return invert ? -a : a;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        holder = args.interactorObject;
        var hand = holder.GetAttachTransform(grab).position;
        refDir = Flat(hand - Pivot);
        if (refDir.sqrMagnitude < 1e-4f) refDir = Flat(transform.forward);
        handAngleAtGrab = 0f;           // refDir is the hand direction at grab time
        doorAngleAtGrab = DoorAngle();
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (args.interactorObject != holder) return;
        holder = null;
        hinge.useMotor = false;         // the door swings freely again (damped by the Rigidbody)
    }

    void FixedUpdate()
    {
        if (holder == null) return;
        var hand = holder.GetAttachTransform(grab).position;
        if (Flat(hand - Pivot).sqrMagnitude < 1e-4f) return;
        float target = Mathf.Clamp(doorAngleAtGrab + (HandAngle(hand) - handAngleAtGrab), hinge.limits.min, hinge.limits.max);
        float error = Mathf.DeltaAngle(DoorAngle(), target);
        if (dbgN++ < 40 || float.IsNaN(error) && dbgN < 60) debug.AppendLine("t=" + Time.time.ToString("F2") + " door=" + DoorAngle().ToString("F1") + " handAng=" + HandAngle(hand).ToString("F1") + " target=" + target.ToString("F1") + " err=" + error.ToString("F1") + " pos=" + transform.position.ToString("F2") + " refDir=" + refDir.ToString("F2") + " hand=" + hand.ToString("F2") + " pivot=" + Pivot.ToString("F2"));
        if (float.IsNaN(error)) return;
        var m = hinge.motor;
        m.targetVelocity = Mathf.Clamp(error * gain, -maxSpeed, maxSpeed);
        m.force = motorForce;
        m.freeSpin = false;
        hinge.motor = m;
        hinge.useMotor = true;
    }
}
