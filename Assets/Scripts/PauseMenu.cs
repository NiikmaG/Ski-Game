using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Scene Canvas (assigned by SkiGame ▸ Setup Scene)")]
    [SerializeField] private Canvas pauseCanvas;

    private GameObject menuRoot;
    private bool isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        CreateIfNeeded();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => CreateIfNeeded();
    }

    static void CreateIfNeeded()
    {
        if (FindAnyObjectByType<PauseMenu>() == null)
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Start()
    {
        if (pauseCanvas != null)
            WireExistingCanvas();
        else
            BuildUI();
        SetPaused(false);
    }

    private void WireExistingCanvas()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var root = pauseCanvas.transform.Find("PauseMenuRoot");
        if (root == null) { BuildUI(); return; }
        menuRoot = root.gameObject;

        var panel = root.Find("PausePanel");
        if (panel == null) { BuildUI(); return; }

        panel.Find("Button_RESUME")?.GetComponent<Button>()?.onClick.AddListener(() => SetPaused(false));
        panel.Find("Button_RESTART")?.GetComponent<Button>()?.onClick.AddListener(Restart);
        panel.Find("Button_QUIT")?.GetComponent<Button>()?.onClick.AddListener(Quit);

        var ms = panel.Find("SliderRow_MUSIC/Slider")?.GetComponent<Slider>();
        if (ms != null) { ms.value = GameAudio.MusicVolume; ms.onValueChanged.AddListener(GameAudio.SetMusicVolume); }

        var gs = panel.Find("SliderRow_GAME/Slider")?.GetComponent<Slider>();
        if (gs != null) { gs.value = GameAudio.GameVolume; gs.onValueChanged.AddListener(GameAudio.SetGameVolume); }
    }

    void Update()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            SetPaused(!isPaused);
    }

    void OnDestroy()
    {
        if (isPaused)
            Time.timeScale = 1f;
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (menuRoot != null)
            menuRoot.SetActive(paused);
    }

    private void Restart()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Quit()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("PauseCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        menuRoot = new GameObject("PauseMenuRoot");
        menuRoot.transform.SetParent(canvasGO.transform, false);

        var overlay = menuRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.55f);

        var overlayRect = (RectTransform)menuRoot.transform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject panel = Panel(menuRoot.transform, new Vector2(560f, 560f));
        Transform panelTransform = panel.transform;

        Txt(panelTransform, "PAUSED", 58f, Color.white, new Vector2(0f, 190f), new Vector2(500f, 72f));

        Button(panelTransform, "RESUME", new Vector2(0f, 112f), () => SetPaused(false));
        Button(panelTransform, "RESTART", new Vector2(0f, 34f), Restart);
        Button(panelTransform, "QUIT", new Vector2(0f, -44f), Quit);

        SliderRow(panelTransform, "MUSIC", new Vector2(0f, -146f), GameAudio.MusicVolume, GameAudio.SetMusicVolume);
        SliderRow(panelTransform, "GAME", new Vector2(0f, -222f), GameAudio.GameVolume, GameAudio.SetGameVolume);
    }

    private static GameObject Panel(Transform parent, Vector2 size)
    {
        var go = new GameObject("PausePanel");
        var image = go.AddComponent<Image>();
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        image.color = new Color(0.02f, 0.05f, 0.09f, 0.92f);
        return go;
    }

    private static TextMeshProUGUI Txt(Transform parent, string text, float size, Color color, Vector2 position, Vector2 area)
    {
        var go = new GameObject("Text_" + text);
        var label = go.AddComponent<TextMeshProUGUI>();
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = area;

        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private static void Button(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label);
        var image = go.AddComponent<Image>();
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 58f);
        image.color = new Color(0.16f, 0.42f, 0.78f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Txt(go.transform, label, 34f, Color.white, Vector2.zero, rect.sizeDelta);
    }

    private static void SliderRow(Transform parent, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var row = new GameObject("SliderRow_" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(440f, 56f);

        TextMeshProUGUI rowLabel = Txt(row.transform, label, 28f, Color.white, new Vector2(-150f, 0f), new Vector2(130f, 44f));
        rowLabel.alignment = TextAlignmentOptions.Left;

        Slider(row.transform, new Vector2(70f, 0f), value, onChange);
    }

    private static Slider Slider(Transform parent, Vector2 position, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 30f);

        var background = ImageChild(go.transform, "Background", new Color(0.12f, 0.16f, 0.22f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fillArea = RectChild(go.transform, "Fill Area", Vector2.zero, Vector2.one, new Vector2(7f, 0f), new Vector2(-7f, 0f));
        var fill = ImageChild(fillArea, "Fill", new Color(0.3f, 0.75f, 1f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var handleArea = RectChild(go.transform, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        var handle = ImageChild(handleArea, "Handle", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(14f, 14f));

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
        slider.targetGraphic = handle;
        slider.fillRect = (RectTransform)fill.transform;
        slider.handleRect = (RectTransform)handle.transform;
        slider.onValueChanged.AddListener(onChange);

        background.raycastTarget = false;
        fill.raycastTarget = false;
        return slider;
    }

    private static RectTransform RectChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static Image ImageChild(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = RectChild(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }
}
