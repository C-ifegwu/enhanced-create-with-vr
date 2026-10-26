// Lesson 1.4 setup helper: hats, hook sockets, head socket and the "Hats" interaction layer.
// Run it from the menu: Tools > Create With VR > Lesson 1.4 Setup.
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class Lesson14Setup
{
    const string Lib = "Assets/_Course Library/_Prefabs/";

    [MenuItem("Tools/Create With VR/Lesson 1.4 Setup")]
    public static void Run()
    {
        int hatsLayer = EnsureInteractionLayer("Hats");
        var mask = new InteractionLayerMask { value = 1 << hatsLayer };

        var dynamicRoot = GameObject.Find("DYNAMIC");
        var hook = Find("Hook_Modern");
        var mirror = Find("Mirror_Square_Brown");
        var table = Find("Table_Coffee_Modern");

        // 1. Hats on the table, near the mirror side, grabbable and on the Hats layer only.
        Vector3 top = new Vector3(2f, 0.5f, 1f);
        if (table != null)
        {
            var r = table.GetComponentInChildren<Renderer>();
            if (r != null) top = new Vector3(r.bounds.center.x, r.bounds.max.y + 0.05f, r.bounds.center.z);
        }
        var hats = new[]
        {
            SpawnHat("Objects/Hats/Hat_Cap.prefab", top + new Vector3(0, 0, 0.2f), dynamicRoot, mask),
            SpawnHat("Objects/Hats/Hat_Top.prefab", top + new Vector3(0.2f, 0, 0.2f), dynamicRoot, mask)
        };

        // 2. Three hook sockets spread along the hook.
        if (hook != null)
        {
            var rend = hook.GetComponentInChildren<Renderer>();
            Bounds b = rend != null ? rend.bounds : new Bounds(hook.position, Vector3.one * 0.3f);
            bool alongX = b.size.x >= b.size.z;
            for (int i = 0; i < 3; i++)
            {
                float t = (i + 1) / 4f;
                Vector3 p = b.center;
                if (alongX) p.x = Mathf.Lerp(b.min.x, b.max.x, t); else p.z = Mathf.Lerp(b.min.z, b.max.z, t);
                p.y = b.min.y + b.size.y * 0.25f;
                var s = MakeSocket("Socket " + (i + 1), hook, p, 0.1f, mask, true);
                if (i < hats.Length) s.startingSelectedInteractable = hats[i];
            }
        }
        else Debug.LogWarning("Hook_Modern not found");

        // 3. Head socket under the Main Camera.
        var cam = Find("Main Camera");
        if (cam != null)
        {
            var head = MakeSocket("Socket", cam, cam.position + cam.up * 0.1f, 0.2f, mask, false);
            head.transform.localPosition = new Vector3(0, 0.1f, 0);
        }

        // 4. Make sure other grabbables only use the Default interaction layer.
        foreach (var g in UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include))
        {
            if (Array.IndexOf(hats, g) >= 0) continue;
            g.interactionLayers = InteractionLayerMask.GetMask("Default");
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 1.4 setup complete");
    }

    static XRGrabInteractable SpawnHat(string path, Vector3 pos, GameObject parent, InteractionLayerMask mask)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Lib + path));
        go.transform.position = pos;
        if (parent != null) go.transform.SetParent(parent.transform, true);
        if (!go.TryGetComponent<Rigidbody>(out var rb)) rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (!go.TryGetComponent<XRGrabInteractable>(out var grab)) grab = go.AddComponent<XRGrabInteractable>();
        grab.smoothPosition = true;
        grab.smoothRotation = true;
        grab.interactionLayers = mask;
        return grab;
    }

    static XRSocketInteractor MakeSocket(string name, Transform parent, Vector3 worldPos, float radius, InteractionLayerMask mask, bool faceDown)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        var col = go.AddComponent<SphereCollider>();
        col.radius = radius;
        col.isTrigger = true;
        var socket = go.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = mask;
        var attach = new GameObject("Attach");
        attach.transform.SetParent(go.transform, false);
        if (faceDown) attach.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        socket.attachTransform = attach.transform;
        return socket;
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
        {
            if (string.IsNullOrEmpty(names.GetArrayElementAtIndex(i).stringValue))
            {
                names.GetArrayElementAtIndex(i).stringValue = layerName;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                return i;
            }
        }
        throw new Exception("No free interaction layer slot");
    }

    static Transform Find(string name)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        return null;
    }
}
