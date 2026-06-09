using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class FlagManager
{
    private const float GateZTolerance = 30f;
    private const float GateSidePadding = 34f;
    private const float MinimumGateWidth = 90f;
    private const float SingleFlagGateWidth = 130f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Setup()
    {
        Initialize();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Initialize();
    }

    static void Initialize()
    {
        ClearGeneratedGateTriggers();

        var flagLikeObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
            .Where(t => t.name.StartsWith("Flag"))
            .ToList();

        var allFlags = flagLikeObjects
            .Where(IsRaceFlagObject)
            .OrderByDescending(t => t.position.z)
            .ToList();

        if (allFlags.Count == 0) return;

        var gates = BuildGateRows(allFlags);
        var raceGates = gates
            .Where(gate => gate.Count >= 2)
            .OrderByDescending(GetAverageZ)
            .ToList();

        if (raceGates.Count == 0)
            raceGates = gates.OrderByDescending(GetAverageZ).ToList();

        RemoveRaceFlagComponentsFromIgnoredFlags(flagLikeObjects, raceGates);

        for (int g = 0; g < raceGates.Count; g++)
        {
            var type = g == 0 ? RaceFlag.FlagType.Start
                     : g == raceGates.Count - 1 ? RaceFlag.FlagType.Finish
                     : RaceFlag.FlagType.Gate;

            var raceFlags = new List<RaceFlag>();
            foreach (var t in raceGates[g])
            {
                var raceFlag = t.GetComponent<RaceFlag>();
                if (raceFlag == null)
                    raceFlag = t.gameObject.AddComponent<RaceFlag>();

                raceFlag.flagType = type;
                raceFlags.Add(raceFlag);
            }

            CreateGateTrigger(g, type, raceGates[g], raceFlags);
        }
    }

    private static void ClearGeneratedGateTriggers()
    {
        foreach (Transform trigger in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!trigger.name.StartsWith("GateTrigger_")) continue;
            Object.Destroy(trigger.gameObject);
        }
    }

    private static void RemoveRaceFlagComponentsFromIgnoredFlags(List<Transform> allFlags, List<List<Transform>> raceGates)
    {
        var raceFlagTransforms = raceGates.SelectMany(gate => gate).ToHashSet();

        foreach (Transform flag in allFlags)
        {
            if (raceFlagTransforms.Contains(flag)) continue;

            var raceFlag = flag.GetComponent<RaceFlag>();
            if (raceFlag != null)
                Object.Destroy(raceFlag);
        }
    }

    private static bool IsRaceFlagObject(Transform transform)
    {
        if (!transform.name.StartsWith("Flag"))
            return false;

        // Ignore hierarchy folders such as a parent object named "Flag".
        foreach (Transform child in transform)
            if (child.name.StartsWith("Flag"))
                return false;

        return transform.GetComponentInChildren<Renderer>(true) != null;
    }

    private static List<List<Transform>> BuildGateRows(List<Transform> flags)
    {
        var gates = new List<List<Transform>>();

        foreach (Transform flag in flags)
        {
            if (gates.Count == 0)
            {
                gates.Add(new List<Transform> { flag });
                continue;
            }

            List<Transform> currentGate = gates[^1];
            float currentGateZ = currentGate.Average(t => t.position.z);

            if (Mathf.Abs(flag.position.z - currentGateZ) <= GateZTolerance)
                currentGate.Add(flag);
            else
                gates.Add(new List<Transform> { flag });
        }

        return gates;
    }

    private static float GetAverageZ(List<Transform> gate)
    {
        return gate.Average(flag => flag.position.z);
    }

    private static void CreateGateTrigger(int index, RaceFlag.FlagType type, List<Transform> gate, List<RaceFlag> raceFlags)
    {
        Vector3 center = Vector3.zero;
        foreach (Transform flag in gate)
            center += flag.position;
        center /= gate.Count;
        center.y += 5f;

        float minX = gate.Min(flag => flag.position.x);
        float maxX = gate.Max(flag => flag.position.x);
        float gateCenterX = (minX + maxX) * 0.5f;
        float width = gate.Count == 1
            ? SingleFlagGateWidth
            : Mathf.Max(MinimumGateWidth, (maxX - minX) + GateSidePadding * 2f);

        minX = gateCenterX - width * 0.5f;
        maxX = gateCenterX + width * 0.5f;

        var triggerObject = new GameObject($"GateTrigger_{type}_{index:00}");
        triggerObject.transform.position = new Vector3(gateCenterX, center.y, center.z);

        var box = triggerObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(width, 24f, 12f);

        triggerObject.AddComponent<RaceGateTrigger>().Initialize(raceFlags.ToArray(), center.z, minX, maxX);
    }
}
