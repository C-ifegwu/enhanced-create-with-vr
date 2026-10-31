// Drives the running scene in Play mode and writes a report to PlayTestResults.txt in the project root.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class PlayTests
{
    static StringBuilder log;
    static IEnumerator routine;
    static double wakeAt;

    static void L(string s) { log.AppendLine(s); Debug.Log("[PlayTest] " + s); File.WriteAllText("PlayTestResults.txt", log.ToString()); }
    static object Wait(float s) { wakeAt = EditorApplication.timeSinceStartup + s; return null; }

    [MenuItem("Tools/Create With VR/Play Tests/Run VR Room tests")]
    public static void Run()
    {
        if (!Application.isPlaying) { Debug.LogError("Enter Play mode in VR_Room first"); return; }
        log = new StringBuilder();
        routine = Tests();
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < wakeAt) return;
        try { if (!routine.MoveNext()) { EditorApplication.update -= Tick; L("DONE"); } }
        catch (Exception e) { L("EXCEPTION " + e); EditorApplication.update -= Tick; }
    }

    static T F<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include); }
    static Transform Find(string n) { return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.gameObject.scene.IsValid() && t.name == n); }

    static IEnumerable WaitScene(string name, float timeout)
    {
        double end = EditorApplication.timeSinceStartup + timeout;
        while (SceneManager.GetActiveScene().name != name && EditorApplication.timeSinceStartup < end) yield return Wait(0.5f);
        yield return Wait(2f);
    }

    static IEnumerator Tests()
    {
        L("Scene: " + SceneManager.GetActiveScene().name);
        yield return Wait(2f);

        // 1 Start UI
        var menu = F<VRRoomMenu>();
        var startBtn = menu.startPanel.GetComponentsInChildren<Button>(true).First(b => b.name == "Start Button");
        L("Start UI active before: " + menu.startPanel.activeSelf + ", panel: " + menu.hudPanel.activeSelf);
        startBtn.onClick.Invoke();
        yield return Wait(0.5f);
        L("After START -> start panel active=" + menu.startPanel.activeSelf + " (expect False), control panel active=" + menu.hudPanel.activeSelf + " (expect True)");
        var cam = Camera.main;
        var hud = menu.hudPanel.transform;
        L("Control panel distance from camera: " + Vector3.Distance(cam.transform.position, hud.position).ToString("F2") + " m");
        var raycasters = menu.hudPanel.GetComponents<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>().Length;
        L("Control panel TrackedDeviceGraphicRaycaster count=" + raycasters);

        // 2 Clock
        var clock = F<AnalogClock>();
        yield return Wait(0.3f);
        var now = DateTime.Now;
        float eh = ((now.Hour % 12) + now.Minute / 60f) * 30f, em = (now.Minute + now.Second / 60f) * 6f, es = (now.Second + now.Millisecond / 1000f) * 6f;
        float Z(Transform t) { return (360f - t.localEulerAngles.z) % 360f; }
        float D(float a, float b) { return Mathf.Abs(Mathf.DeltaAngle(a, b)); }
        L("Clock hour hand angle " + Z(clock.hourHand).ToString("F1") + " expect " + eh.ToString("F1") + " diff " + D(Z(clock.hourHand), eh).ToString("F1"));
        L("Clock minute hand angle " + Z(clock.minuteHand).ToString("F1") + " expect " + em.ToString("F1") + " diff " + D(Z(clock.minuteHand), em).ToString("F1"));
        L("Clock second hand angle " + Z(clock.secondHand).ToString("F1") + " expect ~" + es.ToString("F1") + " diff " + D(Z(clock.secondHand), es).ToString("F1"));
        L("Digital text: " + clock.timeText.text + " " + (clock.periodText ? clock.periodText.text : "") + " | " + (clock.dateText ? clock.dateText.text : ""));

        // 3 Door
        var hinge = Find("glass_panel_1_door").GetComponent<HingeJoint>();
        var rb = hinge.GetComponent<Rigidbody>();
        L("Door hinge limits " + hinge.limits.min + ".." + hinge.limits.max + ", useLimits=" + hinge.useLimits + ", rb kinematic=" + rb.isKinematic + ", animator=" + (hinge.GetComponent<Animator>() != null));
        L("Door rb: mass " + rb.mass + ", inertia " + rb.inertiaTensor + ", constraints " + rb.constraints + ", sleeping " + rb.IsSleeping() + ", hinge axis " + hinge.axis + ", anchor " + hinge.anchor + ", connectedBody " + hinge.connectedBody + ", autoConfigAnchor " + hinge.autoConfigureConnectedAnchor);
        foreach (var c in rb.GetComponentsInChildren<Collider>()) L("  door collider " + c.name + " " + c.GetType().Name + " trigger=" + c.isTrigger + " bounds " + c.bounds.center.ToString("F2") + " size " + c.bounds.size.ToString("F2") + (c is MeshCollider mc ? " convex=" + mc.convex : ""));
        float start = hinge.angle, minA = start, maxA = start;
        rb.gameObject.AddComponent<ContactLogger>();
        foreach (var dir in new[] { -1f, -1f, 1f })
        {
            rb.WakeUp();
            rb.angularVelocity = Vector3.up * dir * 3f;
            for (int i = 0; i < 40; i++) { minA = Mathf.Min(minA, hinge.angle); maxA = Mathf.Max(maxA, hinge.angle); yield return Wait(0.05f); }
            if (dir < 0 && minA < -5)
                foreach (var c in rb.GetComponentsInChildren<BoxCollider>())
                    foreach (var o in Physics.OverlapBox(c.transform.TransformPoint(c.center), Vector3.Scale(c.size * 0.5f, new Vector3(Mathf.Abs(c.transform.lossyScale.x), Mathf.Abs(c.transform.lossyScale.y), Mathf.Abs(c.transform.lossyScale.z))), c.transform.rotation))
                        if (!o.transform.IsChildOf(rb.transform)) L("  at angle " + hinge.angle.ToString("F1") + " box overlaps '" + o.name + "' (" + o.GetType().Name + ", trigger=" + o.isTrigger + ", rb=" + (o.attachedRigidbody != null) + ", ignored=" + Physics.GetIgnoreCollision(c, o) + ") under " + (o.transform.parent ? o.transform.parent.name : "root"));
            L("Door impulse dir " + dir + ": angle now " + hinge.angle.ToString("F1") + ", angular velocity " + rb.angularVelocity.ToString("F2") + ", range so far " + minA.ToString("F1") + ".." + maxA.ToString("F1"));
        }
        foreach (var c in ContactLogger.seen) L("  contact: " + c);
        // What is the door touching at rest?
        foreach (var c in rb.GetComponentsInChildren<Collider>())
            foreach (var o in Physics.OverlapBox(c.bounds.center, c.bounds.extents, Quaternion.identity))
                if (o.transform != c.transform && !o.transform.IsChildOf(rb.transform) && !o.isTrigger) L("  overlaps: " + o.name + " (" + o.GetType().Name + ") under " + (o.transform.parent ? o.transform.parent.name : "root"));
        var doorGrab = hinge.GetComponent<XRGrabInteractable>();
        L("Door XRGrabInteractable present=" + (doorGrab != null) + ", colliders=" + (doorGrab ? doorGrab.colliders.Count : 0));

        // 4 Sockets
        var mgr = F<XRInteractionManager>();
        string[] pairs = { "Flashlight", "RookDark", "KnightLight" };
        string[] sockets = { "Socket Flashlight", "Socket Rook", "Socket Knight" };
        var allGrabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
        var allSocks = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
        var grabs = pairs.Select(n => allGrabs.First(g => g.name == n)).ToArray();
        var socks = sockets.Select(n => allSocks.First(g => g.name == n)).ToArray();
        for (int s = 0; s < 3; s++)
            for (int g = 0; g < 3; g++)
                L("Socket '" + sockets[s] + "' layer-match with '" + pairs[g] + "': " + ((socks[s].interactionLayers.value & grabs[g].interactionLayers.value) != 0) + (s == g ? " (expect True)" : " (expect False)"));
        for (int s = 0; s < 3; s++)
        {
            grabs[s].transform.position = socks[s].transform.position;
            yield return Wait(1f);
            L("After moving " + pairs[s] + " to its socket: socket.hasSelection=" + socks[s].hasSelection + ", snapped pos error " + Vector3.Distance(grabs[s].transform.position, socks[s].attachTransform.position).ToString("F3"));
        }
        // wrong object: put the Rook on the Flashlight socket pad after releasing sockets
        var tracker = F<PuzzleTracker>();
        L("Objectives text: " + tracker.label.text.Replace("\n", " | "));

        // 5 Navigation
        var nav = F<SceneNavigator>();
        L("Navigating to Chibueze_Room");
        double t0 = EditorApplication.timeSinceStartup;
        nav.GoToScene("Chibueze_Room");
        foreach (var x in WaitScene("Chibueze_Room", 40f)) yield return x;
        L("Transition to course scene took about " + (EditorApplication.timeSinceStartup - t0 - 2).ToString("F1") + " s");
        L("Active scene: " + SceneManager.GetActiveScene().name);
        L("XR Origin count: " + UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include).Length + ", Main Camera: " + (Camera.main != null) + ", Interaction managers: " + UnityEngine.Object.FindObjectsByType<XRInteractionManager>(FindObjectsInactive.Include).Length);
        var nav2 = F<SceneNavigator>();
        L("Course scene navigator present: " + (nav2 != null));
        var vrBtn = Find("VR Room Button");
        L("Course 'VR Room Button' present: " + (vrBtn != null));
        t0 = EditorApplication.timeSinceStartup;
        if (vrBtn != null) vrBtn.GetComponent<Button>().onClick.Invoke();
        foreach (var x in WaitScene("VR_Room", 60f)) yield return x;
        L("Transition back to VR Room took about " + (EditorApplication.timeSinceStartup - t0 - 2).ToString("F1") + " s");
        L("Active scene after returning: " + SceneManager.GetActiveScene().name);
        L("XR Origin count: " + UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include).Length + ", Main Camera: " + (Camera.main != null));
        var menu2 = F<VRRoomMenu>();
        L("VRRoomMenu back in VR Room: " + (menu2 != null));
        if (menu2 != null) { menu2.GoToCourseScene(); foreach (var x in WaitScene("Chibueze_Room", 40f)) yield return x; L("Control panel's Course button works: scene " + SceneManager.GetActiveScene().name); }
    }
}
