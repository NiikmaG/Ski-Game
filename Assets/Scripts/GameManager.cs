using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Idle, Racing, Finished }

    public static event Action OnRaceStart;
    public static event Action OnRaceFinish;
    public static event Action OnObstacleHit;
    public static event Action<float> OnPenalty;
    public static event Action OnLeaderboardChanged;

    [SerializeField] private float penaltySeconds = 5f;
    [SerializeField] private float obstacleHitCooldown = 1.2f;
    [SerializeField] private int maxLeaderboardEntries = 5;

    public GameState State { get; private set; } = GameState.Idle;
    public float CurrentTime { get; private set; }
    public float BestTime { get; private set; }
    public float TotalPenalty { get; private set; }
    public int MissedGates { get; private set; }
    public IReadOnlyList<float> Leaderboard => leaderboard;

    private const string BestTimeKey = "SkiGame_BestTime";
    private const string LeaderboardKey = "SkiGame_Leaderboard";
    private readonly List<RaceFlag> gateFlags = new();
    private readonly List<float> leaderboard = new();
    private float lastObstacleHitTime = -99f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return; // already placed in scene
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BestTime = PlayerPrefs.GetFloat(BestTimeKey, float.MaxValue);
        LoadLeaderboard();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        State = GameState.Idle;
        CurrentTime = 0f;
        TotalPenalty = 0f;
        MissedGates = 0;
        gateFlags.Clear();
        lastObstacleHitTime = -99f;
    }

    void Update()
    {
        if (State == GameState.Racing)
            CurrentTime += Time.deltaTime;
    }

    public void RegisterGateFlag(RaceFlag flag)
    {
        gateFlags.Add(flag);
    }

    public void StartRace()
    {
        if (State != GameState.Idle) return;
        State = GameState.Racing;
        CurrentTime = 0f;
        TotalPenalty = 0f;
        MissedGates = 0;
        OnRaceStart?.Invoke();
    }

    public void FinishRace()
    {
        if (State != GameState.Racing) return;
        State = GameState.Finished;

        foreach (var flag in gateFlags)
        {
            if (!flag.Passed)
            {
                MissedGates++;
                TotalPenalty += penaltySeconds;
            }
        }

        float finalTime = CurrentTime + TotalPenalty;
        if (BestTime == float.MaxValue || finalTime < BestTime)
        {
            BestTime = finalTime;
            PlayerPrefs.SetFloat(BestTimeKey, BestTime);
        }

        SaveLeaderboardEntry(finalTime);
        PlayerPrefs.Save();
        OnRaceFinish?.Invoke();
    }

    public void AddPenalty()
    {
        if (State != GameState.Racing) return;
        TotalPenalty += penaltySeconds;
        OnPenalty?.Invoke(penaltySeconds);
    }

    public void TriggerObstacleHit()
    {
        if (State != GameState.Racing) return;
        if (Time.time - lastObstacleHitTime < obstacleHitCooldown) return;

        lastObstacleHitTime = Time.time;
        AddPenalty();
        OnObstacleHit?.Invoke();
    }

    public float FinalTime => CurrentTime + TotalPenalty;

    private void LoadLeaderboard()
    {
        leaderboard.Clear();
        string saved = PlayerPrefs.GetString(LeaderboardKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            foreach (string part in saved.Split('|'))
            {
                if (float.TryParse(part, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float time))
                {
                    leaderboard.Add(time);
                }
            }
        }

        if (leaderboard.Count == 0 && BestTime != float.MaxValue)
            leaderboard.Add(BestTime);

        SortAndTrimLeaderboard();
    }

    private void SaveLeaderboardEntry(float finalTime)
    {
        leaderboard.Add(finalTime);
        SortAndTrimLeaderboard();

        string saved = string.Join("|", leaderboard.Select(t =>
            t.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
        PlayerPrefs.SetString(LeaderboardKey, saved);
        OnLeaderboardChanged?.Invoke();
    }

    private void SortAndTrimLeaderboard()
    {
        leaderboard.Sort();
        while (leaderboard.Count > maxLeaderboardEntries)
            leaderboard.RemoveAt(leaderboard.Count - 1);
    }
}
