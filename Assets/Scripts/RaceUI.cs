using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;

public class RaceUI : MonoBehaviour
{
    [Header("Scene Canvas (assigned by SkiGame ▸ Setup Scene)")]
    [SerializeField] private Canvas gameCanvas;

    private TextMeshProUGUI timeText;
    private TextMeshProUGUI bestTimeText;
    private TextMeshProUGUI leaderboardText;
    private TextMeshProUGUI stateText;
    private TextMeshProUGUI penaltyText;
    private TextMeshProUGUI finalTimeText;
    private TextMeshProUGUI penaltiesText;
    private TextMeshProUGUI recordText;
    private GameObject raceOverPanel;
    private AudioSource audioSource;
    private AudioClip startClip;
    private AudioClip penaltyClip;
    private AudioClip finishClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        CreateIfNeeded();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => CreateIfNeeded();
    }

    static void CreateIfNeeded()
    {
        if (FindAnyObjectByType<RaceUI>() == null)
            new GameObject("RaceUI").AddComponent<RaceUI>();
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        startClip = CreateToneClip("StartTone", 880f, 0.16f, 0.22f);
        penaltyClip = CreateToneClip("PenaltyTone", 180f, 0.22f, 0.28f);
        finishClip = CreateToneClip("FinishTone", 660f, 0.34f, 0.24f);
        ApplyGameVolume();

        if (gameCanvas != null)
            WireExistingCanvas();
        else
            BuildUI();
        UpdateBestTime();
        UpdateLeaderboard();
        SetStateText("Reach the START gate!");
    }

    void OnEnable()
    {
        GameManager.OnRaceStart += HandleRaceStart;
        GameManager.OnRaceFinish += HandleRaceFinish;
        GameManager.OnPenalty += HandlePenalty;
        GameManager.OnLeaderboardChanged += UpdateLeaderboard;
        GameAudio.OnGameVolumeChanged += ApplyGameVolume;
    }

    void OnDisable()
    {
        GameManager.OnRaceStart -= HandleRaceStart;
        GameManager.OnRaceFinish -= HandleRaceFinish;
        GameManager.OnPenalty -= HandlePenalty;
        GameManager.OnLeaderboardChanged -= UpdateLeaderboard;
        GameAudio.OnGameVolumeChanged -= ApplyGameVolume;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State == GameManager.GameState.Racing)
            timeText.text = FormatTime(GameManager.Instance.FinalTime);
    }

    void HandleRaceStart()
    {
        SetStateText("RACING!");
        penaltyText.text = "";
        raceOverPanel.SetActive(false);
        audioSource.PlayOneShot(startClip);
    }

    void HandleRaceFinish()
    {
        SetStateText("FINISHED!");

        float finalTime = GameManager.Instance.FinalTime;
        finalTimeText.text = "Time: " + FormatTime(finalTime);
        penaltiesText.text = $"Penalty: +{GameManager.Instance.TotalPenalty:0}s    Missed gates: {GameManager.Instance.MissedGates}";

        bool isRecord = finalTime <= GameManager.Instance.BestTime;
        recordText.gameObject.SetActive(isRecord);
        raceOverPanel.SetActive(true);
        audioSource.PlayOneShot(finishClip);

        UpdateBestTime();
        UpdateLeaderboard();
    }

    void HandlePenalty(float seconds)
    {
        penaltyText.text = $"+{seconds:0}s PENALTY!";
        audioSource.PlayOneShot(penaltyClip);
        CancelInvoke(nameof(ClearPenalty));
        Invoke(nameof(ClearPenalty), 2f);
    }

    void ClearPenalty()
    {
        penaltyText.text = "";
    }

    void SetStateText(string text)
    {
        if (stateText) stateText.text = text;
    }

    void ApplyGameVolume()
    {
        if (audioSource != null)
            audioSource.volume = GameAudio.GameVolume;
    }

    void UpdateBestTime()
    {
        if (GameManager.Instance == null || bestTimeText == null) return;

        float best = GameManager.Instance.BestTime;
        bestTimeText.text = best == float.MaxValue ? "Best: --:--" : "Best: " + FormatTime(best);
    }

    void UpdateLeaderboard()
    {
        if (GameManager.Instance == null || leaderboardText == null) return;

        var times = GameManager.Instance.Leaderboard;
        if (times.Count == 0)
        {
            leaderboardText.text = "Leaderboard\n--";
            return;
        }

        var builder = new StringBuilder("Leaderboard");
        for (int i = 0; i < times.Count; i++)
            builder.AppendLine().Append(i + 1).Append(". ").Append(FormatTime(times[i]));

        leaderboardText.text = builder.ToString();
    }

    public void RestartRace()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitSequence());
    }

    IEnumerator QuitSequence()
    {
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void WireExistingCanvas()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var t = gameCanvas.transform;
        timeText       = t.Find("T_Timer")?.GetComponent<TextMeshProUGUI>();
        bestTimeText   = t.Find("T_BestTime")?.GetComponent<TextMeshProUGUI>();
        leaderboardText = t.Find("T_Leaderboard")?.GetComponent<TextMeshProUGUI>();
        stateText      = t.Find("T_State")?.GetComponent<TextMeshProUGUI>();
        penaltyText    = t.Find("T_Penalty")?.GetComponent<TextMeshProUGUI>();

        var pt = t.Find("Panel_RaceOver");
        raceOverPanel = pt?.gameObject;
        if (pt != null)
        {
            finalTimeText  = pt.Find("T_FinalTime")?.GetComponent<TextMeshProUGUI>();
            penaltiesText  = pt.Find("T_Penalties")?.GetComponent<TextMeshProUGUI>();
            recordText     = pt.Find("T_Record")?.GetComponent<TextMeshProUGUI>();
            pt.Find("Btn_Restart")?.GetComponent<Button>()?.onClick.AddListener(RestartRace);
            pt.Find("Btn_NextLevel")?.GetComponent<Button>()?.onClick.AddListener(NextLevel);
            pt.Find("Btn_Quit")?.GetComponent<Button>()?.onClick.AddListener(QuitGame);
        }

        if (raceOverPanel != null) raceOverPanel.SetActive(false);
        if (recordText != null) recordText.gameObject.SetActive(false);
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        Transform canvasTransform = canvasGO.transform;

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        timeText = Txt(canvasTransform, "00:00.00", 52, Color.white,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(340, 65));

        bestTimeText = Txt(canvasTransform, "Best: --:--", 32, new Color(1f, 0.85f, 0f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(320, 50));

        leaderboardText = Txt(canvasTransform, "Leaderboard\n--", 28, new Color(0.92f, 0.96f, 1f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -82), new Vector2(360, 190));
        leaderboardText.alignment = TextAlignmentOptions.TopRight;

        stateText = Txt(canvasTransform, "", 36, Color.white,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -20), new Vector2(700, 55));

        penaltyText = Txt(canvasTransform, "", 54, Color.red,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 120), new Vector2(700, 80));

        raceOverPanel = Panel(canvasTransform, new Vector2(620, 500));
        raceOverPanel.SetActive(false);
        Transform panelTransform = raceOverPanel.transform;

        finalTimeText = Txt(panelTransform, "Time: 00:00.00", 52, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 138), new Vector2(600, 70));

        penaltiesText = Txt(panelTransform, "Penalty: +0s    Missed gates: 0", 28, new Color(0.9f, 0.95f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 88), new Vector2(600, 48));

        recordText = Txt(panelTransform, ">> NEW RECORD! <<", 38, Color.yellow,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 42), new Vector2(500, 55));
        recordText.gameObject.SetActive(false);

        Btn(panelTransform, "RESTART", new Vector2(0, -20), RestartRace);
        Btn(panelTransform, "NEXT LEVEL", new Vector2(0, -100), NextLevel);
        Btn(panelTransform, "QUIT", new Vector2(0, -180), QuitGame);
    }

    static TextMeshProUGUI Txt(Transform parent, string text, float size, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
    {
        var go = new GameObject("T");
        var textComponent = go.AddComponent<TextMeshProUGUI>();
        go.transform.SetParent(parent, false);

        var rectTransform = (RectTransform)go.transform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = sizeDelta;

        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;

        return textComponent;
    }

    static GameObject Panel(Transform parent, Vector2 size)
    {
        var go = new GameObject("Panel");
        var image = go.AddComponent<Image>();
        go.transform.SetParent(parent, false);

        var rectTransform = (RectTransform)go.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;

        image.color = new Color(0f, 0f, 0f, 0.82f);
        return go;
    }

    void Btn(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        var image = go.AddComponent<Image>();
        go.transform.SetParent(parent, false);

        var rectTransform = (RectTransform)go.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(320, 60);
        image.color = new Color(0.15f, 0.45f, 0.85f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var labelGO = new GameObject("L");
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelGO.transform.SetParent(go.transform, false);

        var labelRect = (RectTransform)labelGO.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        labelText.text = label;
        labelText.fontSize = 36;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
    }

    static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60);
        float remainder = seconds % 60;
        return $"{minutes:00}:{remainder.ToString("00.00", System.Globalization.CultureInfo.InvariantCulture)}";
    }

    static AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
