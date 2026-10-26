// Lesson 2.4 setup helper: world-space welcome and reset UI, secondary-button reset, start anchor and poke interactors.
// Run it from the menu: Tools > Create With VR > Lesson 2.4 Setup.
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class Lesson24Setup
{
    [MenuItem("Tools/Create With VR/Lesson 2.4 Setup")]
    public static void Run()
    {
        var origin = Find("XR Origin (VR)");

        // Start anchor: a duplicate of the mat placed where the player starts.
        var anchorGo = Find("Start Anchor", false);
        if (anchorGo == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Course Library/_Prefabs/Rugs/Mat_Round_Blue.prefab");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Start Anchor";
            inst.transform.SetPositionAndRotation(new Vector3(origin.position.x, 0.01f, origin.position.z), Quaternion.Euler(0, origin.eulerAngles.y, 0));
            var stat = GameObject.Find("STATIC");
            if (stat != null) inst.transform.SetParent(stat.transform, true);
            anchorGo = inst.transform;
        }
        var anchor = anchorGo.GetComponent<TeleportationAnchor>();

        // INTERFACE organizer and XR UI Canvas.
        var interfaceGo = GameObject.Find("INTERFACE");
        if (interfaceGo == null) { interfaceGo = new GameObject("INTERFACE"); }
        interfaceGo.transform.position = Vector3.zero;
        Selection.activeGameObject = interfaceGo;
        EditorApplication.ExecuteMenuItem("GameObject/XR/UI Canvas");
        var canvasGo = Selection.activeGameObject;
        if (canvasGo == null || canvasGo.GetComponent<Canvas>() == null) throw new System.Exception("XR UI Canvas not created");
        canvasGo.transform.SetParent(interfaceGo.transform, false);
        canvasGo.name = "Canvas";
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.sortingOrder = 1;
        var rt = canvasGo.GetComponent<RectTransform>();
        rt.localScale = Vector3.one * 0.1f;
        Vector3 fwd = anchorGo.forward;
        canvasGo.transform.position = anchorGo.position + fwd * 2f + Vector3.up * 1.5f;
        canvasGo.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Course Library/Fonts/Inter-Regular-slnt=0 SDF.asset");

        // Welcome panel with OK button.
        var welcome = MakePanel(canvasGo.transform, "Welcome_Background", font,
            "Welcome to the room!\nPoint a ray with A/X or thumbstick touch to teleport,\ngrab objects with the grip, and press B/Y to reset.", "Welcome Text");
        var ok = MakeButton(welcome.transform, "OK_Button", "Got it!", font, new Vector2(0, -3.2f));
        UnityEventTools.AddBoolPersistentListener(ok.onClick, welcome.SetActive, false);

        // Reset panel with Cancel / Yes buttons.
        var reset = MakePanel(canvasGo.transform, "Reset_Background", font,
            "Reset the room?\nThis puts everything back to how it started.", "Reset Text");
        var cancel = MakeButton(reset.transform, "Cancel_Button", "Cancel", font, new Vector2(-3.5f, -3.2f));
        var yes = MakeButton(reset.transform, "Yes_Button", "Yes", font, new Vector2(3.5f, -3.2f));
        UnityEventTools.AddBoolPersistentListener(cancel.onClick, reset.SetActive, false);
        var loader = yes.gameObject.AddComponent<LoadScene>();
        UnityEventTools.AddVoidPersistentListener(yes.onClick, loader.ReloadCurrentScene);
        reset.SetActive(false);

        // Secondary button (B/Y): show reset panel, hide welcome, teleport back to the start.
        var teleport = Find("Teleport");
        var tp = origin.GetComponent<TeleportPlayer>();
        if (tp == null) tp = origin.gameObject.AddComponent<TeleportPlayer>();
        tp.anchor = anchor;
        tp.provider = teleport.GetComponent<TeleportationProvider>();
        var press = origin.GetComponent<OnButtonPress>();
        if (press == null) press = origin.gameObject.AddComponent<OnButtonPress>();
        var action = new InputAction("Show Reset", InputActionType.Button);
        action.AddBinding("<XRController>{RightHand}/secondaryButton");
        press.action = action;
        for (int i = press.OnPress.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(press.OnPress, i);
        UnityEventTools.AddBoolPersistentListener(press.OnPress, reset.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(press.OnPress, welcome.SetActive, false);
        UnityEventTools.AddVoidPersistentListener(press.OnPress, tp.Teleport);
        EditorUtility.SetDirty(press);
        EditorUtility.SetDirty(tp);

        // Poke interactors on both hands (UI layer).
        foreach (var side in new[] { "Left", "Right" })
        {
            var ctrl = Find(side + " Hand Controller");
            var existing = ctrl.Find("Poke Interactor");
            if (existing != null) continue;
            var go = new GameObject("Poke Interactor");
            go.transform.SetParent(ctrl, false);
            go.layer = LayerMask.NameToLayer("UI");
            go.AddComponent<XRPokeInteractor>();
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 2.4 setup complete");
    }

    static GameObject MakePanel(Transform parent, string name, TMP_FontAsset font, string message, string textName)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(20, 10);
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f);
        img.raycastTarget = false;
        var t = new GameObject(textName, typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(go.transform, false);
        var trt = t.GetComponent<RectTransform>();
        trt.anchoredPosition = new Vector2(0, 1.2f);
        trt.sizeDelta = new Vector2(18, 5.5f);
        var tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.font = font;
        tmp.fontSize = 1.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return go;
    }

    static Button MakeButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(5, 2);
        var img = go.GetComponent<Image>();
        img.color = Color.white;
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
        colors.highlightedColor = new Color(0.4f, 0.8f, 1f);
        colors.pressedColor = new Color(0.2f, 0.6f, 0.9f);
        colors.selectedColor = new Color(0.4f, 0.8f, 1f);
        btn.colors = colors;
        var t = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(go.transform, false);
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = 1.3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return btn;
    }

    static Transform Find(string name, bool throwIfMissing = true)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        if (throwIfMissing) throw new System.Exception("Not found: " + name);
        return null;
    }
}
