// Lesson 2.3 setup helper: Raycasts layer, Near-Far interactors on both hands, the teleport ray
// that only shows while a button is held, haptics and hand hiding.
// Run it from the menu: Tools > Create With VR > Lesson 2.3 Setup.
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class Lesson23Setup
{
    [MenuItem("Tools/Create With VR/Lesson 2.3 Setup")]
    public static void Run()
    {
        int rayLayer = EnsureInteractionLayer("Raycasts");
        var rayMask = new InteractionLayerMask { value = 1 << rayLayer };

        // Teleport areas and anchors only react to the Raycasts layer.
        foreach (var area in UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include))
        { area.interactionLayers = rayMask; EditorUtility.SetDirty(area); }
        var matPath = "Assets/_Course Library/_Prefabs/Rugs/Mat_Round_Blue.prefab";
        var matRoot = PrefabUtility.LoadPrefabContents(matPath);
        foreach (var a in matRoot.GetComponentsInChildren<TeleportationAnchor>(true)) a.interactionLayers = rayMask;
        PrefabUtility.SaveAsPrefabAsset(matRoot, matPath);
        PrefabUtility.UnloadPrefabContents(matRoot);

        SetupHand("Left", rayMask);
        SetupHand("Right", rayMask);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 2.3 setup complete");
    }

    static void SetupHand(string side, InteractionLayerMask rayMask)
    {
        var ctrl = Find(side + " Hand Controller");
        var teleport = ctrl.Find(side + " Ray Interactor");
        if (teleport == null) teleport = ctrl.Find("Teleport Interactor");
        teleport.name = "Teleport Interactor";
        var ray = teleport.GetComponent<XRRayInteractor>();
        ray.interactionLayers = rayMask;
        // Remove the hide-hand select events so the hand stays visible while teleporting.
        for (int i = ray.selectEntered.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(ray.selectEntered, i);
        for (int i = ray.selectExited.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(ray.selectExited, i);
        EditorUtility.SetDirty(ray);

        // Near-Far interactor (created through the XR menu, then parented to this controller).
        string nfName = side + " Near-Far Interactor";
        var nf = ctrl.Find(nfName);
        if (nf == null)
        {
            Selection.activeGameObject = ctrl.gameObject;
            EditorApplication.ExecuteMenuItem("GameObject/XR/Near-Far Interactor");
            var created = Selection.activeGameObject;
            if (created == null || created.GetComponent<NearFarInteractor>() == null)
                throw new Exception("Near-Far Interactor was not created");
            created.transform.SetParent(ctrl, false);
            created.transform.localPosition = Vector3.zero;
            created.transform.localRotation = Quaternion.identity;
            created.name = nfName;
            nf = created.transform;
        }
        var nearFar = nf.GetComponent<NearFarInteractor>();
        var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Settings/" + side + "_NearFarInteractor.preset");
        preset.ApplyTo(nearFar);
        // The preset resets the component references, so wire the sibling components back up.
        nearFar.interactionAttachController = nf.GetComponent<InteractionAttachController>();
        nearFar.nearInteractionCaster = nf.GetComponent<SphereInteractionCaster>();
        nearFar.farInteractionCaster = nf.GetComponent<CurveInteractionCaster>();
        nearFar.interactionLayers = InteractionLayerMask.GetMask("Default", "Hats");

        // Hide the hand while grabbing, show it on release.
        Transform hand = null;
        foreach (Transform c in ctrl) if (c.name.StartsWith("VR_Hand")) hand = c;
        if (hand != null)
        {
            for (int i = nearFar.selectEntered.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(nearFar.selectEntered, i);
            for (int i = nearFar.selectExited.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(nearFar.selectExited, i);
            UnityEventTools.AddBoolPersistentListener(nearFar.selectEntered, hand.gameObject.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(nearFar.selectExited, hand.gameObject.SetActive, true);
        }

        // Haptics on grab.
        if (!nf.TryGetComponent<SimpleHapticFeedback>(out var hap)) hap = nf.gameObject.AddComponent<SimpleHapticFeedback>();
        var so = new SerializedObject(hap);
        so.FindProperty("m_InteractorSourceObject").objectReferenceValue = nearFar;
        so.FindProperty("m_HapticImpulsePlayer").objectReferenceValue = ctrl.GetComponent<HapticImpulsePlayer>();
        so.FindProperty("m_PlaySelectEntered").boolValue = true;
        so.FindProperty("m_SelectEnteredData.m_Amplitude").floatValue = 0.5f;
        so.FindProperty("m_SelectEnteredData.m_Duration").floatValue = 0.1f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(nearFar);

        // Teleport ray only while primary button or thumbstick touch is held.
        if (!ctrl.TryGetComponent<ToggleInteractorComponents>(out var toggle)) toggle = ctrl.gameObject.AddComponent<ToggleInteractorComponents>();
        toggle.nearFarInteractor = nearFar;
        toggle.teleportInteractor = teleport.gameObject;
        if (!ctrl.TryGetComponent<OnButtonPress>(out var press)) press = ctrl.gameObject.AddComponent<OnButtonPress>();
        var action = new InputAction("Show Teleport Ray", InputActionType.Button);
        action.AddBinding($"<XRController>{{{side}Hand}}/primaryButton");
        action.AddBinding($"<XRController>{{{side}Hand}}/thumbstickTouched");
        press.action = action;
        for (int i = press.OnPress.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(press.OnPress, i);
        for (int i = press.OnRelease.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(press.OnRelease, i);
        UnityEventTools.AddVoidPersistentListener(press.OnPress, toggle.EnableTeleportRay);
        UnityEventTools.AddVoidPersistentListener(press.OnRelease, toggle.DisableTeleportRay);
        teleport.gameObject.SetActive(false);
        EditorUtility.SetDirty(toggle);
        EditorUtility.SetDirty(press);
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

    static Transform Find(string name)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        throw new Exception("Not found: " + name);
    }
}
