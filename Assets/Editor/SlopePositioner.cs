using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SlopePositioner
{
    private const float SlopeTopZ     = 54.68f;
    private const float SlopeTopY     = 25.66f;
    private const float SlopeGradient = 0.08969f;

    private static readonly string[] NonFlagSurfacePrefixes =
        { "Snowman", "TreeLarge", "TreeMedium", "TreeSmall", "Tree", "Arch", "Ledge", "Jump", "RockGroup" };

    [MenuItem("Ski Game/Fix Object Positions On Slope")]
    static void FixPositions()
    {
        int slopeMask  = 1 << 8;
        int fixedCount = 0;

        // --- Fix flags using PrefabUtility so nested instances are found ---
        var flagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flag.prefab");
        if (flagPrefab != null)
        {
            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (!go.scene.IsValid()) continue;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;

                var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                if (src != flagPrefab) continue;

                var t = go.transform;
                float surfaceY = RaycastSlopeY(t.position.x, t.position.z, slopeMask);

                Undo.RecordObject(t, "Fix Flag Position");
                Vector3 pos = t.position;
                pos.y = surfaceY;
                t.position = pos;
                fixedCount++;

                Debug.Log($"[SlopePositioner] Flag  z={pos.z:F1}  → worldY={pos.y:F2}");
            }
        }

        // --- Fix other surface objects (root-level only) ---
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.parent != null) continue;

            bool isOther = MatchesPrefixes(t.name, NonFlagSurfacePrefixes);
            bool isCloud = t.name.StartsWith("Cloud");

            if (!isOther && !isCloud) continue;

            float surfaceY = RaycastSlopeY(t.position.x, t.position.z, slopeMask);

            Undo.RecordObject(t, "Fix Slope Position");
            Vector3 pos = t.position;
            pos.y = isCloud ? surfaceY + 30f : surfaceY + 5f;
            t.position = pos;
            fixedCount++;

            Debug.Log($"[SlopePositioner] {t.name}  z={pos.z:F1}  → y={pos.y:F2}");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[SlopePositioner] Done — fixed {fixedCount} objects.");
    }

    static float RaycastSlopeY(float wx, float wz, int layerMask)
    {
        var origin = new Vector3(wx, 300f, wz);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 600f, layerMask))
            return hit.point.y;

        float y = SlopeTopY + SlopeGradient * (wz - SlopeTopZ);
        return Mathf.Min(y, SlopeTopY);
    }

    static bool MatchesPrefixes(string name, string[] prefixes)
    {
        foreach (var p in prefixes)
            if (name == p || name.StartsWith(p + " ("))
                return true;
        return false;
    }
}
