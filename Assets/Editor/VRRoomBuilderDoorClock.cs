// Phase 3 (physics door) and phase 4 (wall clock) of the VR Room builder.
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static partial class VRRoomBuilder
{
    // ---------------------------------------------------------------- Phase 3: physics door
    [MenuItem("Tools/Create With VR/VR Room/3 Physics door")]
    public static void Phase3()
    {
        var frame = FindT("GlassDoor");
        var leaf = frame.Find("glass_panel_1_door");
        if (leaf == null) throw new System.Exception("door leaf not found");

        // The shipped door is animated. Remove the Animator so only physics moves the leaf.
        var animator = frame.GetComponent<Animator>();
        if (animator != null) UnityEngine.Object.DestroyImmediate(animator);
        var leafAnim = leaf.GetComponent<Animator>();
        if (leafAnim != null) UnityEngine.Object.DestroyImmediate(leafAnim);

        // Rigidbody on the leaf, joint anchored at the leaf pivot (the hinge side of the model).
        var rb = Get<Rigidbody>(leaf.gameObject);
        rb.mass = 8f;
        rb.useGravity = false;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;

        foreach (var old in leaf.GetComponents<HingeJoint>()) UnityEngine.Object.DestroyImmediate(old);
        var hinge = leaf.gameObject.AddComponent<HingeJoint>();
        hinge.autoConfigureConnectedAnchor = true;       // connected to the world (no connected body)
        hinge.anchor = Vector3.zero;                     // pivot of the leaf = hinge position
        hinge.axis = Vector3.up;
        hinge.useLimits = true;
        hinge.limits = new JointLimits { min = -110f, max = 0f, bounciness = 0f, contactDistance = 5f };
        hinge.useSpring = false;
        hinge.useMotor = false;
        hinge.enableCollision = false;

        // Door handles on both faces at the free edge, so the door can be pushed and pulled.
        var leafRenderer = leaf.GetComponentInChildren<Renderer>();
        Bounds b = leafRenderer != null ? leafRenderer.bounds : new Bounds(leaf.position, new Vector3(1.3f, 3f, 0.14f));
        Vector3 pivot = leaf.position;
        float freeX = Mathf.Abs(b.min.x - pivot.x) > Mathf.Abs(b.max.x - pivot.x) ? b.min.x : b.max.x;
        float dirX = Mathf.Sign(freeX - pivot.x);
        var metal = MakeMat("Assets/Materials/DoorHandle.mat", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.8f);
        var handles = new System.Collections.Generic.List<Collider>();
        foreach (var side in new[] { -1f, 1f })
        {
            var old = leaf.Find("Door Handle " + (side < 0 ? "A" : "B"));
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var h = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            h.name = "Door Handle " + (side < 0 ? "A" : "B");
            h.transform.SetParent(leaf, true);
            h.transform.position = new Vector3(freeX - dirX * 0.12f, 1.05f, pivot.z + side * (b.extents.z + 0.05f));
            h.transform.rotation = Quaternion.identity;
            h.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
            h.GetComponent<Renderer>().sharedMaterial = metal;
            var cap = h.GetComponent<CapsuleCollider>();
            handles.Add(cap);
            // Small standoff post that connects the handle to the glass.
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = h.name + " Post";
            UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
            post.transform.SetParent(leaf, true);
            post.transform.position = new Vector3(h.transform.position.x, 1.05f, pivot.z + side * (b.extents.z + 0.02f));
            post.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            post.transform.localScale = new Vector3(0.03f, 0.04f, 0.03f);
            post.GetComponent<Renderer>().sharedMaterial = metal;
        }

        // Grab the leaf through the handles. Velocity tracking pulls the Rigidbody, the hinge constrains it.
        var grab = Get<XRGrabInteractable>(leaf.gameObject);
        grab.colliders.Clear();
        grab.colliders.AddRange(handles);
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.trackRotation = false;      // the hand position drives the door, hand rotation does not
        grab.throwOnDetach = false;
        grab.forceGravityOnDetach = false;
        grab.smoothPosition = true;
        grab.smoothPositionAmount = 12f;
        grab.velocityDamping = 0.6f;
        grab.velocityScale = 0.8f;
        grab.angularVelocityDamping = 1f;
        grab.useDynamicAttach = true;    // grab at the point the hand touches the handle
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = false;
        grab.interactionLayers = new InteractionLayerMask { value = InteractionLayerMask.GetMask("Default") };
        EditorUtility.SetDirty(grab);
        // Highlight the handles when hovered.
        var hl = Get<HoverHighlight>(leaf.gameObject);
        hl.strength = 0.6f;

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("VR Room phase 3 complete: hinge limits -110..0, free edge x=" + freeX);
    }

    // ---------------------------------------------------------------- Phase 4: wall clock
    [MenuItem("Tools/Create With VR/VR Room/4 Wall clock")]
    public static void Phase4()
    {
        const string path = "Assets/Prefabs/Wall Clock.prefab";
        BuildClockPrefab(path);
        var old = GameObject.Find("Wall Clock");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var clock = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        clock.transform.SetPositionAndRotation(new Vector3(-5.82f, 2.35f, 0.6f), Quaternion.Euler(0f, -90f, 0f));
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("VR Room phase 4 complete");
    }

    static void BuildClockPrefab(string path)
    {
        var root = new GameObject("Wall Clock");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
        var dark = MakeMat("Assets/Materials/Clock_Frame.mat", new Color(0.08f, 0.08f, 0.1f), 0.7f, 0.5f);
        var face = MakeMat("Assets/Materials/Clock_Face.mat", new Color(0.95f, 0.94f, 0.9f), 0f, 0.3f);
        var handMat = MakeMat("Assets/Materials/Clock_Hand.mat", new Color(0.06f, 0.06f, 0.07f), 0.3f, 0.4f);
        var red = MakeMat("Assets/Materials/Clock_Second.mat", new Color(0.85f, 0.1f, 0.1f), 0.2f, 0.5f);

        // Clock faces +Z away from the viewer. Viewer stands on the -Z side.
        var frame = Disc(root.transform, "Frame", 0.76f, 0.06f, 0f, dark);
        Disc(root.transform, "Face", 0.68f, 0.02f, -0.025f, face);

        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f;
            var rot = Quaternion.Euler(0f, 0f, -a);
            bool quarter = i % 3 == 0;
            var tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tick.name = "Tick " + i;
            UnityEngine.Object.DestroyImmediate(tick.GetComponent<Collider>());
            tick.transform.SetParent(root.transform, false);
            tick.transform.localRotation = rot;
            tick.transform.localPosition = rot * new Vector3(0f, 0.30f, -0.037f);
            tick.transform.localScale = quarter ? new Vector3(0.018f, 0.05f, 0.004f) : new Vector3(0.01f, 0.03f, 0.004f);
            tick.GetComponent<Renderer>().sharedMaterial = dark;

            var num = new GameObject("Number " + (i == 0 ? 12 : i));
            num.transform.SetParent(root.transform, false);
            num.transform.localPosition = rot * new Vector3(0f, 0.235f, -0.04f);
            var t = num.AddComponent<TextMeshPro>();
            t.text = (i == 0 ? 12 : i).ToString();
            t.font = font;
            t.fontSize = 0.7f;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.1f, 0.1f, 0.12f);
            t.rectTransform.sizeDelta = new Vector2(0.15f, 0.1f);
        }

        var hour = Hand(root.transform, "Hour Hand", 0.17f, 0.028f, -0.045f, handMat);
        var minute = Hand(root.transform, "Minute Hand", 0.25f, 0.020f, -0.052f, handMat);
        var second = Hand(root.transform, "Second Hand", 0.29f, 0.008f, -0.059f, red);
        var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "Center Cap";
        UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
        cap.transform.SetParent(root.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0f, -0.062f);
        cap.transform.localScale = Vector3.one * 0.03f;
        cap.GetComponent<Renderer>().sharedMaterial = red;

        // Digital read-out below the dial: dark panel, glowing digits, AM/PM and date.
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Digital Panel";
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.transform.SetParent(root.transform, false);
        panel.transform.localPosition = new Vector3(0f, -0.52f, 0f);
        panel.transform.localScale = new Vector3(0.76f, 0.22f, 0.04f);
        panel.GetComponent<Renderer>().sharedMaterial = dark;
        var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.name = "Digital Screen";
        UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
        screen.transform.SetParent(root.transform, false);
        screen.transform.localPosition = new Vector3(0f, -0.52f, -0.021f);
        screen.transform.localScale = new Vector3(0.70f, 0.17f, 0.004f);
        screen.GetComponent<Renderer>().sharedMaterial = MakeMat("Assets/Materials/Clock_Screen.mat", new Color(0.02f, 0.05f, 0.06f), 0f, 0.9f);

        var glow = new Color(0.25f, 1f, 0.9f);
        var timeText = Label(root.transform, "Time Text", new Vector3(-0.04f, -0.505f, -0.027f), 0.46f, 0.12f, 0.105f, glow, font, TextAlignmentOptions.Center);
        var period = Label(root.transform, "AM PM Text", new Vector3(0.29f, -0.49f, -0.027f), 0.10f, 0.06f, 0.04f, new Color(1f, 0.75f, 0.3f), font, TextAlignmentOptions.Center);
        var date = Label(root.transform, "Date Text", new Vector3(0f, -0.585f, -0.027f), 0.66f, 0.05f, 0.037f, new Color(0.7f, 0.85f, 0.9f), font, TextAlignmentOptions.Center);
        timeText.text = "12:00:00"; period.text = "AM"; date.text = "Monday";

        var clock = root.AddComponent<AnalogClock>();
        clock.hourHand = hour; clock.minuteHand = minute; clock.secondHand = second;
        clock.timeText = timeText; clock.periodText = period; clock.dateText = date;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    static GameObject Disc(Transform parent, string name, float diameter, float thickness, float z, Material m)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = name;
        UnityEngine.Object.DestroyImmediate(d.GetComponent<Collider>());
        d.transform.SetParent(parent, false);
        d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        d.transform.localPosition = new Vector3(0f, 0f, z);
        d.transform.localScale = new Vector3(diameter, thickness / 2f, diameter);
        d.GetComponent<Renderer>().sharedMaterial = m;
        return d;
    }

    static Transform Hand(Transform parent, string name, float length, float width, float z, Material m)
    {
        var pivot = new GameObject(name + " Pivot");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(0f, 0f, z);
        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = name;
        UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
        mesh.transform.SetParent(pivot.transform, false);
        mesh.transform.localPosition = new Vector3(0f, length * 0.4f, 0f);   // base sits slightly behind the pivot
        mesh.transform.localScale = new Vector3(width, length, 0.006f);
        mesh.GetComponent<Renderer>().sharedMaterial = m;
        return pivot.transform;
    }

    static TextMeshPro Label(Transform parent, string name, Vector3 pos, float w, float h, float size, Color color, TMP_FontAsset font, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.font = font;
        t.fontSize = size * 10f;
        t.alignment = align;
        t.color = color;
        t.enableWordWrapping = false;
        t.rectTransform.sizeDelta = new Vector2(w, h);
        return t;
    }
}
