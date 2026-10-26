// Lesson 1.3 setup helper: builds the hand models, grabbable ball and bat, ray-interactor
// tweaks and Hierarchy organisation from the Unity Learn "Grabbable GameObjects" lesson.
// Run it from the menu: Tools > Create With VR > Lesson 1.3 Setup.
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class Lesson13Setup
{
    const string Lib = "Assets/_Course Library/_Prefabs/";

    [MenuItem("Tools/Create With VR/Lesson 1.3 Setup")]
    public static void Run()
    {
        var left = FindDeep("Left Hand Controller");
        var right = FindDeep("Right Hand Controller");
        if (left == null || right == null) { Debug.LogError("Hand controllers not found"); return; }

        GameObject leftHand = SetupHand(left, "VR/Hands/VR_Hand_Blue_Left.prefab");
        GameObject rightHand = SetupHand(right, "VR/Hands/VR_Hand_Blue_Right.prefab");
        SetupRay(left, leftHand);
        SetupRay(right, rightHand);

        var dynamic = Organizer("DYNAMIC");
        var table = FindDeep("Table_Coffee_Modern");
        Vector3 top = new Vector3(2.0f, 0.5f, 1.0f);
        if (table != null)
        {
            var r = table.GetComponentInChildren<Renderer>();
            if (r != null) top = new Vector3(r.bounds.center.x, r.bounds.max.y + 0.05f, r.bounds.center.z);
        }

        var ball = Spawn("Objects/Sports/Tennis_Ball_Green.prefab", top + new Vector3(-0.15f, 0, 0), dynamic);
        MakeGrabbable(ball);
        var ballCol = ball.GetComponent<Collider>();
        var mats = AssetDatabase.FindAssets("t:PhysicsMaterial");
        if (ballCol != null && mats.Length > 0)
            ballCol.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(AssetDatabase.GUIDToAssetPath(mats[0]));

        // Measuring stick: a tennis ball is 6.7 cm across.
        var stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stick.name = "Measuring Stick";
        stick.transform.localScale = new Vector3(0.01f, 0.067f, 0.01f);
        stick.transform.position = top + new Vector3(-0.25f, 0.034f, 0);
        stick.transform.SetParent(dynamic.transform, true);

        var bat = Spawn("Objects/Sports/Cricket_Bat_Blue.prefab", top + new Vector3(0.2f, 0.05f, 0), dynamic);
        var grab = MakeGrabbable(bat);
        var attach = new GameObject("Attach");
        attach.transform.SetParent(bat.transform, false);
        var br = bat.GetComponentInChildren<Renderer>();
        if (br != null)
        {
            var b = br.bounds;
            Vector3 axis = b.size.x > b.size.y && b.size.x > b.size.z ? Vector3.right : (b.size.y > b.size.z ? Vector3.up : Vector3.forward);
            attach.transform.position = b.center - axis * (Vector3.Dot(b.size, axis) * 0.35f);
        }
        grab.attachTransform = attach.transform;

        // Hierarchy organisation
        var xr = Organizer("XR");
        var lighting = Organizer("LIGHTING");
        var stat = Organizer("STATIC");
        foreach (var n in new[] { "XR Origin (VR)", "Input Action Manager", "XR Interaction Manager", "XR Interaction Simulator" })
            Reparent(n, xr);
        Reparent("Directional Light", lighting);
        foreach (var n in new[] { "Room_Modern", "Foreground_Hills", "Background_Hills", "Mirror_Square_Brown", "Hook_Modern",
                 "Couch_Modern_Brown", "Fireplace_Modern", "Mat_Round_Blue (1)", "Mat_Round_Blue", "Table_Coffee_Modern",
                 "TV_FlatWithStand", "Rug_Rect_Gray" })
            Reparent(n, stat);

        EditorSceneManagerSave();
        Debug.Log("Lesson 1.3 setup complete");
    }

    static void EditorSceneManagerSave()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    static GameObject SetupHand(Transform controller, string prefab)
    {
        foreach (Transform c in controller)
            if (c.name.Contains("Sphere")) c.gameObject.SetActive(false);
        var hand = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Lib + prefab), controller);
        hand.transform.localPosition = Vector3.zero;
        return hand;
    }

    static void SetupRay(Transform controller, GameObject hand)
    {
        var ray = controller.GetComponentInChildren<XRRayInteractor>(true);
        if (ray == null) { Debug.LogWarning("No ray interactor under " + controller.name); return; }
        var so = new SerializedObject(ray);
        SetBool(so, "m_UseForceGrab", true);
        SetBool(so, "m_ManipulateAttachTransform", false);
        so.ApplyModifiedProperties();
        UnityEventTools.AddBoolPersistentListener(ray.selectEntered, hand.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(ray.selectExited, hand.SetActive, true);
        EditorUtility.SetDirty(ray);
    }

    static void SetBool(SerializedObject so, string name, bool v)
    {
        var p = so.FindProperty(name);
        if (p != null) p.boolValue = v; else Debug.LogWarning("Property not found: " + name);
    }

    static XRGrabInteractable MakeGrabbable(GameObject go)
    {
        var grab = go.GetComponent<XRGrabInteractable>() ?? go.AddComponent<XRGrabInteractable>();
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.smoothPosition = true;
        grab.smoothRotation = true;
        return grab;
    }

    static GameObject Spawn(string path, Vector3 pos, GameObject parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Lib + path));
        go.transform.position = pos;
        go.transform.SetParent(parent.transform, true);
        return go;
    }

    static GameObject Organizer(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) { go = new GameObject(name); go.transform.position = Vector3.zero; }
        return go;
    }

    static void Reparent(string name, GameObject parent)
    {
        var t = FindDeep(name);
        if (t != null && t.parent != parent.transform) t.SetParent(parent.transform, true);
    }

    static Transform FindDeep(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name) return t;
        return null;
    }
}
