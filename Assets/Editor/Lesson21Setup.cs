// Lesson 2.1 setup helper: controller haptics, hover/select audio, 3D fire sound and a room reverb zone.
// Run it from the menu: Tools > Create With VR > Lesson 2.1 Setup.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class Lesson21Setup
{
    const string Fx = "Assets/_Course Library/Audio/FX/";

    [MenuItem("Tools/Create With VR/Lesson 2.1 Setup")]
    public static void Run()
    {
        SetupHand("Left Hand Controller", "XRI Left/Haptic Device");
        SetupHand("Right Hand Controller", "XRI Right/Haptic Device");
        SetupFire();
        SetupReverb();
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 2.1 setup complete");
    }

    static void SetupHand(string controllerName, string hapticAction)
    {
        var ctrl = Find(controllerName);
        var ray = ctrl.GetComponentInChildren<XRRayInteractor>(true);

        // Haptic Impulse Player on the controller, wired to the controller's haptic output action.
        var player = ctrl.GetComponent<HapticImpulsePlayer>();
        if (player == null) player = ctrl.gameObject.AddComponent<HapticImpulsePlayer>();
        var so = new SerializedObject(player);
        so.FindProperty("m_HapticOutput.m_InputSourceMode").enumValueIndex = 2;
        so.FindProperty("m_HapticOutput.m_InputActionReference").objectReferenceValue = FindAction(hapticAction);
        so.ApplyModifiedProperties();

        // Simple Haptic Feedback on the ray interactor: gentle on hover, stronger on grab.
        var hap = ray.GetComponent<SimpleHapticFeedback>();
        if (hap == null) hap = ray.gameObject.AddComponent<SimpleHapticFeedback>();
        var h = new SerializedObject(hap);
        h.FindProperty("m_InteractorSourceObject").objectReferenceValue = ray;
        h.FindProperty("m_HapticImpulsePlayer").objectReferenceValue = player;
        h.FindProperty("m_PlayHoverEntered").boolValue = true;
        h.FindProperty("m_HoverEnteredData.m_Amplitude").floatValue = 0.25f;
        h.FindProperty("m_HoverEnteredData.m_Duration").floatValue = 0.1f;
        h.FindProperty("m_PlaySelectEntered").boolValue = true;
        h.FindProperty("m_SelectEnteredData.m_Amplitude").floatValue = 0.6f;
        h.FindProperty("m_SelectEnteredData.m_Duration").floatValue = 0.25f;
        h.ApplyModifiedProperties();

        // Simple Audio Feedback on the controller: a soft click on hover, a pop on grab.
        var src = ctrl.GetComponent<AudioSource>();
        if (src == null) src = ctrl.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        var aud = ctrl.GetComponent<SimpleAudioFeedback>();
        if (aud == null) aud = ctrl.gameObject.AddComponent<SimpleAudioFeedback>();
        var a = new SerializedObject(aud);
        a.FindProperty("m_InteractorSourceObject").objectReferenceValue = ray;
        a.FindProperty("m_AudioSource").objectReferenceValue = src;
        a.FindProperty("m_PlayHoverEntered").boolValue = true;
        a.FindProperty("m_HoverEnteredClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Fx + "SFX_UI_Quick_1.ogg");
        a.FindProperty("m_PlaySelectEntered").boolValue = true;
        a.FindProperty("m_SelectEnteredClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Fx + "SFX_Cork_Pop.ogg");
        a.ApplyModifiedProperties();
    }

    static void SetupFire()
    {
        var fire = Find("Fireplace_Modern");
        Transform particle = null;
        foreach (var t in fire.GetComponentsInChildren<Transform>(true))
            if (t.name == "Particle") { particle = t; break; }
        if (particle == null) { Debug.LogWarning("Fire Particle child not found, using fireplace root"); particle = fire; }
        var src = particle.GetComponent<AudioSource>();
        if (src == null) src = particle.gameObject.AddComponent<AudioSource>();
        var so = new SerializedObject(src);
        var clip = so.FindProperty("m_audioClip");
        if (clip != null) clip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Fx + "SFX_Loop_Fire.ogg");
        so.ApplyModifiedProperties();
        src.playOnAwake = true;
        src.loop = true;
        src.spatialBlend = 1f;
        src.minDistance = 0.5f;
        src.maxDistance = 8f;
    }

    static void SetupReverb()
    {
        var room = Find("Room_Modern");
        Transform existing = room.Find("Audio Reverb Zone");
        GameObject go = existing != null ? existing.gameObject : new GameObject("Audio Reverb Zone");
        go.transform.SetParent(room, false);
        var zone = go.GetComponent<AudioReverbZone>();
        if (zone == null) zone = go.AddComponent<AudioReverbZone>();
        float width = 6f;
        var r = room.GetComponentInChildren<Renderer>();
        if (r != null) width = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
        zone.minDistance = width;
        zone.maxDistance = width * 1.5f;
        zone.reverbPreset = AudioReverbPreset.Room;
    }

    static InputActionReference FindAction(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                if (o is InputActionReference r && r.name == name) return r;
        Debug.LogWarning("Input action not found: " + name);
        return null;
    }

    static Transform Find(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        throw new System.Exception("Not found: " + name);
    }
}
