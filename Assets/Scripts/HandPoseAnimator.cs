using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Animates a rigged hand model from the controller's analog inputs.
/// The trigger curls the index finger, the grip curls the middle, ring and little fingers and
/// pulls the thumb in. Finger bones are rotated around their local curl axis relative to the
/// rest pose, so no animation clips are needed and the pose follows the finger pressure smoothly.
/// Bone names follow the OpenXR hand skeleton: "L_IndexProximal", "R_RingDistal" and so on.
/// </summary>
public class HandPoseAnimator : MonoBehaviour
{
    /// <summary>Which controller drives this hand.</summary>
    public enum Side { Left, Right }

    [Tooltip("Left or right hand. Selects the controller and the bone name prefix.")]
    public Side side = Side.Left;
    [Tooltip("Local axis the finger bones curl around.")]
    public Vector3 curlAxis = Vector3.right;
    [Tooltip("How fast the pose follows the input, in blend units per second.")]
    public float speed = 12f;
    [Range(0f, 1f), Tooltip("Current grip value (set by the controller at runtime, editable to preview).")]
    public float grip;
    [Range(0f, 1f), Tooltip("Current trigger value (set by the controller at runtime, editable to preview).")]
    public float trigger;
    [Tooltip("Read the real controller. Turn off to pose the hand by hand in the inspector.")]
    public bool useControllerInput = true;

    private class Finger
    {
        public Transform[] joints;
        public Quaternion[] rest;
        public float[] maxCurl;
        public Vector3[] axis;
    }

    private Finger index, middle, ring, little, thumb;
    private InputAction gripAction, triggerAction;
    private float gripSmooth, triggerSmooth;
    private bool built;

    private void Awake() { Build(); }

    private void OnEnable()
    {
        if (!useControllerInput) return;
        string hand = side == Side.Left ? "LeftHand" : "RightHand";
        gripAction = new InputAction("Grip", InputActionType.Value, $"<XRController>{{{hand}}}/grip");
        triggerAction = new InputAction("Trigger", InputActionType.Value, $"<XRController>{{{hand}}}/trigger");
        gripAction.Enable();
        triggerAction.Enable();
    }

    private void OnDisable()
    {
        gripAction?.Dispose();
        triggerAction?.Dispose();
        gripAction = triggerAction = null;
    }

    private void Update()
    {
        if (useControllerInput && gripAction != null)
        {
            grip = gripAction.ReadValue<float>();
            trigger = triggerAction.ReadValue<float>();
        }
        gripSmooth = Mathf.MoveTowards(gripSmooth, grip, speed * Time.deltaTime);
        triggerSmooth = Mathf.MoveTowards(triggerSmooth, trigger, speed * Time.deltaTime);
        Apply(gripSmooth, triggerSmooth);
    }

    /// <summary>Immediately poses the hand (used by the editor preview).</summary>
    /// <param name="gripValue">0 = open, 1 = fist for the three lower fingers.</param>
    /// <param name="triggerValue">0 = open, 1 = index finger curled.</param>
    public void SetPose(float gripValue, float triggerValue)
    {
        Build();
        gripSmooth = grip = gripValue;
        triggerSmooth = trigger = triggerValue;
        Apply(gripSmooth, triggerSmooth);
    }

    private void Apply(float g, float t)
    {
        Curl(index, t);
        Curl(middle, g);
        Curl(ring, g);
        Curl(little, g);
        Curl(thumb, Mathf.Max(g * 0.8f, t * 0.35f));
    }

    private static void Curl(Finger f, float amount)
    {
        if (f == null) return;
        for (int i = 0; i < f.joints.Length; i++)
            if (f.joints[i] != null)
                f.joints[i].localRotation = f.rest[i] * Quaternion.AngleAxis(f.maxCurl[i] * amount, f.axis[i]);
    }

    private void Build()
    {
        if (built) return;
        built = true;
        string p = side == Side.Left ? "L_" : "R_";
        index = Make(p, "Index", new[] { 70f, 90f, 55f });
        middle = Make(p, "Middle", new[] { 75f, 95f, 60f });
        ring = Make(p, "Ring", new[] { 78f, 95f, 60f });
        little = Make(p, "Little", new[] { 80f, 95f, 60f });
        // Thumb: metacarpal swings across the palm, then the two phalanges bend.
        thumb = Make(p, "Thumb", new[] { 20f, 35f, 45f }, new[] { "Metacarpal", "Proximal", "Distal" });
        if (thumb != null)
        {
            // The thumb bones are rotated differently from the finger bones, so they use their own axis.
            Vector3 thumbAxis = new Vector3(0f, side == Side.Left ? -1f : 1f, 0f);
            thumb.axis[0] = thumbAxis;
            thumb.axis[1] = Vector3.right;
            thumb.axis[2] = Vector3.right;
        }
    }

    private Finger Make(string prefix, string finger, float[] curls, string[] segments = null)
    {
        segments = segments ?? new[] { "Proximal", "Intermediate", "Distal" };
        var f = new Finger
        {
            joints = new Transform[segments.Length],
            rest = new Quaternion[segments.Length],
            maxCurl = curls,
            axis = new Vector3[segments.Length]
        };
        for (int i = 0; i < segments.Length; i++)
        {
            f.joints[i] = FindDeep(transform, prefix + finger + segments[i]);
            f.rest[i] = f.joints[i] != null ? f.joints[i].localRotation : Quaternion.identity;
            f.axis[i] = curlAxis;
        }
        return f;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
