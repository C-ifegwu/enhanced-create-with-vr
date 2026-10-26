// Lesson 2.2 setup helper: grabbable remote (sound, red indicator light) that toggles a video on the TV.
// Run it from the menu: Tools > Create With VR > Lesson 2.2 Setup.
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class Lesson22Setup
{
    [MenuItem("Tools/Create With VR/Lesson 2.2 Setup")]
    public static void Run()
    {
        // TV screen: 3D audio, video player (_BaseMap) and the PlayVideo helper, off at start.
        var tv = Find("TV_FlatWithStand");
        Transform screen = null;
        foreach (var t in tv.GetComponentsInChildren<Transform>(true))
            if (t.name == "Screen") { screen = t; break; }
        if (screen == null) throw new System.Exception("TV Screen child not found");
        var audio = Get<AudioSource>(screen.gameObject);
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        var vp = Get<VideoPlayer>(screen.gameObject);
        vp.playOnAwake = false;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, audio);
        vp.targetMaterialProperty = "_BaseMap";
        var play = Get<PlayVideo>(screen.gameObject);
        play.playAtStart = false;
        play.videoClips.Clear();
        play.videoClips.Add(AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Course Library/Video/Video_Unity-for-everyone.mp4"));
        EditorUtility.SetDirty(play);

        // Remote control, placed on the coffee table.
        var table = Find("Table_Coffee_Modern");
        Vector3 pos = new Vector3(2f, 0.5f, 1f);
        var r = table.GetComponentInChildren<Renderer>();
        if (r != null) pos = new Vector3(r.bounds.center.x - 0.2f, r.bounds.max.y + 0.05f, r.bounds.center.z - 0.15f);
        var remote = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Course Library/_Prefabs/Objects/Electronics/Remote.prefab"));
        remote.transform.position = pos;
        var dyn = GameObject.Find("DYNAMIC");
        if (dyn != null) remote.transform.SetParent(dyn.transform, true);

        var rb = Get<Rigidbody>(remote);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var grab = Get<XRGrabInteractable>(remote);
        grab.smoothPosition = true;
        grab.smoothRotation = true;
        var attach = new GameObject("Attach");
        attach.transform.SetParent(remote.transform, false);
        grab.attachTransform = attach.transform;

        var src = Get<AudioSource>(remote);
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        var quick = Get<PlayQuickSound>(remote);
        quick.sound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Course Library/Audio/FX/SFX_UI_Click_1.ogg");
        quick.volume = 5f;

        Transform light = null;
        foreach (var t in remote.GetComponentsInChildren<Transform>(true))
            if (t.name == "Indicator_Light") { light = t; break; }
        ChangeMaterial change = null;
        if (light != null)
        {
            change = Get<ChangeMaterial>(light.gameObject);
            change.otherMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Course Library/Materials/Solid Colors/Red.mat");
        }

        // Activated: click sound, light turns red, TV toggles. Deactivated: light reverts.
        UnityEventTools.AddVoidPersistentListener(grab.activated, quick.Play);
        if (change != null)
        {
            UnityEventTools.AddVoidPersistentListener(grab.activated, change.SetOtherMaterial);
            UnityEventTools.AddVoidPersistentListener(grab.deactivated, change.SetOriginalMaterial);
        }
        UnityEventTools.AddVoidPersistentListener(grab.activated, play.TogglePlayPause);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 2.2 setup complete");
    }

    static T Get<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();
    }

    static Transform Find(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        throw new System.Exception("Not found: " + name);
    }
}
