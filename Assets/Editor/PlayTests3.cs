// Drives the scene through the Input System with the simulated XR controller devices (real action pipeline).
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class PlayTests3
{
    static StringBuilder log;
    static IEnumerator routine;
    static double wakeAt;
    static XRSimulatedController left, right;
    static Unity.XR.CoreUtils.XROrigin origin;
    static XRSimulatedControllerState lState, rState;

    static void L(string s) { log.AppendLine(s); Debug.Log("[PlayTest3] " + s); File.WriteAllText("PlayTestResults3.txt", log.ToString()); }
    static object Wait(float s) { wakeAt = EditorApplication.timeSinceStartup + s; return null; }
    static IEnumerable Sleep(float s) { double end = EditorApplication.timeSinceStartup + s; while (EditorApplication.timeSinceStartup < end) yield return Wait(0.05f); }

    [MenuItem("Tools/Create With VR/Play Tests/Run real-input test (VR Room)")]
    public static void Run()
    {
        if (!Application.isPlaying) { Debug.LogError("Enter Play mode first"); return; }
        log = new StringBuilder();
        routine = VRRoom();
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < wakeAt) return;
        try { if (!routine.MoveNext()) { EditorApplication.update -= Tick; L("DONE"); } }
        catch (Exception e) { L("EXCEPTION " + e); EditorApplication.update -= Tick; }
    }

    static T[] All<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include); }

    // ---- device helpers
    static void Push(bool isRight)
    {
        var dev = isRight ? right : left; var st = isRight ? rState : lState;
        st.isTracked = true; st.trackingState = 3;
        InputSystem.QueueStateEvent(dev, st);
    }
    static void Pose(bool isRight, Vector3 worldPos, Quaternion worldRot)
    {
        var t = origin.CameraFloorOffsetObject.transform;
        var st = isRight ? rState : lState;
        st.devicePosition = t.InverseTransformPoint(worldPos);
        st.deviceRotation = Quaternion.Inverse(t.rotation) * worldRot;
        if (isRight) rState = st; else lState = st;
        Push(isRight);
    }
    static void Buttons(bool isRight, float grip, float trigger, Vector2 stick)
    {
        var st = isRight ? rState : lState;
        st.grip = grip; st.trigger = trigger; st.primary2DAxis = stick;
        st.WithButton(ControllerButton.GripButton, grip > 0.5f);
        st.WithButton(ControllerButton.TriggerButton, trigger > 0.5f);
        st.WithButton(ControllerButton.Primary2DAxisTouch, stick.sqrMagnitude > 0.01f);
        if (isRight) rState = st; else lState = st;
        Push(isRight);
    }
    static void Secondary(bool isRight, bool down)
    {
        var st = isRight ? rState : lState;
        st.WithButton(ControllerButton.SecondaryButton, down);
        if (isRight) rState = st; else lState = st;
        Push(isRight);
    }
    static void Primary(bool isRight, bool down)
    {
        var st = isRight ? rState : lState;
        st.WithButton(ControllerButton.PrimaryButton, down);
        if (isRight) rState = st; else lState = st;
        Push(isRight);
    }
    static IEnumerable MoveTo(bool isRight, Vector3 from, Vector3 to, Quaternion rot, float seconds)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(seconds / 0.05f));
        for (int i = 0; i <= steps; i++) { Pose(isRight, Vector3.Lerp(from, to, i / (float)steps), rot); yield return Wait(0.05f); }
    }

    static IEnumerator VRRoom()
    {
        L("Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        foreach (var x0 in Sleep(1.5f)) yield return x0;
        // stop the keyboard/mouse simulator from overwriting the device state, keep the devices
        var simGo = All<XRInteractionSimulator>()[0].gameObject;
        L("XR Interaction Simulator object active before: " + simGo.activeSelf);
        simGo.SetActive(true);
        foreach (var x in Sleep(1.5f)) yield return x;
        foreach (var d in InputSystem.devices) L("  device: " + d.name + " | " + d.GetType().Name + " | usages: " + string.Join(",", d.usages.Select(u => u.ToString())));
        L("XRDeviceSimulator components: " + All<XRDeviceSimulator>().Length + ", XRInteractionSimulator: " + All<XRInteractionSimulator>().Length + ", lifecycle managers: " + All<SimulatedDeviceLifecycleManager>().Length);
        foreach (var s in All<XRDeviceSimulator>()) s.enabled = false;
        foreach (var s in All<XRInteractionSimulator>()) s.enabled = false;
        foreach (var d in InputSystem.devices) L("  device after disabling: " + d.name + " | " + d.GetType().Name);
        left = InputSystem.devices.OfType<XRSimulatedController>().FirstOrDefault(d => d.usages.Any(u => u == CommonUsages.LeftHand));
        right = InputSystem.devices.OfType<XRSimulatedController>().FirstOrDefault(d => d.usages.Any(u => u == CommonUsages.RightHand));
        L("Simulated controllers found: left=" + (left != null) + " right=" + (right != null));
        if (left == null || right == null) yield break;
        origin = All<Unity.XR.CoreUtils.XROrigin>()[0];
        var cam = Camera.main.transform;
        L("Rig origin at " + origin.transform.position.ToString("F2") + ", camera at " + cam.position.ToString("F2"));
        var interactors = All<NearFarInteractor>();
        foreach (var i in interactors) L("NearFarInteractor '" + i.name + "' under '" + i.transform.parent.name + "'");
        var rightNF = interactors.First(i => i.transform.parent.name.Contains("Right"));
        var leftNF = interactors.First(i => i.transform.parent.name.Contains("Left"));
        var menu = All<VRRoomMenu>()[0];

        Quaternion pointing = Quaternion.LookRotation(cam.forward);
        Pose(true, cam.position + cam.right * 0.25f + Vector3.down * 0.35f + cam.forward * 0.3f, pointing);
        Pose(false, cam.position - cam.right * 0.25f + Vector3.down * 0.35f + cam.forward * 0.3f, pointing);
        foreach (var x in Sleep(1f)) yield return x;


        // ---- DIAG
        L("DIAG EventSystem: " + (UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentInputModule + "" : "none"));
        foreach (var c in All<Canvas>().Where(c => c.renderMode == RenderMode.WorldSpace))
            L("DIAG world canvas " + c.name + " active=" + c.gameObject.activeInHierarchy + " cam=" + (c.worldCamera != null ? c.worldCamera.name : "null") + " trackedRaycaster=" + (c.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() != null) + " graphicRaycaster=" + (c.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null));
        foreach (var nf in new[] { rightNF, leftNF })
        {
            foreach (var pr in nf.GetType().GetProperties())
                if (pr.Name.ToLower().StartsWith("ui") || pr.Name.ToLower().StartsWith("enableui") || pr.Name.ToLower().StartsWith("enablefar"))
                { try { L("DIAG " + nf.name + "." + pr.Name + " = " + pr.GetValue(nf)); } catch { } }
        }
        foreach (var m in All<MonoBehaviour>().Where(m => m != null && (m.GetType().Name.Contains("ControllerInputActionManager") || m.GetType().Name.Contains("Locomotion"))))
            L("DIAG component " + m.GetType().Name + " on " + m.name + " enabled=" + m.enabled);
        foreach (var a in All<BaseTeleportationInteractable>())
        { var col = a.GetComponentInChildren<Collider>(); L("DIAG teleport " + a.GetType().Name + " " + a.name + " at " + a.transform.position.ToString("F1") + (col != null ? " bounds " + col.bounds.size.ToString("F2") + " topY " + col.bounds.max.y.ToString("F3") + " centerY " + col.bounds.center.y.ToString("F3") : "")); }
        foreach (var r in All<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>())
            L("DIAG ray " + r.name + " parent " + (r.transform.parent != null ? r.transform.parent.name : "") + " active=" + r.gameObject.activeInHierarchy + " lineType=" + r.lineType + " maxRaycastDistance=" + r.maxRaycastDistance + " velocity=" + r.velocity);

        // ---- 1 UI: point at START and pull the trigger
        var startBtn = menu.startPanel.GetComponentsInChildren<Button>(true).First(b => b.name == "Start Button");
        var corners = new Vector3[4]; ((RectTransform)startBtn.transform).GetWorldCorners(corners);
        var target = (corners[0] + corners[2]) * 0.5f;
        var handPos = cam.position + cam.right * 0.2f + Vector3.down * 0.3f;
        var aim = Quaternion.LookRotation((target - handPos).normalized);
        Pose(true, handPos, aim);
        foreach (var x in Sleep(0.6f)) yield return x;
        {
            var mod = UnityEngine.EventSystems.EventSystem.current.currentInputModule as UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule;
            var ui = (UnityEngine.XR.Interaction.Toolkit.UI.IUIInteractor)rightNF;
            UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceModel m;
            bool got = mod != null && mod.GetTrackedDeviceModel(ui, out m);
            L("DIAG ui model found=" + got + " interactor fwd=" + rightNF.transform.forward.ToString("F2") + " wanted=" + (aim * Vector3.forward).ToString("F2") + " interactorPos=" + rightNF.transform.position.ToString("F2") + " handPos=" + handPos.ToString("F2"));
            if (got) { mod.GetTrackedDeviceModel(ui, out m); L("DIAG model pos=" + m.position.ToString("F2") + " raycastPoints=" + m.raycastPoints.Count + " layerMask=" + m.raycastLayerMask.value + " currentRaycast=" + m.currentRaycast.gameObject); }
            var sc = GameObject.Find("Start UI"); L("DIAG Start UI layer=" + (sc != null ? LayerMask.LayerToName(sc.layer) : "-") + " pos=" + (sc != null ? sc.transform.position.ToString("F2") : "-") + " targetBtn=" + target.ToString("F2"));
            var caster = rightNF.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster>(true);
            L("DIAG far caster: " + (caster != null ? caster.name + " enabled=" + caster.enabled + " mask=" + caster.raycastMask.value + " trigger=" + caster.raycastTriggerInteraction : "none"));
        }
        rightNF.uiHoverEntered.AddListener(e => L("   UI hover entered: " + e.uiObject));
        Buttons(true, 0f, 1f, Vector2.zero); foreach (var x in Sleep(0.4f)) yield return x;
        {
            var rd = rightNF.uiPressInput;
            var refA = rd.inputActionReferencePerformed != null ? rd.inputActionReferencePerformed.action : null;
            var mod2 = UnityEngine.EventSystems.EventSystem.current.currentInputModule as UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule;
            UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceModel m2; mod2.GetTrackedDeviceModel((UnityEngine.XR.Interaction.Toolkit.UI.IUIInteractor)rightNF, out m2);
            if (refA != null) { foreach (var c in refA.controls) L("DIAG uiPress bound control: " + c.path + " value=" + c.ReadValueAsObject()); foreach (var b in refA.bindings) L("DIAG uiPress binding: " + b.path); }
            L("DIAG during trigger: uiPress performed=" + rd.ReadIsPerformed() + " value=" + rd.ReadValue().ToString("F2") + " mode=" + rd.inputSourceMode + " action=" + (refA != null ? refA.name + " enabled=" + refA.enabled + " bindings=" + refA.bindings.Count + " controls=" + refA.controls.Count : "null") + " model.select=" + m2.select + " selectDelta=" + m2.selectDelta + " hit=" + m2.currentRaycast.gameObject + " deviceTrigger=" + right.trigger.ReadValue().ToString("F2"));
        }
        Buttons(true, 0f, 0f, Vector2.zero); foreach (var x in Sleep(0.6f)) yield return x;
        if (menu.startPanel.activeSelf)
        {
            L("   attempt B: assigning Camera.main as worldCamera on world canvases");
            foreach (var c in All<Canvas>().Where(c => c.renderMode == RenderMode.WorldSpace)) c.worldCamera = Camera.main;
            Pose(true, handPos + Vector3.up * 0.01f, aim); foreach (var x in Sleep(0.6f)) yield return x;
            Buttons(true, 0f, 1f, Vector2.zero); foreach (var x in Sleep(0.4f)) yield return x;
            Buttons(true, 0f, 0f, Vector2.zero); foreach (var x in Sleep(0.6f)) yield return x;
        }
        L("1 UI ray + trigger on START -> start panel active=" + menu.startPanel.activeSelf + " (expect False), control panel active=" + menu.hudPanel.activeSelf + " (expect True)");
        if (menu.startPanel.activeSelf) { L("   (fallback) invoking START by code"); startBtn.onClick.Invoke(); foreach (var x in Sleep(0.5f)) yield return x; }

        // ---- 2 hand animation from real input
        var hand = All<HandPoseAnimator>().First(h => h.side == HandPoseAnimator.Side.Right);
        var bones = hand.GetComponentsInChildren<Transform>();
        var rest = bones.Select(t => t.localRotation).ToArray();
        Buttons(true, 1f, 1f, Vector2.zero); foreach (var x in Sleep(0.8f)) yield return x;
        L("   readback: device trigger=" + right.trigger.ReadValue().ToString("F2") + " grip=" + right.grip.ReadValue().ToString("F2") + " enabled=" + right.enabled + " added=" + right.added + " isTracked=" + right.isTracked.ReadValue() + " focused=" + Application.isFocused + " bg=" + InputSystem.settings.backgroundBehavior + " simEnabled=" + All<XRInteractionSimulator>()[0].enabled + " lifecycle=" + All<SimulatedDeviceLifecycleManager>()[0].enabled);
        int curled = 0; for (int i = 0; i < bones.Length; i++) if (Quaternion.Angle(rest[i], bones[i].localRotation) > 5f) curled++;
        L("2 Right hand with grip+trigger from controller input: " + curled + " of " + bones.Length + " bones rotated > 5 deg, hand.grip=" + hand.grip.ToString("F2") + " trigger=" + hand.trigger.ToString("F2"));
        Buttons(true, 0f, 0f, Vector2.zero); foreach (var x in Sleep(0.8f)) yield return x;

        // ---- 3 Y button toggles the control panel
        bool before = menu.hudPanel.activeSelf;
        Secondary(false, true); foreach (var x in Sleep(0.3f)) yield return x;
        Secondary(false, false); foreach (var x in Sleep(0.5f)) yield return x;
        L("3 Left Y button: control panel active " + before + " -> " + menu.hudPanel.activeSelf);
        Secondary(false, true); foreach (var x in Sleep(0.3f)) yield return x;
        Secondary(false, false); foreach (var x in Sleep(0.5f)) yield return x;
        L("   Y again: " + menu.hudPanel.activeSelf);

        // ---- 4 grabbing: wrong object into a socket, then right objects
        XRGrabInteractable G(string n) { return All<XRGrabInteractable>().First(g => g.name == n); }
        XRSocketInteractor S(string n) { return All<XRSocketInteractor>().First(g => g.name == n); }
        var rook = G("RookDark"); var flash = G("Flashlight"); var knight = G("KnightLight");
        var sFlash = S("Socket Flashlight"); var sRook = S("Socket Rook"); var sKnight = S("Socket Knight");

        // grab helper: moves the right hand to the object, grips, carries it to a destination and releases
        IEnumerable Carry(XRGrabInteractable obj, Vector3 dest, string label)
        {
            // teleport the player next to the table/object first so the object is within reach
            var start = obj.transform.position;
            var approach = start + Vector3.up * 0.0f;
            Pose(true, approach + Vector3.up * 0.05f, pointing);
            foreach (var x in Sleep(0.5f)) yield return x;
            Buttons(true, 1f, 0f, Vector2.zero);
            foreach (var x in Sleep(0.7f)) yield return x;
            L("   " + label + ": grabbed=" + obj.isSelected + " by '" + (obj.isSelected ? obj.interactorsSelecting[0].transform.name : "-") + "'");
            foreach (var x in MoveTo(true, approach, dest + Vector3.up * 0.05f, pointing, 1.2f)) yield return x;
            foreach (var x in Sleep(0.3f)) yield return x;
            Buttons(true, 0f, 0f, Vector2.zero);
            foreach (var x in Sleep(1.2f)) yield return x;
        }

        L("4 Grabbing with the right controller (near grab)");
        foreach (var x in Carry(rook, sFlash.transform.position, "Rook -> flashlight socket (wrong)")) yield return x;
        L("   Flashlight socket holds anything: " + sFlash.hasSelection + " (expect False, wrong object rejected)");
        foreach (var x in Carry(rook, sRook.transform.position, "Rook -> rook socket")) yield return x;
        L("   Rook socket filled: " + sRook.hasSelection + " (expect True)");
        foreach (var x in Carry(flash, sFlash.transform.position, "Flashlight -> flashlight socket")) yield return x;
        L("   Flashlight socket filled: " + sFlash.hasSelection + " (expect True)");
        foreach (var x in Carry(knight, sKnight.transform.position, "Knight -> knight socket")) yield return x;
        L("   Knight socket filled: " + sKnight.hasSelection + " (expect True)");
        L("   Objectives: " + All<PuzzleTracker>()[0].label.text.Replace("\n", " | "));

        // ---- 5 door
        var hinge = All<HingeJoint>().First(h => h.name == "glass_panel_1_door");
        var pivot = hinge.transform.position;
        var handleA = hinge.transform.Find("Door Handle A");
        var hpos = handleA.position;
        Func<float> doorAngle = () => Quaternion.Angle(Quaternion.identity, Quaternion.identity);
        var closedRot = hinge.transform.localRotation;
        Func<float> ang = () => Mathf.Abs(hinge.GetComponent<DoorHandleDrive>().CurrentAngle);
        L("5 Door: world euler now " + hinge.transform.eulerAngles.ToString("F1") + " local " + hinge.transform.localEulerAngles.ToString("F1") + " drive closed world euler " + hinge.GetComponent<DoorHandleDrive>().ClosedEuler.ToString("F1") + " drive angle " + hinge.GetComponent<DoorHandleDrive>().CurrentAngle.ToString("F1"));
        L("5 Door: closed angle " + ang().ToString("F1") + ", handle at " + hpos.ToString("F2") + ", pivot " + pivot.ToString("F2"));
        Pose(true, hpos, pointing); foreach (var x in Sleep(0.5f)) yield return x;
        Buttons(true, 1f, 0f, Vector2.zero); foreach (var x in Sleep(0.7f)) yield return x;
        L("   door grabbed by hand: " + hinge.GetComponent<XRGrabInteractable>().isSelected);
        float best = 0f; float sign = 1f;
        var startPos = hinge.transform.position;
        L("   joint: anchor=" + hinge.anchor + " connectedAnchor=" + hinge.connectedAnchor + " auto=" + hinge.autoConfigureConnectedAnchor + " axis=" + hinge.axis + " connectedBody=" + hinge.connectedBody + " limits=" + hinge.limits.min + ".." + hinge.limits.max + " rb.mass=" + hinge.GetComponent<Rigidbody>().mass + " breakForce=" + hinge.breakForce);
        int pass = 0;
        foreach (var sgn in new[] { -1f, 1f, -1f })
        {
            pass++;
            L("   --- pass " + pass);
            for (int i = 1; i <= 24; i++)
            {
                float a = sgn * 3f * i;
                Pose(true, pivot + Quaternion.Euler(0, a, 0) * (hpos - pivot), pointing);
                yield return Wait(0.05f);
            }
            foreach (var x in Sleep(0.5f)) yield return x;
            float now = ang();
            L("   drift from start pos: " + (hinge.transform.position - startPos).magnitude.ToString("F2") + " m, handle now at " + handleA.position.ToString("F2") + ", hand pose pivot dist " + (handleA.position - pivot).magnitude.ToString("F2"));
            L("   door euler=" + hinge.transform.localEulerAngles.ToString("F1") + " localPos=" + hinge.transform.localPosition.ToString("F3") + " driveAngle=" + hinge.GetComponent<DoorHandleDrive>().CurrentAngle.ToString("F1") + " motor=" + hinge.useMotor + " rbKinematic=" + hinge.GetComponent<Rigidbody>().isKinematic);
            L("   " + (sgn < 0 ? "pull the door open" : "ABUSE: shove the handle into the closed stop") + " (hand arc " + (sgn * 72f) + " deg): door angle " + now.ToString("F1") + ", motor on=" + hinge.useMotor);
            if (now > best) { best = now; sign = sgn; }
            // go back to start before trying the other direction
            for (int i = 24; i >= 0; i--) { Pose(true, pivot + Quaternion.Euler(0, sgn * 3f * i, 0) * (hpos - pivot), pointing); yield return Wait(0.05f); }
            foreach (var x in Sleep(0.8f)) yield return x;
        }
        Buttons(true, 0f, 0f, Vector2.zero); foreach (var x in Sleep(0.5f)) yield return x;
        L("   objectives while door open: " + All<PuzzleTracker>()[0].label.text.Replace("\n", " | "));
        L("   best door opening by dragging the handle with the hand: " + best.ToString("F1") + " deg (expect > 20)");

        // ---- 6 teleport with the thumbstick ray
        var floorTarget = new Vector3(3.5f, 0.1f, 3f);
        var rigBefore = origin.transform.position;
        var hp = cam.position + cam.right * 0.2f + Vector3.down * 0.3f;
        Pose(true, hp, Quaternion.LookRotation((floorTarget - hp).normalized));
        foreach (var x in Sleep(0.5f)) yield return x;
        Primary(true, true); foreach (var x in Sleep(1.0f)) yield return x;
        var tray = All<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>().First(r => r.name == "Teleport Interactor" && r.transform.parent.name.Contains("Right"));
        L("   teleport ray active while A held: " + tray.gameObject.activeInHierarchy);
        { RaycastHit th; bool hit = tray.TryGetCurrent3DRaycastHit(out th); L("   teleport ray hits: " + (hit ? th.collider.name + " at " + th.point.ToString("F1") : "nothing") + " hovering=" + tray.hasHover + " selectable=" + tray.hasSelection);
          var sa = tray.selectInput.inputActionReferencePerformed != null ? tray.selectInput.inputActionReferencePerformed.action : null;
          L("   teleport select action: " + (sa != null ? sa.name + " bindings: " + string.Join(", ", sa.bindings.Select(b => b.path + (string.IsNullOrEmpty(b.interactions) ? "" : " [" + b.interactions + "]"))) : "none mode=" + tray.selectInput.inputSourceMode));
          L("   teleport interactor pos=" + tray.transform.position.ToString("F2") + " fwd=" + tray.transform.forward.ToString("F2") + " ray origin fwd wanted=" + (Quaternion.LookRotation((floorTarget - hp).normalized) * Vector3.forward).ToString("F2")); }
        Buttons(true, 1f, 0f, Vector2.zero); foreach (var x in Sleep(0.6f)) yield return x;
        L("   after grip squeeze: rig at " + origin.transform.position.ToString("F2") + ", teleport interactor selecting=" + tray.hasSelection);
        Buttons(true, 0f, 0f, Vector2.zero); Primary(true, false); foreach (var x in Sleep(2.0f)) yield return x;
        L("6 Teleport: rig moved from " + rigBefore.ToString("F2") + " to " + origin.transform.position.ToString("F2") + " (target about " + floorTarget + ")");
    }
}
