// Lesson 3.3 setup helper: baked/mixed lights, lighting settings asset, light probes, bake.
// Run from: Tools > Create With VR > Lesson 3.3 Setup.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class Lesson33Setup
{
    [MenuItem("Tools/Create With VR/Lesson 3.3 Setup")]
    public static void Run()
    {
        // URP: more lights per object, shorter shadow distance.
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRenderPipelineAsset.asset");
        urp.maxAdditionalLightsCount = 8;
        urp.shadowDistance = 15f;
        EditorUtility.SetDirty(urp);

        // Remove the two default point lights that ship inside the Room prefab.
        var room = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "Room_Modern");
        var src = PrefabUtility.GetCorrespondingObjectFromSource(room.gameObject);
        if (src != null)
        {
            var path = AssetDatabase.GetAssetPath(src);
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var l in root.GetComponentsInChildren<Light>(true).Where(l => l.type == LightType.Point).ToArray())
                Object.DestroyImmediate(l.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // Real light sources: a warm point light in the fireplace and a spot above the coffee table.
        AddLight("Fireplace_Modern", "Fire Light", LightType.Point, new Color(1f, 0.55f, 0.2f), 2.5f, 4f, new Vector3(0, 0.4f, 0.3f));
        AddLight("Table_Coffee_Modern", "Table Spot", LightType.Spot, new Color(1f, 0.92f, 0.8f), 3f, 6f, new Vector3(0, 2.2f, 0));

        // Directional light: mixed mode, brighter, more indirect bounce.
        var sun = Object.FindFirstObjectByType<Light>(FindObjectsInactive.Include);
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l.type == LightType.Directional) sun = l;
        sun.lightmapBakeType = LightmapBakeType.Mixed;
        sun.intensity = 1.2f;
        sun.bounceIntensity = 1.5f;

        // Every non-moving light is baked.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l.type != LightType.Directional) l.lightmapBakeType = LightmapBakeType.Baked;

        // Large background meshes are left out of the lightmap.
        foreach (var n in new[] { "Foreground_Hills", "Background_Hills" })
        {
            var t = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == n);
            if (t == null) continue;
            var so = new SerializedObject(t.GetComponent<MeshRenderer>());
            so.FindProperty("m_ScaleInLightmap").floatValue = 0f;
            so.ApplyModifiedProperties();
        }

        // Light probe group around the room so dynamic objects pick up baked light.
        var probes = room.GetComponentInChildren<LightProbeGroup>(true);
        if (probes == null)
        {
            var go = new GameObject("Light Probe Group");
            go.transform.SetParent(room, false);
            probes = go.AddComponent<LightProbeGroup>();
            var pos = new System.Collections.Generic.List<Vector3>();
            foreach (var x in new[] { -3f, 0f, 3f })
                foreach (var z in new[] { -3f, 0f, 3f })
                    foreach (var y in new[] { 0.5f, 1.5f, 2.5f })
                        pos.Add(new Vector3(x, y, z));
            probes.probePositions = pos.ToArray();
        }

        // Lighting settings asset: final-quality values from the lesson.
        const string lsPath = "Assets/Settings/Chibueze_LightingSettings.lighting";
        var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
        if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, lsPath); }
        ls.bakedGI = true;
        ls.mixedBakeMode = MixedLightingMode.Subtractive;
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        ls.lightmapResolution = 10f;      // raise to 20-80 for the final bake
        ls.lightmapMaxSize = 4096;
        ls.lightmapCompression = LightmapCompression.None;
        ls.indirectScale = 1.5f;
        EditorUtility.SetDirty(ls);
        Lightmapping.lightingSettings = ls;

        // Everything under STATIC contributes to the bake.
        var stat = GameObject.Find("STATIC");
        if (stat != null)
            foreach (var t in stat.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)int.MaxValue);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Lightmapping.BakeAsync();
        Debug.Log("Lesson 3.3 setup complete - bake started");
    }

    static void AddLight(string parent, string name, LightType type, Color color, float intensity, float range, Vector3 local)
    {
        var p = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == parent);
        if (p == null || p.Find(name) != null) return;
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.localPosition = local;
        if (type == LightType.Spot) go.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var l = go.AddComponent<Light>();
        l.type = type; l.color = color; l.intensity = intensity; l.range = range;
        l.lightmapBakeType = LightmapBakeType.Baked;
    }
}
