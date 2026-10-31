using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class DoorDriveSetup
{
    [MenuItem("Tools/Create With VR/VR Room/Fix door drive (hinge motor)")]
    public static void Run()
    {
        var hinge = Object.FindObjectsByType<HingeJoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var h in hinge)
        {
            if (h.name != "glass_panel_1_door") continue;
            var grab = h.GetComponent<XRGrabInteractable>();
            grab.trackPosition = false; grab.trackRotation = false; grab.throwOnDetach = false;
            if (h.GetComponent<DoorHandleDrive>() == null) h.gameObject.AddComponent<DoorHandleDrive>();
            var m = h.motor; m.targetVelocity = 0; m.force = 150f; m.freeSpin = false; h.motor = m; h.useMotor = false;
            EditorUtility.SetDirty(h); EditorUtility.SetDirty(grab);
            Debug.Log("Door drive set up on " + h.name);
        }
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }
}
