// Lesson 3.4 setup helper: app name/version and recommended Meta Quest (Android) player settings.
// Run from: Tools > Create With VR > Lesson 3.4 Setup.
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

public static class Lesson34Setup
{
    [MenuItem("Tools/Create With VR/Lesson 3.4 Setup")]
    public static void Run()
    {
        PlayerSettings.companyName = "Chibueze Ifegwu";
        PlayerSettings.productName = "Chibueze VR Room";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.chibueze.vrroom");

        // Quest recommendations: IL2CPP, ARM64, API 29+, Linear colour, Vulkan.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, true);

        // Scenes in build.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Chibueze_Room.unity", true) };

        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 3.4 setup complete");
    }
}
