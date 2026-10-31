// Lesson 3.1 setup helper: fade-in from black, settings panel with Snap Turning and Distance Grab toggles.
// Run it from the menu: Tools > Create With VR > Lesson 3.1 Setup.
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class Lesson31Setup
{
    const string Tex = "Assets/_Course Library/Source Files/Textures/";

    [MenuItem("Tools/Create With VR/Lesson 3.1 Setup")]
    public static void Run()
    {
        var cam = Find("Main Camera").GetComponent<Camera>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Course Library/Fonts/Inter-Regular-slnt=0 SDF.asset");

        // 1-2. Fade canvas and scene loader.
        var fadeGo = new GameObject("Fade_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = fadeGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.1f;
        canvas.sortingOrder = 1;
        var img = new GameObject("Image", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(fadeGo.transform, false);
        img.GetComponent<Image>().color = Color.black;
        img.GetComponent<Image>().raycastTarget = false;
        img.GetComponent<RectTransform>().localScale = new Vector3(100, 100, 1);
        var group = fadeGo.AddComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
        var fade = fadeGo.AddComponent<FadeCanvas>();
        var loader = new GameObject("Scene Loader");
        var onLoad = loader.AddComponent<OnSceneLoad>();
        UnityEventTools.AddVoidPersistentListener(onLoad.OnLoad, fade.StartFadeOut);
        var iface = GameObject.Find("INTERFACE");
        if (iface != null) { fadeGo.transform.SetParent(iface.transform, false); }

        // 3. Settings button on the welcome panel, settings panel with toggles.
        var welcome = Find("Welcome_Background").gameObject;
        var canvasTr = welcome.transform.parent;
        var gear = MakeIconButton(welcome.transform, "Settings Button", Tex + "Icon_Gear.png", new Vector2(8.3f, 3.6f));
        var settings = MakePanel(canvasTr, "Settings Background");
        var close = MakeIconButton(settings.transform, "Close Button", Tex + "Icon_Close.png", new Vector2(8.3f, 3.6f));

        var snapGo = Find("Snap Turn").gameObject;
        var snapToggle = MakeSettingRow(settings.transform, "Snap Turning", font, 1.5f, true);
        UnityEventTools.AddPersistentListener(snapToggle.onValueChanged, new UnityAction<bool>(snapGo.SetActive));

        // Distance grab: one ToggleNearFarAttach per hand, both driven by one toggle.
        var distToggle = MakeSettingRow(settings.transform, "Distance Grab", font, -1.5f, true);
        foreach (var side in new[] { "Left", "Right" })
        {
            var nfGo = Find(side + " Near-Far Interactor").gameObject;
            var tgl = nfGo.GetComponent<ToggleNearFarAttach>();
            if (tgl == null) tgl = nfGo.AddComponent<ToggleNearFarAttach>();
            tgl.nearFarInteractor = nfGo.GetComponent<NearFarInteractor>();
            UnityEventTools.AddPersistentListener(distToggle.onValueChanged, new UnityAction<bool>(tgl.SetFarAttachMode));
        }

        UnityEventTools.AddBoolPersistentListener(gear.onClick, settings.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(gear.onClick, welcome.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(close.onClick, settings.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(close.onClick, welcome.SetActive, true);
        settings.SetActive(false);

        // 6. The reset (B/Y) press also hides the settings panel.
        var press = Find("XR Origin (VR)").GetComponent<OnButtonPress>();
        UnityEventTools.AddBoolPersistentListener(press.OnPress, settings.SetActive, false);
        EditorUtility.SetDirty(press);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Lesson 3.1 setup complete");
    }

    static GameObject MakePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(20, 10);
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f);
        img.raycastTarget = false;
        return go;
    }

    static Button MakeIconButton(Transform parent, string name, string spritePath, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1.6f, 1.6f);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.highlightedColor = new Color(0.4f, 0.8f, 1f);
        c.pressedColor = new Color(0.2f, 0.6f, 0.9f);
        btn.colors = c;
        return btn;
    }

    static Toggle MakeSettingRow(Transform parent, string label, TMP_FontAsset font, float y, bool isOn)
    {
        var t = new GameObject(label + " Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(parent, false);
        var trt = t.GetComponent<RectTransform>();
        trt.anchoredPosition = new Vector2(-3f, y);
        trt.sizeDelta = new Vector2(10, 2);
        var tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = 1.5f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        var go = new GameObject(label + " Toggle", typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(t.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(7.5f, 0);
        rt.sizeDelta = new Vector2(1.6f, 1.6f);
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = Color.white;
        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(bg.transform, false);
        var crt = check.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.15f, 0.15f); crt.anchorMax = new Vector2(0.85f, 0.85f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        check.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.9f);
        var tg = go.GetComponent<Toggle>();
        tg.targetGraphic = bg.GetComponent<Image>();
        tg.graphic = check.GetComponent<Image>();
        tg.isOn = isOn;
        return tg;
    }

    static Transform Find(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name) return t;
        throw new System.Exception("Not found: " + name);
    }
}
