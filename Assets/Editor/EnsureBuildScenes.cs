using System.Collections.Generic;
using System.Linq;
using UnityEditor;
// Keeps both scenes in the build, in order, so scene navigation can never silently break in a headset build.
[InitializeOnLoad]
public static class EnsureBuildScenes
{
    static readonly string[] Required = { "Assets/Scenes/Chibueze_Room.unity", "Assets/Scenes/VR_Room.unity" };
    static EnsureBuildScenes() { EditorApplication.delayCall += Apply; }
    [MenuItem("Tools/Create With VR/Ensure both scenes in build")]
    public static void Apply()
    {
        var list = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        foreach (var p in Required)
        {
            var existing = list.FindIndex(s => s.path == p);
            if (existing < 0) { list.Add(new EditorBuildSettingsScene(p, true)); changed = true; }
            else if (!list[existing].enabled) { list[existing] = new EditorBuildSettingsScene(p, true); changed = true; }
        }
        var ordered = Required.Select(p => list.First(s => s.path == p)).Concat(list.Where(s => !Required.Contains(s.path))).ToArray();
        if (changed || !ordered.Select(s => s.path).SequenceEqual(list.Select(s => s.path))) EditorBuildSettings.scenes = ordered;
    }
}
