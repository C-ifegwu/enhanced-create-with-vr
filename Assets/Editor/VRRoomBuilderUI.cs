// Phase 5 (UI, objectives, projector, hints) and phase 6 (scene navigation, build settings, course scene links).
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static partial class VRRoomBuilder
{
    static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.12f, 0.94f);
    static readonly Color Accent = new Color(0.25f, 0.8f, 0.95f);

    // ---------------------------------------------------------------- Phase 5
    [MenuItem("Tools/Create With VR/VR Room/5 UI, objectives and hints")]
    public static void Phase5()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
        var cam = FindT("Main Camera").GetComponent<Camera>();

        foreach (var n in new[] { "VR Room Menu", "Start UI", "Control Panel", "Fade Canvas", "Hint Door", "Hint Table" })
        {
            var o = GameObject.Find(n);
            if (o != null) UnityEngine.Object.DestroyImmediate(o);
        }

        // Navigation object: menu handlers plus fade navigator.
        var menuGo = new GameObject("VR Room Menu");
        var menu = menuGo.AddComponent<VRRoomMenu>();
        var nav = menuGo.AddComponent<SceneNavigator>();
        nav.fadeGroup = MakeFadeCanvas(cam);
        menu.navigator = nav;
        menu.courseScene = "Chibueze_Room";
        menu.teleportProvider = UnityEngine.Object.FindFirstObjectByType<TeleportationProvider>(FindObjectsInactive.Include);
        menu.resettableRoot = FindT("Interactables");
        // The physics door leaf is reset too.
        var doorRb = FindT("GlassDoor").Find("glass_panel_1_door").GetComponent<Rigidbody>();

        // ---------- Start UI ----------
        var start = MakeCanvas("Start UI", new Vector3(0f, 1.55f, -0.3f), Quaternion.identity, new Vector2(1100f, 760f));
        Panel(start.transform, "Background", Vector2.zero, new Vector2(1100f, 760f), PanelColor);
        Panel(start.transform, "Accent Bar", new Vector2(0f, 300f), new Vector2(1100f, 8f), Accent);
        Txt(start.transform, "Title", "VR ROOM", 110, Accent, new Vector2(0f, 225f), new Vector2(1000f, 140f), font, FontStyles.Bold);
        Txt(start.transform, "Subtitle", "Storage bay escape - grab, place, push", 40, Color.white, new Vector2(0f, 140f), new Vector2(1000f, 60f), font);
        Txt(start.transform, "Goals",
            "1  Place the flashlight, rook and knight on the matching pads of the puzzle table.\n" +
            "2  Grab the handle and push the glass door open.\n" +
            "3  Check the time on the wall clock.", 32, new Color(0.85f, 0.9f, 0.95f), new Vector2(0f, 20f), new Vector2(980f, 190f), font, FontStyles.Normal, TextAlignmentOptions.Left);
        Txt(start.transform, "Controls",
            "Point and pull the trigger to press buttons   |   Grip to grab   |   Hold A / X or touch the thumbstick to aim, squeeze grip to teleport   |   Y shows the control panel", 24,
            new Color(0.6f, 0.7f, 0.8f), new Vector2(0f, -110f), new Vector2(1000f, 80f), font);
        var startBtn = Btn(start.transform, "Start Button", "START", new Vector2(-220f, -240f), new Vector2(380f, 110f), new Color(0.12f, 0.6f, 0.4f), font, 52);
        var backBtn = Btn(start.transform, "Course Button", "CREATE WITH VR ROOM", new Vector2(220f, -240f), new Vector2(430f, 110f), new Color(0.2f, 0.3f, 0.55f), font, 30);
        UnityEventTools.AddVoidPersistentListener(startBtn.onClick, menu.StartExperience);
        UnityEventTools.AddVoidPersistentListener(backBtn.onClick, menu.GoToCourseScene);
        menu.startPanel = start.gameObject;

        // ---------- Control panel (in-project UI) ----------
        var hud = MakeCanvas("Control Panel", new Vector3(1.4f, 1.4f, -0.8f), Quaternion.Euler(0f, -30f, 0f), new Vector2(820f, 1060f));
        Panel(hud.transform, "Background", Vector2.zero, new Vector2(820f, 1060f), PanelColor);
        Panel(hud.transform, "Accent Bar", new Vector2(0f, 480f), new Vector2(820f, 8f), Accent);
        Txt(hud.transform, "Title", "CONTROL PANEL", 56, Accent, new Vector2(-40f, 430f), new Vector2(620f, 80f), font, FontStyles.Bold);
        var close = Btn(hud.transform, "Close Button", "X", new Vector2(350f, 430f), new Vector2(80f, 80f), new Color(0.6f, 0.2f, 0.2f), font, 40);
        UnityEventTools.AddVoidPersistentListener(close.onClick, menu.ToggleHud);

        var obj = Txt(hud.transform, "Objectives Text", "Objectives", 34, Color.white, new Vector2(0f, 270f), new Vector2(720f, 230f), font, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var tracker = hud.gameObject.AddComponent<PuzzleTracker>();
        tracker.label = obj;
        tracker.doorHinge = doorRb.GetComponent<HingeJoint>();
        tracker.sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(s => s.name.StartsWith("Socket ")).OrderBy(s => s.name).ToArray();
        var snd = hud.gameObject.AddComponent<AudioSource>();
        snd.playOnAwake = false; snd.spatialBlend = 1f;
        snd.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Course Library/Audio/FX/SFX_Cork_Pop.ogg");
        tracker.snapSound = snd;
        var win = hud.gameObject.AddComponent<AudioSource>();
        win.playOnAwake = false; win.spatialBlend = 0f;
        win.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Course Library/Audio/FX/SFX_Game_Success.ogg");
        UnityEventTools.AddVoidPersistentListener(tracker.onAllSocketsFilled, win.Play);

        // Buttons: reset, rooms, scene navigation.
        var reset = Btn(hud.transform, "Reset Button", "RESET OBJECTS", new Vector2(-195f, 90f), new Vector2(360f, 100f), new Color(0.65f, 0.4f, 0.1f), font, 32);
        UnityEventTools.AddVoidPersistentListener(reset.onClick, menu.ResetObjects);
        var toStorage = Btn(hud.transform, "Storage Button", "STORAGE ROOM", new Vector2(-195f, -30f), new Vector2(360f, 100f), new Color(0.2f, 0.3f, 0.55f), font, 32);
        var toMain = Btn(hud.transform, "Main Button", "MAIN ROOM", new Vector2(195f, -30f), new Vector2(360f, 100f), new Color(0.2f, 0.3f, 0.55f), font, 32);
        UnityEventTools.AddObjectPersistentListener<Transform>(toStorage.onClick, menu.TeleportTo, FindT("Anchor Storage Room"));
        UnityEventTools.AddObjectPersistentListener<Transform>(toMain.onClick, menu.TeleportTo, FindT("Anchor Main Room"));
        var course = Btn(hud.transform, "Course Button", "CREATE WITH VR ROOM", new Vector2(195f, 90f), new Vector2(360f, 100f), new Color(0.12f, 0.6f, 0.4f), font, 26);
        UnityEventTools.AddVoidPersistentListener(course.onClick, menu.GoToCourseScene);

        // Comfort settings, like the course scene.
        var snapGo = FindT("Snap Turn").gameObject;
        var snapToggle = ToggleRow(hud.transform, "Snap Turning", new Vector2(0f, -190f), font);
        UnityEventTools.AddPersistentListener(snapToggle.onValueChanged, new UnityAction<bool>(snapGo.SetActive));
        var distToggle = ToggleRow(hud.transform, "Distance Grab", new Vector2(0f, -280f), font);
        foreach (var side in new[] { "Left", "Right" })
        {
            var nfGo = FindT(side + " Near-Far Interactor").gameObject;
            var tgl = Get<ToggleNearFarAttach>(nfGo);
            tgl.nearFarInteractor = nfGo.GetComponent<NearFarInteractor>();
            UnityEventTools.AddPersistentListener(distToggle.onValueChanged, new UnityAction<bool>(tgl.SetFarAttachMode));
        }
        Txt(hud.transform, "Help", "Y button: show / hide this panel\nGrip: grab   Trigger: select\nA / X or thumbstick touch: aim, then squeeze grip to teleport", 28,
            new Color(0.6f, 0.7f, 0.8f), new Vector2(0f, -420f), new Vector2(740f, 150f), font);
        menu.hudPanel = hud.gameObject;

        // ---------- Projector (extra interaction) ----------
        var projector = FindT("Projector");
        var ps = projector.GetComponentInChildren<ParticleSystem>(true);
        var pl = projector.GetComponentInChildren<Light>(true);
        var pt = Get<ProjectorToggle>(projector.gameObject);
        pt.particles = ps; pt.projectorLight = pl;
        var pAudio = Get<AudioSource>(projector.gameObject);
        pAudio.playOnAwake = false; pAudio.spatialBlend = 1f;
        pAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Course Library/Audio/FX/SFX_Machine_Powerup.ogg");
        pt.sound = pAudio;
        var simple = Get<XRSimpleInteractable>(projector.gameObject);
        simple.selectMode = InteractableSelectMode.Single;
        for (int i = simple.selectEntered.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(simple.selectEntered, i);
        UnityEventTools.AddVoidPersistentListener(simple.selectEntered, pt.Toggle);
        Get<HoverHighlight>(projector.gameObject);

        // ---------- In-world hints ----------
        Hint("Hint Door", "Grab the handle and push\nor pull the glass door", new Vector3(0f, 2.55f, 5.6f), font);
        Hint("Hint Table", "Match each object\nto its glowing pad", new Vector3(-1.5f, 1.85f, 2.35f), font);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("VR Room phase 5 complete");
    }

    // ---------------------------------------------------------------- Phase 6
    [MenuItem("Tools/Create With VR/VR Room/6 Navigation and build settings")]
    public static void Phase6()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CoursePath, true),
            new EditorBuildSettingsScene(VRPath, true)
        };

        // Course scene: navigator, a button to the VR Room, and the wall clock.
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(CoursePath, OpenSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);

        var loader = FindT("Scene Loader").gameObject;
        var nav = Get<SceneNavigator>(loader);
        nav.fadeGroup = FindT("Fade_Canvas").GetComponent<CanvasGroup>();

        var okRt = FindT("OK_Button").GetComponent<RectTransform>();
        var existing = okRt.parent.Find("VR Room Button");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var dup = UnityEngine.Object.Instantiate(okRt.gameObject, okRt.parent);
        dup.name = "VR Room Button";
        var btn = dup.GetComponent<Button>();
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(btn.onClick, i);
        var rt = dup.GetComponent<RectTransform>();
        float w = okRt.sizeDelta.x;
        okRt.anchoredPosition += new Vector2(-w * 0.6f, 0f);
        rt.anchoredPosition = okRt.anchoredPosition + new Vector2(w * 1.2f, 0f);
        rt.sizeDelta = new Vector2(w * 1.25f, okRt.sizeDelta.y);
        var label = dup.GetComponentInChildren<TextMeshProUGUI>();
        label.text = "Enter VR Room";
        label.enableAutoSizing = true;
        UnityEventTools.AddStringPersistentListener(btn.onClick, nav.GoToScene, "VR_Room");

        // Wall clock in the course room as well.
        const string clockPath = "Assets/Prefabs/Wall Clock.prefab";
        var oldClock = GameObject.Find("Wall Clock");
        if (oldClock != null) UnityEngine.Object.DestroyImmediate(oldClock);
        var clock = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(clockPath));
        clock.transform.SetPositionAndRotation(new Vector3(-2.93f, 1.85f, 0.2f), Quaternion.Euler(0f, -90f, 0f));
        var statics = GameObject.Find("STATIC");
        if (statics != null) clock.transform.SetParent(statics.transform, true);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("VR Room phase 6 complete");
    }

    // ---------------------------------------------------------------- UI helpers
    static CanvasGroup MakeFadeCanvas(Camera cam)
    {
        var fadeGo = new GameObject("Fade Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = fadeGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.1f;
        canvas.sortingOrder = 10;
        var img = new GameObject("Image", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(fadeGo.transform, false);
        img.GetComponent<Image>().color = Color.black;
        img.GetComponent<Image>().raycastTarget = false;
        img.GetComponent<RectTransform>().localScale = new Vector3(100f, 100f, 1f);
        var group = fadeGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    static Canvas MakeCanvas(string name, Vector3 pos, Quaternion rot, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one * 0.001f;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 3f;
        scaler.referencePixelsPerUnit = 100f;
        return canvas;
    }

    static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box, TMP_FontAsset font,
        FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.font = font; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, TMP_FontAsset font, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = Color.white;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = Color.Lerp(color, Color.white, 0.35f);
        cb.pressedColor = Color.Lerp(color, Color.black, 0.3f);
        cb.selectedColor = color;
        cb.colorMultiplier = 1f;
        btn.colors = cb;
        Txt(go.transform, "Label", label, fontSize, Color.white, Vector2.zero, size - new Vector2(20f, 10f), font, FontStyles.Bold);
        return btn;
    }

    static Toggle ToggleRow(Transform parent, string label, Vector2 pos, TMP_FontAsset font)
    {
        var row = new GameObject(label + " Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.GetComponent<RectTransform>().anchoredPosition = pos;
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 80f);
        Txt(row.transform, "Label", label, 38, Color.white, new Vector2(-70f, 0f), new Vector2(400f, 70f), font, FontStyles.Normal, TextAlignmentOptions.Left);
        var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(row.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(210f, 0f);
        rt.sizeDelta = new Vector2(80f, 80f);
        var bg = Panel(go.transform, "Background", Vector2.zero, new Vector2(80f, 80f), Color.white);
        bg.raycastTarget = true;
        var check = Panel(bg.transform, "Checkmark", Vector2.zero, new Vector2(50f, 50f), new Color(0.12f, 0.6f, 0.4f));
        var tg = go.GetComponent<Toggle>();
        tg.targetGraphic = bg;
        tg.graphic = check;
        tg.isOn = true;
        return tg;
    }

    static void Hint(string name, string text, Vector3 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.font = font;
        t.fontSize = 2f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.8f, 0.95f, 1f);
        t.rectTransform.sizeDelta = new Vector2(2.2f, 0.7f);
    }
}
