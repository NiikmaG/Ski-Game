using UnityEngine;
using System;

public class GameAudio : MonoBehaviour
{
    private const string SkiingClipPath = "Audio/Skiing";
    private const string MusicClipPath = "Audio/Music";
    private const string MusicVolumeKey = "SkiGame_MusicVolume";
    private const string GameVolumeKey = "SkiGame_GameVolume";
    private const float DefaultMusicVolume = 0.16f;
    private const float DefaultGameVolume = 0.24f;

    private static GameAudio instance;

    private AudioSource musicSource;
    private AudioSource skiingSource;

    public static event Action OnGameVolumeChanged;
    public static float MusicVolume { get; private set; } = DefaultMusicVolume;
    public static float GameVolume { get; private set; } = DefaultGameVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null || FindAnyObjectByType<GameAudio>() != null)
            return;

        new GameObject("GameAudio").AddComponent<GameAudio>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumes();
        BuildSources();
    }

    void OnEnable()
    {
        GameManager.OnRaceStart += PlaySkiing;
        GameManager.OnRaceFinish += StopSkiing;
    }

    void OnDisable()
    {
        GameManager.OnRaceStart -= PlaySkiing;
        GameManager.OnRaceFinish -= StopSkiing;
    }

    void Start()
    {
        if (musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();

        if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Racing)
            PlaySkiing();
    }

    void Update()
    {
        bool isRacing = GameManager.Instance != null
                     && GameManager.Instance.State == GameManager.GameState.Racing
                     && Time.timeScale > 0.01f;

        if (isRacing && !skiingSource.isPlaying)
            PlaySkiing();
        else if (!isRacing && skiingSource.isPlaying)
            StopSkiing();
    }

    private void BuildSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = Resources.Load<AudioClip>(MusicClipPath);
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;

        skiingSource = gameObject.AddComponent<AudioSource>();
        skiingSource.clip = Resources.Load<AudioClip>(SkiingClipPath);
        skiingSource.loop = true;
        skiingSource.playOnAwake = false;
        skiingSource.spatialBlend = 0f;

        ApplyVolumes();
    }

    public static void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        instance?.ApplyVolumes();
    }

    public static void SetGameVolume(float volume)
    {
        GameVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(GameVolumeKey, GameVolume);
        PlayerPrefs.Save();
        instance?.ApplyVolumes();
        OnGameVolumeChanged?.Invoke();
    }

    private void LoadVolumes()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        GameVolume = PlayerPrefs.GetFloat(GameVolumeKey, DefaultGameVolume);
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = MusicVolume;

        if (skiingSource != null)
            skiingSource.volume = GameVolume;
    }

    private void PlaySkiing()
    {
        if (skiingSource.clip != null && !skiingSource.isPlaying)
            skiingSource.Play();
    }

    private void StopSkiing()
    {
        if (skiingSource.isPlaying)
            skiingSource.Stop();
    }
}
