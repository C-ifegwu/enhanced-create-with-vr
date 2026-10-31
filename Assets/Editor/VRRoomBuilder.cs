// Editor automation that builds the VR Room scene step by step. Each phase has its own menu item under
// Tools > Create With VR > VR Room. Phase 1 builds the shared XR rig prefab and realistic hands,
// phase 2 makes the props grabbable and builds the three filtered sockets on the puzzle table.
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.SceneManagement;

public static partial class VRRoomBuilder
{
    public const string CoursePath = "Assets/Scenes/Chibueze_Room.unity";
    public const string OriginalVRPath = "Assets/VR Room/Scenes/VR Room.unity";
    public const string VRPath = "Assets/Scenes/VR_Room.unity";
    public const string RigPath = "Assets/Prefabs/XR Rig.prefab";
    const string Font = "Assets/_Course Library/Fonts/Inter-Regular-slnt=0 SDF.asset";

    // ---------------------------------------------------------------- Phase 1
    [MenuItem("Tools/Create With VR/VR Room/1 Rig, hands and floors")]
    public static void Phase1()
    {
        // Course scene: realistic hands, then save the XR rig as a prefab.
        EditorSceneManager.OpenScene(CoursePath, OpenSceneMode.Single);
        ReplaceHands();
        MakeRigPrefab();
        EditorSceneManager.SaveOpenScenes();

        // VR Room scene: work on a copy inside Assets/Scenes so the imported original stays untouched.
        var vr = EditorSceneManager.OpenScene(OriginalVRPath, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(vr, VRPath, false);

        var oldCam = GameObject.Find("Camera");
        if (oldCam != null) UnityEngine.Object.DestroyImmediate(oldCam);

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigPath));
        rig.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1.5f), Quaternion.identity);

        int rayLayer = EnsureInteractionLayer("Raycasts");
        var rayMask = new InteractionLayerMask { value = 1 << rayLayer };
        var areas = new GameObject("Teleport Areas");
        MakeArea(areas.transform, "Storage Room Floor", new Vector3(0f, 0.1f, 0f), new Vector3(11.2f, 0.05f, 11.2f), rayMask);
        MakeArea(areas.transform, "Main Room Floor", new Vector3(6f, 0.1f, 16f), new Vector3(15.2f, 0.05f, 19.2f), rayMask);
        // Entry anchors so UI buttons can send the player to each room.
        MakeAnchor(areas.transform, "Anchor Storage Room", new Vector3(0f, 0.1f, -1.5f), 0f, rayMask);
        MakeAnchor(areas.transform, "Anchor Main Room", new Vector3(4f, 0.1f, 9f), 0f, rayMask);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("VR Room phase 1 complete");
    }

    static void ReplaceHands()
    {
        foreach (var side in new[] { "Left", "Right" })
        {
            var ctrl = FindT(side + " Hand Controller");
            foreach (Transform c in ctrl.Cast<Transform>().ToArray())
                if (c.name.StartsWith("VR_Hand") || c.name.StartsWith("Realistic Hand")) UnityEngine.Object.DestroyImmediate(c.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Realistic Hand " + side + ".prefab");
            var hand = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            hand.transform.SetParent(ctrl, false);
            hand.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            hand.transform.localRotation = Quaternion.identity;
            // The realistic hand stays visible while grabbing, so remove the old "hide hand" events.
            var nf = ctrl.Find(side + " Near-Far Interactor").GetComponent<NearFarInteractor>();
            for (int i = nf.selectEntered.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(nf.selectEntered, i);
            for (int i = nf.selectExited.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(nf.selectExited, i);
            EditorUtility.SetDirty(nf);
        }
    }

    static void MakeRigPrefab()
    {
        foreach (var stale in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == "XR Rig").ToArray())
            UnityEngine.Object.DestroyImmediate(stale);
        var root = new GameObject("XR Rig");
        foreach (var n in new[] { "Input Action Manager", "XR", "EventSystem" })
        {
            var src = FindRoot(n);
            var copy = UnityEngine.Object.Instantiate(src);
            copy.name = n;  // the original "Input Action Manager" name has a trailing space; the copy is trimmed
            copy.transform.SetParent(root.transform, false);
            copy.transform.localPosition = Vector3.zero;
        }
        // Remove the parts that point at objects of the course scene.
        foreach (var tp in root.GetComponentsInChildren<TeleportPlayer>(true)) UnityEngine.Object.DestroyImmediate(tp);
        var origin = root.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true);
        foreach (var btn in origin.GetComponents<OnButtonPress>()) UnityEngine.Object.DestroyImmediate(btn);
        foreach (var s in origin.GetComponentsInChildren<XRSocketInteractor>(true).Where(x => x.name == "Socket").ToArray())
            UnityEngine.Object.DestroyImmediate(s.gameObject);
        PrefabUtility.SaveAsPrefabAsset(root, RigPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void MakeArea(Transform parent, string name, Vector3 center, Vector3 size, InteractionLayerMask mask)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        var area = go.AddComponent<TeleportationArea>();
        area.interactionLayers = mask;
    }

    static void MakeAnchor(Transform parent, string name, Vector3 pos, float yaw, InteractionLayerMask mask)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        // The anchor is only a destination for the menu buttons, so it is not selectable by the teleport ray.
        var anchor = go.AddComponent<TeleportationAnchor>();
        anchor.interactionLayers = mask;
        anchor.teleportAnchorTransform = go.transform;
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.5f, 0.02f, 0.5f);
    }

    // ---------------------------------------------------------------- Phase 2
    [MenuItem("Tools/Create With VR/VR Room/2 Grabbables and sockets")]
    public static void Phase2()
    {
        int flash = EnsureInteractionLayer("Flashlight");
        int rook = EnsureInteractionLayer("Rook");
        int knight = EnsureInteractionLayer("Knight");
        uint def = (uint)InteractionLayerMask.GetMask("Default");

        // All dynamic props are grabbable. The big console stays fixed.
        var interactables = FindT("Interactables");
        foreach (var rb in interactables.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb.name == "Console") { rb.isKinematic = true; continue; }
            foreach (var mc in rb.GetComponents<MeshCollider>()) mc.convex = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var grab = Get<XRGrabInteractable>(rb.gameObject);
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.smoothPosition = true;
            grab.smoothRotation = true;
            grab.throwOnDetach = true;
            uint mask = def;
            if (rb.name == "Flashlight") mask |= 1u << flash;
            if (rb.name == "RookDark") mask |= 1u << rook;
            if (rb.name == "KnightLight") mask |= 1u << knight;
            grab.interactionLayers = new InteractionLayerMask { value = (int)mask };
            Get<HoverHighlight>(rb.gameObject);
            EditorUtility.SetDirty(grab);
        }

        // Puzzle table with three sockets. Each socket accepts exactly one interaction layer.
        var old = GameObject.Find("Puzzle Table");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Puzzle Table";
        table.transform.position = new Vector3(-1.5f, 0.54f, 2.0f);
        table.transform.localScale = new Vector3(2.6f, 0.9f, 0.8f);
        table.GetComponent<Renderer>().sharedMaterial = MakeMat("Assets/Materials/PuzzleTable.mat", new Color(0.16f, 0.19f, 0.24f), 0.55f, 0.6f);
        var inter = FindT("Interactables");
        var puzzle = new GameObject("Puzzle Sockets");
        puzzle.transform.SetParent(table.transform.parent, true);
        float topY = 0.99f;
        MakeSocket(puzzle.transform, "Socket Flashlight", new Vector3(-2.4f, topY, 2.0f), 1u << flash, new Color(0.3f, 0.7f, 1f), "FLASHLIGHT");
        MakeSocket(puzzle.transform, "Socket Rook", new Vector3(-1.5f, topY, 2.0f), 1u << rook, new Color(1f, 0.4f, 0.35f), "ROOK");
        MakeSocket(puzzle.transform, "Socket Knight", new Vector3(-0.6f, topY, 2.0f), 1u << knight, new Color(0.4f, 1f, 0.5f), "KNIGHT");

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("VR Room phase 2 complete");
    }

    static void MakeSocket(Transform parent, string name, Vector3 pos, uint layerBit, Color color, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos + Vector3.up * 0.06f;
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.12f;
        var socket = go.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = new InteractionLayerMask { value = (int)layerBit };
        socket.showInteractableHoverMeshes = true;
        var attach = new GameObject("Attach");
        attach.transform.SetParent(go.transform, false);
        socket.attachTransform = attach.transform;

        // Visible pad on the table showing where the object goes.
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = name + " Pad";
        UnityEngine.Object.DestroyImmediate(pad.GetComponent<Collider>());
        pad.transform.SetParent(go.transform, true);
        pad.transform.position = pos + Vector3.up * 0.005f;
        pad.transform.localScale = new Vector3(0.3f, 0.005f, 0.3f);
        pad.GetComponent<Renderer>().sharedMaterial = MakeMat("Assets/Materials/Pad_" + label + ".mat", color, 0.1f, 0.2f);

        var textGo = new GameObject(name + " Label");
        textGo.transform.SetParent(go.transform, true);
        textGo.transform.position = pos + new Vector3(0f, 0.01f, -0.28f);
        textGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
        tmp.fontSize = 1.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 0.3f);
    }

    // ---------------------------------------------------------------- helpers
    static Material MakeMat(string path, Color color, float metallic, float smooth)
    {
        System.IO.Directory.CreateDirectory("Assets/Materials");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    static T Get<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();
    }

    static Transform FindT(string name)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name) return t;
        throw new Exception("Not found: " + name);
    }

    static GameObject FindRoot(string name)
    {
        foreach (var g in SceneManager.GetActiveScene().GetRootGameObjects())
            if (g.name.Trim() == name) return g;
        throw new Exception("Root not found: " + name);
    }

    static int EnsureInteractionLayer(string layerName)
    {
        int existing = InteractionLayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.InteractionLayerSettings, Unity.XR.Interaction.Toolkit");
        var prop = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        var settings = (ScriptableObject)prop.GetValue(null);
        var so = new SerializedObject(settings);
        var names = so.FindProperty("m_LayerNames");
        for (int i = 1; i < names.arraySize; i++)
            if (string.IsNullOrEmpty(names.GetArrayElementAtIndex(i).stringValue))
            {
                names.GetArrayElementAtIndex(i).stringValue = layerName;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                return i;
            }
        throw new Exception("No free interaction layer slot");
    }
}
