// Builds the realistic hand prefabs (skinned OpenXR hand mesh + HandPoseAnimator) and an editor preview of the poses.
// Run from: Tools > Create With VR > Hands: Build Prefabs, then Hands: Preview Poses.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HandsSetup
{
    const string SkinPath = "Assets/Hands/Hand_Skin.mat";

    [MenuItem("Tools/Create With VR/Hands: Build Prefabs")]
    public static void Build()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(SkinPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.86f, 0.66f, 0.54f));
            mat.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(mat, SkinPath);
        }
        foreach (var side in new[] { "Left", "Right" })
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Hands/" + side + "Hand.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = "Realistic Hand " + side;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.sharedMaterial = mat;
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var anim = go.AddComponent<HandPoseAnimator>();
            anim.side = side == "Left" ? HandPoseAnimator.Side.Left : HandPoseAnimator.Side.Right;
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Realistic Hand " + side + ".prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Hand prefabs built");
    }

    [MenuItem("Tools/Create With VR/Hands: Preview Poses")]
    public static void Preview()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var left = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Realistic Hand Left.prefab");
        var right = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Realistic Hand Right.prefab");
        float[,] poses = { { 0, 0 }, { 0, 1 }, { 1, 0 }, { 1, 1 } };   // open, trigger only, grip only, fist
        for (int i = 0; i < 4; i++)
        {
            var l = (GameObject)PrefabUtility.InstantiatePrefab(left);
            l.transform.position = new Vector3(i * 0.3f, 0, 0);
            l.GetComponent<HandPoseAnimator>().SetPose(poses[i, 0], poses[i, 1]);
            var r = (GameObject)PrefabUtility.InstantiatePrefab(right);
            r.transform.position = new Vector3(i * 0.3f, 0, 0.35f);
            r.GetComponent<HandPoseAnimator>().SetPose(poses[i, 0], poses[i, 1]);
        }
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.LookAt(new Vector3(0.45f, 0, 0.17f), Quaternion.Euler(60, 20, 0), 0.7f);
            sv.Repaint();
        }
    }
}
