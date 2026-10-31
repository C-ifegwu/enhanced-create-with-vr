// Lesson 3.2 setup helper: FPS overlay, static batching, 4x MSAA, post-processing volume, painting, texture settings.
// Run it from the menu: Tools > Create With VR > Lesson 3.2 Setup.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Lesson32Setup
{
    [MenuItem("Tools/Create With VR/Lesson 3.2 Setup")]
    public static void Run()
    {
        var cam = Find("Main Camera").GetComponent<Camera>();

        // 1. FPS overlay UI on the main camera.
        if (Find("FPS Overlay UI", false) == null)
        {
            var fps = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Course Library/_Prefabs/Testing/FPS Overlay UI.prefab"));
            var iface = GameObject.Find("INTERFACE");
            if (iface != null) fps.transform.SetParent(iface.transform, false);
            var c = fps.GetComponentInChildren<Canvas>(true);
            if (c != null) c.worldCamera = cam;
        }

        // 2. Static batching: everything under STATIC is marked static.
        var stat = GameObject.Find("STATIC");
        if (stat != null)
            foreach (var t in stat.GetComponentsInChildren<Transform>(true))
                if (t != stat.transform) GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)int.MaxValue);

        // Fire particles: unlit shader so they ignore dynamic lights.
        var fire = Find("Fireplace_Modern");
        var unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        foreach (var pr in fire.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (pr.sharedMaterial != null && unlit != null && pr.sharedMaterial.shader != unlit)
            { pr.sharedMaterial.shader = unlit; EditorUtility.SetDirty(pr.sharedMaterial); }

        // 3. Post-processing with a gentle colour adjustment (no blur or lens distortion).
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        const string profilePath = "Assets/Settings/PostProcessProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.2f);
            ca.saturation.Override(10f);
            AssetDatabase.AddObjectToAsset(ca, profile);
            EditorUtility.SetDirty(profile);
        }
        var volGo = GameObject.Find("Post-process Volume");
        if (volGo == null) volGo = new GameObject("Post-process Volume");
        var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        var light = GameObject.Find("LIGHTING");
        if (light != null) volGo.transform.SetParent(light.transform, false);

        // 4. Anti-aliasing: 4x MSAA on the URP asset and on the mirror render textures.
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRenderPipelineAsset.asset");
        urp.msaaSampleCount = 4;
        EditorUtility.SetDirty(urp);
        foreach (var n in new[] { "Brown", "Black" })
        {
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/_Course Library/Source Files/Textures/Mirror_Texture_" + n + ".renderTexture");
            if (rt != null) { rt.antiAliasing = 4; EditorUtility.SetDirty(rt); }
        }

        // 5. A painting beside the mirror, to show the texture settings.
        var mirror = Find("Mirror_Square_Brown");
        if (Find("Art_Painting_Sunset", false) == null)
        {
            var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Course Library/_Prefabs/Art/Art_Painting_Sunset.prefab"));
            art.transform.SetPositionAndRotation(mirror.position + mirror.right * 1.3f, mirror.rotation);
            if (stat != null) art.transform.SetParent(stat.transform, true);
        }

        // Texture import: aniso filtering and mipmaps on the floor texture.
        var imp = AssetImporter.GetAtPath("Assets/_Course Library/Source Files/Textures/WoodFloor_Texture.png") as TextureImporter;
        if (imp != null) { imp.mipmapEnabled = true; imp.anisoLevel = 8; imp.SaveAndReimport(); }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 3.2 setup complete");
    }

    static Transform Find(string name, bool throwIfMissing = true)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        if (throwIfMissing) throw new System.Exception("Not found: " + name);
        return null;
    }
}
