// Generic play mode checks for any scene (course scene and challenge scenes). Report: PlayTestResults2.txt in the project root.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.Events;

public static class PlayTests2
{
    static StringBuilder log;
    static IEnumerator routine;
    static double wakeAt;
    static List<string> errors = new List<string>();

    static void L(string s) { log.AppendLine(s); Debug.Log("[PlayTest2] " + s); File.WriteAllText("PlayTestResults2.txt", log.ToString()); }
    static object Wait(float s) { wakeAt = EditorApplication.timeSinceStartup + s; return null; }

    [MenuItem("Tools/Create With VR/Play Tests/Open course scene")]
    public static void OpenCourse() { EditorSceneManager.OpenScene("Assets/Scenes/Chibueze_Room.unity", OpenSceneMode.Single); }
    [MenuItem("Tools/Create With VR/Play Tests/Open painting challenge")]
    public static void OpenPainting() { EditorSceneManager.OpenScene("Assets/Challenges/02_3DPainting/Scenes/3DPainting_Prototype_Broken.unity", OpenSceneMode.Single); }
    [MenuItem("Tools/Create With VR/Play Tests/Open training challenge")]
    public static void OpenTraining() { EditorSceneManager.OpenScene("Assets/Challenges/03_Training/Scenes/Training_Prototype_Broken.unity", OpenSceneMode.Single); }

    [MenuItem("Tools/Create With VR/Play Tests/Run generic scene check")]
    public static void Run()
    {
        if (!Application.isPlaying) { Debug.LogError("Enter Play mode first"); return; }
        log = new StringBuilder(); errors.Clear();
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        routine = Tests();
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    static void OnLog(string msg, string stack, LogType t)
    {
        if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert)
            errors.Add(t + ": " + msg.Split('\n')[0]);
    }

    static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < wakeAt) return;
        try { if (!routine.MoveNext()) { EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; L("DONE"); } }
        catch (Exception e) { L("EXCEPTION " + e); EditorApplication.update -= Tick; }
    }

    static T[] All<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include); }

    static IEnumerator Tests()
    {
        var sc = SceneManager.GetActiveScene();
        L("Scene: " + sc.name);
        yield return Wait(6f);
        L("XR Origin count: " + All<Unity.XR.CoreUtils.XROrigin>().Length + ", Main Camera: " + (Camera.main != null) + ", XRInteractionManager: " + All<XRInteractionManager>().Length + ", EventSystem: " + All<EventSystem>().Length);
        L("Grab interactables: " + All<XRGrabInteractable>().Length + ", simple interactables: " + All<XRSimpleInteractable>().Length + ", sockets: " + All<XRSocketInteractor>().Length + ", near-far interactors: " + All<NearFarInteractor>().Length);

        int missing = 0;
        foreach (var go in All<Transform>().Select(t => t.gameObject)) missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        L("Missing script references: " + missing);

        var canvases = All<Canvas>().Where(c => c.isRootCanvas).ToArray();
        foreach (var c in canvases)
        {
            bool tracked = c.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() != null;
            L("Canvas '" + c.name + "' mode " + c.renderMode + (c.renderMode == RenderMode.WorldSpace ? ", TrackedDeviceGraphicRaycaster=" + tracked : "") + ", active=" + c.gameObject.activeInHierarchy);
        }

        int btn = 0, badListeners = 0;
        foreach (var b in All<Button>())
        {
            btn++;
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentTarget(i) == null || string.IsNullOrEmpty(b.onClick.GetPersistentMethodName(i)))
                { badListeners++; L("  Button '" + b.name + "' has a missing listener at index " + i); }
        }
        L("Buttons: " + btn + ", broken listeners: " + badListeners);
        int tog = 0, badTog = 0;
        foreach (var t in All<Toggle>())
        {
            tog++;
            for (int i = 0; i < t.onValueChanged.GetPersistentEventCount(); i++)
                if (t.onValueChanged.GetPersistentTarget(i) == null) { badTog++; L("  Toggle '" + t.name + "' has a missing listener at index " + i); }
        }
        L("Toggles: " + tog + ", broken listeners: " + badTog);

        // Interactable events with missing targets
        int badEvents = 0;
        foreach (var g in All<XRBaseInteractable>())
        {
            foreach (var ev in new UnityEvent<ActivateEventArgs>[] { g.activated, }) { }
        }
        L("Interactable events check skipped (typed events); see per-scene checks below");

        // Hands (course scene)
        var hands = All<HandPoseAnimator>();
        L("HandPoseAnimator count: " + hands.Length);
        foreach (var h in hands)
        {
            h.useControllerInput = false;
            var bones = h.GetComponentsInChildren<Transform>();
            var before = bones.Select(t => t.localRotation).ToArray();
            h.SetPose(0f, 0f); yield return Wait(0.3f);
            var open = bones.Select(t => t.localRotation).ToArray();
            h.SetPose(1f, 1f); yield return Wait(0.5f);
            var fist = bones.Select(t => t.localRotation).ToArray();
            int changed = 0; for (int i = 0; i < bones.Length; i++) if (Quaternion.Angle(open[i], fist[i]) > 5f) changed++;
            L("Hand '" + h.name + "': " + changed + " of " + bones.Length + " bones change more than 5 degrees between open and fist");
            h.SetPose(0f, 0f);
        }

        // Painting challenge specifics
        var pedestal = GameObject.Find("Canvas_Pedestal");
        if (pedestal != null) L("Pedestal canvas mode: " + pedestal.GetComponent<Canvas>().renderMode + " (expect WorldSpace)");
        foreach (var g in All<XRGrabInteractable>())
        {
            if (g.activated.GetPersistentEventCount() > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < g.activated.GetPersistentEventCount(); i++) sb.Append(g.activated.GetPersistentMethodName(i) + " ");
                var sb2 = new StringBuilder();
                for (int i = 0; i < g.deactivated.GetPersistentEventCount(); i++) sb2.Append(g.deactivated.GetPersistentMethodName(i) + " ");
                L("Grab '" + g.name + "' Activated: " + sb + "| Deactivated: " + sb2);
            }
        }

        yield return Wait(3f);
        L("Errors/exceptions logged during the run: " + errors.Count);
        foreach (var e in errors.Distinct().Take(15)) L("  " + e);
    }
}
