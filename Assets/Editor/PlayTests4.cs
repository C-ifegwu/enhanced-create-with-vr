// Real-input test for the course scene and the navigation VR Room <-> course scene, driven through simulated XR controllers.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class PlayTests4
{
    static StringBuilder log;
    static IEnumerator routine;
    static double wakeAt;
    static XRSimulatedController left, right;
    static Unity.XR.CoreUtils.XROrigin origin;
    static XRSimulatedControllerState lState, rState;
    static Transform cam;
    static Quaternion pointing;

    static void L(string s) { log.AppendLine(s); Debug.Log("[PlayTest4] " + s); File.WriteAllText("PlayTestResults4.txt", log.ToString()); }
    static object Wait(float s) { wakeAt = EditorApplication.timeSinceStartup + s; return null; }
    static IEnumerable Sleep(float s) { double end = EditorApplication.timeSinceStartup + s; while (EditorApplication.timeSinceStartup < end) yield return Wait(0.05f); }
    static T[] All<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include); }

    [MenuItem("Tools/Create With VR/Play Tests/Run real-input test (course scene + navigation)")]
    public static void Run()
    {
        if (!Application.isPlaying) { Debug.LogError("Enter Play mode first"); return; }
        log = new StringBuilder();
        routine = Main();
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < wakeAt) return;
        try { if (!routine.MoveNext()) { EditorApplication.update -= Tick; L("DONE"); } }
        catch (Exception e) { L("EXCEPTION " + e); EditorApplication.update -= Tick; }
    }

    static void Push(bool isRight) { var dev = isRight ? right : left; var st = isRight ? rState : lState; st.isTracked = true; st.trackingState = 3; InputSystem.QueueStateEvent(dev, st); }
    static void Pose(bool isRight, Vector3 worldPos, Quaternion worldRot)
    {
        var t = origin.CameraFloorOffsetObject.transform; var st = isRight ? rState : lState;
        st.devicePosition = t.InverseTransformPoint(worldPos); st.deviceRotation = Quaternion.Inverse(t.rotation) * worldRot;
        if (isRight) rState = st; else lState = st; Push(isRight);
    }
    static void Buttons(bool isRight, float grip, float trigger)
    {
        var st = isRight ? rState : lState; st.grip = grip; st.trigger = trigger;
        st.WithButton(ControllerButton.GripButton, grip > 0.5f); st.WithButton(ControllerButton.TriggerButton, trigger > 0.5f);
        if (isRight) rState = st; else lState = st; Push(isRight);
    }
    static IEnumerable MoveTo(Vector3 from, Vector3 to, float seconds)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(seconds / 0.05f));
        for (int i = 0; i <= steps; i++) { Pose(true, Vector3.Lerp(from, to, i / (float)steps), pointing); yield return Wait(0.05f); }
    }

    // Makes the simulated controllers available for the scene that is currently loaded.
    static IEnumerable Setup(Action<bool> done)
    {
        foreach (var x in Sleep(1.5f)) yield return x;
        var sims = All<XRInteractionSimulator>();
        L("  scene '" + SceneManager.GetActiveScene().name + "': XRInteractionSimulator objects=" + sims.Length);
        if (sims.Length > 0) { sims[0].gameObject.SetActive(true); foreach (var x in Sleep(1.5f)) yield return x; foreach (var s in sims) s.enabled = false; }
        left = InputSystem.devices.OfType<XRSimulatedController>().FirstOrDefault(d => d.usages.Any(u => u == CommonUsages.LeftHand));
        right = InputSystem.devices.OfType<XRSimulatedController>().FirstOrDefault(d => d.usages.Any(u => u == CommonUsages.RightHand));
        L("  simulated controllers: left=" + (left != null) + " right=" + (right != null));
        if (left == null || right == null) { done(false); yield break; }
        origin = All<Unity.XR.CoreUtils.XROrigin>()[0];
        cam = Camera.main.transform; pointing = Quaternion.LookRotation(cam.forward);
        lState = new XRSimulatedControllerState(); rState = new XRSimulatedControllerState();
        Pose(true, cam.position + cam.right * 0.25f + Vector3.down * 0.35f + cam.forward * 0.3f, pointing);
        Pose(false, cam.position - cam.right * 0.25f + Vector3.down * 0.35f + cam.forward * 0.3f, pointing);
        foreach (var x in Sleep(1f)) yield return x;
        done(true);
    }

    static IEnumerable WaitScene(string name, float timeout) { double end = EditorApplication.timeSinceStartup + timeout; while (SceneManager.GetActiveScene().name != name && EditorApplication.timeSinceStartup < end) yield return Wait(0.1f); foreach (var x in Sleep(1.5f)) yield return x; }

    static string Path(Transform t) { var s = t.name; while (t.parent != null && s.Length < 90) { t = t.parent; s = t.name + "/" + s; } return s; }

    // Aim the right ray at a UI element and pull the trigger.
    static IEnumerable PressUI(Selectable sel, string label, Action<bool> result)
    {
        int clicks = 0; Button btn = sel as Button; Toggle tog = sel as Toggle; bool toggleBefore = tog != null && tog.isOn;
        UnityEngine.Events.UnityAction cb = () => clicks++;
        if (btn != null) btn.onClick.AddListener(cb);
        var corners = new Vector3[4]; ((RectTransform)sel.transform).GetWorldCorners(corners);
        var target = (corners[0] + corners[2]) * 0.5f;
        var viewer = (cam.position - target); viewer.y = 0; viewer = viewer.sqrMagnitude < 0.01f ? -sel.transform.forward : viewer.normalized;
        var handPos = target + viewer * 1.1f + Vector3.down * 0.1f;
        Pose(true, handPos, Quaternion.LookRotation((target - handPos).normalized));
        foreach (var x in Sleep(0.6f)) yield return x;
        Buttons(true, 0f, 1f); foreach (var x in Sleep(0.4f)) yield return x;
        Buttons(true, 0f, 0f); foreach (var x in Sleep(0.6f)) yield return x;
        bool ok = btn != null ? clicks > 0 : tog != null && tog.isOn != toggleBefore;
        if (btn != null) btn.onClick.RemoveListener(cb);
        L("    UI '" + label + "' (" + Path(sel.transform) + "): " + (ok ? "PRESSED" : "NOT PRESSED"));
        result(ok);
    }

    static IEnumerator Main()
    {
        L("Real-input test, course scene + navigation. Start scene: " + SceneManager.GetActiveScene().name);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        bool ok = false;
        foreach (var x in Setup(r => ok = r)) yield return x;
        if (!ok) yield break;

        // ---------------- discovery
        var rig = origin.gameObject;
        var canvases = All<Canvas>().Where(c => c.renderMode == RenderMode.WorldSpace && c.gameObject.activeInHierarchy).ToArray();
        L("World canvases: " + string.Join(", ", canvases.Select(c => c.name)));
        var selectables = canvases.SelectMany(c => c.GetComponentsInChildren<Selectable>(false)).Where(s => s.interactable && s.gameObject.activeInHierarchy && (s is Button || s is Toggle)).ToList();
        L("UI controls: " + string.Join(" | ", selectables.Select(s => s.name + (s is Toggle ? "(toggle)" : ""))));
        var grabs = All<XRGrabInteractable>().Where(g => !g.transform.IsChildOf(rig.transform) && g.gameObject.activeInHierarchy).ToList();
        L("Grab interactables: " + string.Join(" | ", grabs.Select(g => g.name + " layers=" + g.interactionLayers.value)));
        var sockets = All<XRSocketInteractor>().Where(s => s.gameObject.activeInHierarchy).ToList();
        L("Sockets: " + string.Join(" | ", sockets.Select(s => s.name + " layers=" + s.interactionLayers.value)));

        // ---------------- navigation first: the VR Room button lives on the welcome panel that the OK button closes
        {
            var okBtn = selectables.FirstOrDefault(q => q.name.StartsWith("OK"));
            if (okBtn != null) { bool okr = false; L("3b Press OK first so the welcome panel closes"); foreach (var x in PressUI(okBtn, okBtn.name, v => okr = v)) yield return x; foreach (var x in Sleep(1f)) yield return x; L("    welcome panel active after OK: " + (okBtn.gameObject.activeInHierarchy)); }
            var vrButton0 = selectables.FirstOrDefault(q => q.name == "VR Room Button (Always)") ?? selectables.FirstOrDefault(q => q.name.Contains("VR Room"));
            if (vrButton0 != null) L("    nav button chosen: " + vrButton0.name + ", visible after OK: " + vrButton0.gameObject.activeInHierarchy);
        if (vrButton0 != null)
        {
            L("4 Navigation: press the VR Room button with ray + trigger");
            bool r = false;
            foreach (var x in PressUI(vrButton0, vrButton0.name, v => r = v)) yield return x;
            foreach (var x in WaitScene("VR_Room", 25f)) yield return x;
            L("    active scene after press: " + SceneManager.GetActiveScene().name + " (expect VR_Room)");
            if (SceneManager.GetActiveScene().name == "VR_Room")
            {
                foreach (var x in Setup(v => ok = v)) yield return x;
                if (ok)
                {
                    L("    XR origins in VR Room: " + All<Unity.XR.CoreUtils.XROrigin>().Length + ", cameras tagged MainCamera: " + GameObject.FindGameObjectsWithTag("MainCamera").Length);
                    var menu = All<VRRoomMenu>()[0];
                    var back = menu.startPanel.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name != "Start Button" && b.gameObject.activeInHierarchy)
                               ?? menu.hudPanel.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name.Contains("Course"));
                    if (back != null)
                    {
                        L("    going back with '" + back.name + "'");
                        bool r2 = false;
                        foreach (var x in PressUI(back, back.name, v => r2 = v)) yield return x;
                        foreach (var x in WaitScene("Chibueze_Room", 25f)) yield return x;
                        L("    active scene after return: " + SceneManager.GetActiveScene().name + " (expect Chibueze_Room)");
                    }
                }
            }
        }

            // back in the course scene (new copy of the scene): set up again
            if (SceneManager.GetActiveScene().name == "Chibueze_Room")
            {
                foreach (var x in Setup(v => ok = v)) yield return x;
                if (!ok) yield break;
                rig = origin.gameObject;
                canvases = All<Canvas>().Where(c => c.renderMode == RenderMode.WorldSpace && c.gameObject.activeInHierarchy).ToArray();
                selectables = canvases.SelectMany(c => c.GetComponentsInChildren<Selectable>(false)).Where(sl => sl.interactable && sl.gameObject.activeInHierarchy && (sl is Button || sl is Toggle)).ToList();
                grabs = All<XRGrabInteractable>().Where(g => !g.transform.IsChildOf(rig.transform) && g.gameObject.activeInHierarchy).ToList();
                sockets = All<XRSocketInteractor>().Where(sk => sk.gameObject.activeInHierarchy).ToList();
                L("  back in the course scene: " + selectables.Count + " UI controls, " + grabs.Count + " grabbables, " + sockets.Count + " sockets");
            }
        }

        // ---------------- UI with the ray and the trigger
        L("1 UI pressed with ray + trigger");
        int uiOk = 0, uiTotal = 0;
        foreach (var s in selectables)
        {
            if (s.name.Contains("VR Room")) continue;
            if (s.name.Contains("Close") || s.name.Contains("Quit")) continue;
            bool r = false; uiTotal++;
            foreach (var x in PressUI(s, s.name, v => r = v)) yield return x;
            if (r) uiOk++;
        }
        L("   UI controls pressed: " + uiOk + " of " + uiTotal);

        // ---------------- grabbing (near grab)
        L("2 Grab with the right hand: every interactable");
        foreach (var g in grabs)
        {
            if (g == null) continue;
            int activated = 0; UnityEngine.Events.UnityAction<ActivateEventArgs> act = a => activated++;
            g.activated.AddListener(act);
            var p = g.transform.position;
            Pose(true, p + Vector3.up * 0.02f, pointing); foreach (var x in Sleep(0.4f)) yield return x;
            Buttons(true, 1f, 0f); foreach (var x in Sleep(0.6f)) yield return x;
            bool sel = g.isSelected;
            Buttons(true, 1f, 1f); foreach (var x in Sleep(0.4f)) yield return x;
            Buttons(true, 1f, 0f); foreach (var x in Sleep(0.3f)) yield return x;
            Buttons(true, 0f, 0f); foreach (var x in Sleep(0.6f)) yield return x;
            g.activated.RemoveListener(act);
            L("    " + g.name + ": grabbed=" + sel + ", trigger activates it=" + (activated > 0) + ", released=" + !g.isSelected);
        }

        // ---------------- sockets: carry each object that matches the layers, and one that does not
        L("3 Sockets: carry matching objects in, check wrong ones are rejected");
        foreach (var s in sockets)
        {
            L("    before: socket '" + s.name + "' holds " + (s.hasSelection ? s.firstInteractableSelected.transform.name : "nothing"));
            var match = grabs.FirstOrDefault(g => g != null && (g.interactionLayers.value & s.interactionLayers.value) != 0 && !(s.hasSelection && s.firstInteractableSelected.transform == g.transform));
            var wrong = grabs.FirstOrDefault(g => g != null && (g.interactionLayers.value & s.interactionLayers.value) == 0);
            var heldBefore = s.hasSelection ? s.firstInteractableSelected.transform.name : "nothing";
            if (wrong != null)
            {
                Pose(true, wrong.transform.position + Vector3.up * 0.02f, pointing); foreach (var x in Sleep(0.4f)) yield return x;
                Buttons(true, 1f, 0f); foreach (var x in Sleep(0.6f)) yield return x;
                foreach (var x in MoveTo(wrong.transform.position, s.transform.position + Vector3.up * 0.02f, 1.0f)) yield return x;
                Buttons(true, 0f, 0f); foreach (var x in Sleep(1.0f)) yield return x;
                L("    socket '" + s.name + "' with non-matching '" + wrong.name + "': holds " + (s.hasSelection ? "SOMETHING (bad if it is the wrong object)" : "nothing (rejected)"));
            }
            if (match != null)
            {
                Pose(true, match.transform.position + Vector3.up * 0.02f, pointing); foreach (var x in Sleep(0.4f)) yield return x;
                Buttons(true, 1f, 0f); foreach (var x in Sleep(0.6f)) yield return x;
                foreach (var x in MoveTo(match.transform.position, s.transform.position + Vector3.up * 0.02f, 1.0f)) yield return x;
                Buttons(true, 0f, 0f); foreach (var x in Sleep(1.2f)) yield return x;
                bool ok2 = s.hasSelection && s.firstInteractableSelected == (IXRSelectInteractable)match;
                L("    socket '" + s.name + "' with matching '" + match.name + "': " + (ok2 ? "FILLED" : "NOT FILLED (holds " + (s.hasSelection ? s.firstInteractableSelected.transform.name : "nothing") + ")"));
            }
        }

    }
}
