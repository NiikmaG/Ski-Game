using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: SkiGame ▸ Setup Scene
/// Creates GameManager, GameAudio, PauseMenu + PauseCanvas, RaceUI + GameCanvas
/// as real scene objects — visible and editable in Hierarchy in Edit mode.
/// </summary>
public static class SkiGameSetup
{
    [MenuItem("SkiGame/Setup Scene")]
    static void Setup()
    {
        EnsureGameManager();
        EnsureGameAudio();
        EnsurePauseMenu();
        EnsureRaceUI();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SkiGame] Setup complete. Press Ctrl+S to save the scene.");
    }

    // ─── GameManager ──────────────────────────────────────────────────────────

    static void EnsureGameManager()
    {
        if (Object.FindAnyObjectByType<GameManager>() != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
    }

    // ─── GameAudio ────────────────────────────────────────────────────────────

    static void EnsureGameAudio()
    {
        if (Object.FindAnyObjectByType<GameAudio>() != null) return;
        var go = new GameObject("GameAudio");
        go.AddComponent<GameAudio>();
        Undo.RegisterCreatedObjectUndo(go, "Create GameAudio");
    }

    // ─── PauseMenu + PauseCanvas ──────────────────────────────────────────────

    static void EnsurePauseMenu()
    {
        var pauseMenu = GetOrCreateComponent<PauseMenu>("PauseMenu");

        var so = new SerializedObject(pauseMenu);
        var canvasProp = so.FindProperty("pauseCanvas");
        if (canvasProp.objectReferenceValue != null) return; // already set up

        var canvasGO = BuildCanvas("PauseCanvas", 100);
        BuildPauseHierarchy(canvasGO.transform);

        canvasProp.objectReferenceValue = canvasGO.GetComponent<Canvas>();
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create PauseCanvas");
    }

    static void BuildPauseHierarchy(Transform canvasT)
    {
        // Dark overlay (full screen)
        var rootGO = UIChild(canvasT, "PauseMenuRoot");
        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.55f);
        Stretch(RT(rootGO));

        // Panel (560×560, centered)
        var panelGO = UIChild(rootGO.transform, "PausePanel");
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.05f, 0.09f, 0.92f);
        SetCenter(RT(panelGO), Vector2.zero, new Vector2(560, 560));

        var panel = panelGO.transform;

        // Title
        TMP(panel, "Text_PAUSED", "PAUSED", 58, Color.white, new Vector2(0, 190), new Vector2(500, 72));

        // Buttons
        Btn(panel, "Button_RESUME",  "RESUME",  new Vector2(0,  112));
        Btn(panel, "Button_RESTART", "RESTART", new Vector2(0,   34));
        Btn(panel, "Button_QUIT",    "QUIT",    new Vector2(0,  -44));

        // Volume sliders
        SliderRow(panel, "MUSIC", new Vector2(0, -146));
        SliderRow(panel, "GAME",  new Vector2(0, -222));
    }

    // ─── RaceUI + GameCanvas ──────────────────────────────────────────────────

    static void EnsureRaceUI()
    {
        var raceUI = GetOrCreateComponent<RaceUI>("RaceUI");

        var so = new SerializedObject(raceUI);
        var canvasProp = so.FindProperty("gameCanvas");
        if (canvasProp.objectReferenceValue != null) return;

        var canvasGO = BuildCanvas("GameCanvas", 10);
        BuildGameHierarchy(canvasGO.transform);

        canvasProp.objectReferenceValue = canvasGO.GetComponent<Canvas>();
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameCanvas");
    }

    static void BuildGameHierarchy(Transform t)
    {
        // Timer  — top left
        TMP(t, "T_Timer", "00:00.00", 52, Color.white,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(340, 65));

        // Best time — top right
        TMP(t, "T_BestTime", "Best: --:--", 32, new Color(1f, 0.85f, 0f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(320, 50));

        // Leaderboard — top right below best time
        var lb = TMP(t, "T_Leaderboard", "Leaderboard\n--", 28, new Color(0.92f, 0.96f, 1f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -82), new Vector2(360, 190));
        lb.alignment = TextAlignmentOptions.TopRight;

        // Race state — top center
        TMP(t, "T_State", "", 36, Color.white,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -20), new Vector2(700, 55));

        // Penalty flash — center
        TMP(t, "T_Penalty", "", 54, Color.red,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 120), new Vector2(700, 80));

        // Race over panel — centered
        var panelGO = UIChild(t, "Panel_RaceOver");
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        SetCenter(RT(panelGO), Vector2.zero, new Vector2(620, 500));
        panelGO.SetActive(false);

        var pt = panelGO.transform;
        TMP(pt, "T_FinalTime", "Time: 00:00.00", 52, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 138), new Vector2(600, 70));

        TMP(pt, "T_Penalties", "Penalty: +0s    Missed gates: 0", 28, new Color(0.9f, 0.95f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 88), new Vector2(600, 48));

        var rec = TMP(pt, "T_Record", ">> NEW RECORD! <<", 38, Color.yellow,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 42), new Vector2(500, 55));
        rec.gameObject.SetActive(false);

        Btn(pt, "Btn_Restart",  "RESTART",    new Vector2(0,  -20));
        Btn(pt, "Btn_NextLevel","NEXT LEVEL",  new Vector2(0, -100));
        Btn(pt, "Btn_Quit",     "QUIT",        new Vector2(0, -180));
    }

    // ─── UI Helpers ───────────────────────────────────────────────────────────

    static GameObject BuildCanvas(string name, int sortOrder)
    {
        var existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortOrder;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static T GetOrCreateComponent<T>(string name) where T : Component
    {
        var existing = Object.FindAnyObjectByType<T>();
        if (existing != null) return existing;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.AddComponent<T>();
    }

    // Plain UI container (RectTransform only)
    static GameObject UIChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform RT(GameObject go)
    {
        return go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetCenter(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // TMP (anchor-based, for GameCanvas HUD elements)
    static TextMeshProUGUI TMP(Transform parent, string name, string text, float size, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 sizeD)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeD;
        return tmp;
    }

    // TMP (centered, for PauseCanvas panel elements)
    static TextMeshProUGUI TMP(Transform parent, string name, string text, float size, Color color,
        Vector2 pos, Vector2 sizeD)
    {
        return TMP(parent, name, text, size, color,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            pos, sizeD);
    }

    static void Btn(Transform parent, string goName, string label, Vector2 pos)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.42f, 0.78f, 1f);
        SetCenter(go.GetComponent<RectTransform>(), pos, new Vector2(320, 58));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
    }

    static void SliderRow(Transform parent, string label, Vector2 pos)
    {
        var row = new GameObject("SliderRow_" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetCenter(row.GetComponent<RectTransform>(), pos, new Vector2(440, 56));

        // Label text
        var lbl = new GameObject("Text_" + label);
        lbl.transform.SetParent(row.transform, false);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        SetCenter(lbl.GetComponent<RectTransform>(), new Vector2(-150, 0), new Vector2(130, 44));

        // Slider container
        var sliderGO = new GameObject("Slider", typeof(RectTransform));
        sliderGO.transform.SetParent(row.transform, false);
        SetCenter(sliderGO.GetComponent<RectTransform>(), new Vector2(70, 0), new Vector2(300, 30));

        // Background
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(sliderGO.transform, false);
        bg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 1f);
        Stretch(bg.GetComponent<RectTransform>());

        // Fill Area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(7, 0); faRT.offsetMax = new Vector2(-7, 0);

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        fill.AddComponent<Image>().color = new Color(0.3f, 0.75f, 1f, 1f);
        Stretch(fill.GetComponent<RectTransform>());

        // Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGO.transform, false);
        var haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        var hRT = handle.GetComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = hRT.pivot = new Vector2(0.5f, 0.5f);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta = new Vector2(28, 28);

        // Wire Slider component
        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hRT;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
    }
}
