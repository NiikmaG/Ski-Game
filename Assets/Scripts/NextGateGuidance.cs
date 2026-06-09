using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NextGateGuidance : MonoBehaviour
{
    private TextMeshProUGUI markerText;
    private Camera mainCamera;
    private readonly List<RaceFlag> flags = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        CreateIfNeeded();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => CreateIfNeeded();
    }

    static void CreateIfNeeded()
    {
        if (FindAnyObjectByType<NextGateGuidance>() == null)
            new GameObject("NextGateGuidance").AddComponent<NextGateGuidance>();
    }

    void Start()
    {
        mainCamera = Camera.main;
        BuildUI();
        InvokeRepeating(nameof(RefreshFlags), 0.2f, 0.5f);
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || markerText == null) return;

        RaceFlag nextFlag = GetNextFlag();
        if (nextFlag == null)
        {
            markerText.gameObject.SetActive(false);
            return;
        }

        Vector3 gateCenter = GetGateCenter(nextFlag);
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(gateCenter + Vector3.up * 7f);

        if (screenPoint.z <= 0f)
        {
            markerText.gameObject.SetActive(false);
            return;
        }

        markerText.gameObject.SetActive(true);
        markerText.text = nextFlag.flagType == RaceFlag.FlagType.Finish ? "FINISH\nv" : "NEXT GATE\nv";
        markerText.rectTransform.position = screenPoint;
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("GateGuideCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var textGO = new GameObject("NextGateMarker");
        markerText = textGO.AddComponent<TextMeshProUGUI>();
        textGO.transform.SetParent(canvasGO.transform, false);

        markerText.rectTransform.sizeDelta = new Vector2(260f, 90f);
        markerText.alignment = TextAlignmentOptions.Center;
        markerText.fontSize = 30f;
        markerText.color = new Color(1f, 0.92f, 0.2f);
        markerText.outlineWidth = 0.25f;
        markerText.outlineColor = Color.black;
        markerText.gameObject.SetActive(false);
    }

    private void RefreshFlags()
    {
        flags.Clear();
        flags.AddRange(FindObjectsByType<RaceFlag>(FindObjectsInactive.Exclude)
            .OrderByDescending(flag => flag.transform.position.z));
    }

    private RaceFlag GetNextFlag()
    {
        return flags.FirstOrDefault(flag => !flag.Passed);
    }

    private Vector3 GetGateCenter(RaceFlag target)
    {
        var gateFlags = flags
            .Where(flag => Mathf.Abs(flag.transform.position.z - target.transform.position.z) < 4f)
            .ToList();

        if (gateFlags.Count == 0)
            return target.transform.position;

        Vector3 center = Vector3.zero;
        foreach (RaceFlag flag in gateFlags)
            center += flag.transform.position;

        return center / gateFlags.Count;
    }
}
