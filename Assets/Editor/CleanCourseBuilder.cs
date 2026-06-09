using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CleanCourseBuilder
{
    private const string ReportPath = "work/CourseLayoutReport.json";
    private const float EdgeMargin = 12f;
    private const float CourseMarginZ = 55f;

    private static GameObject slope;
    private static Bounds slopeBounds;
    private static float startZ;
    private static float finishZ;
    private static float downhillSign;
    private static float centerX;
    private static float safeMinX;
    private static float safeMaxX;
    private static readonly List<string> reportLines = new();

    [MenuItem("Ski Game/Rebuild Course On Actual Slope")]
    public static void RebuildCourse()
    {
        reportLines.Clear();
        if (!InitializeSlope())
        {
            Debug.LogError("[CleanCourseBuilder] Could not find Slope with Renderer or Collider.");
            return;
        }

        Undo.SetCurrentGroupName("Rebuild Ski Course");
        int undoGroup = Undo.GetCurrentGroup();

        PlacePlayer();
        PlaceFlags();
        PlaceTrees();
        PlaceCourseObjects();
        PlaceClouds();
        RenderPreviews();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        WriteReport();

        Debug.Log("[CleanCourseBuilder] Rebuilt course using actual Slope bounds. Report: " + ReportPath);
    }

    [MenuItem("Ski Game/Write Course Layout Report")]
    public static void WriteOnlyReport()
    {
        reportLines.Clear();
        if (!InitializeSlope())
        {
            Debug.LogError("[CleanCourseBuilder] Could not find Slope with Renderer or Collider.");
            return;
        }

        AddObjectReport("Flag");
        AddObjectReport("Tree");
        AddObjectReport("TreeLarge");
        AddObjectReport("TreeMedium");
        AddObjectReport("TreeSmall");
        AddObjectReport("Snowman");
        AddObjectReport("RockGroup");
        AddObjectReport("Jump");
        AddObjectReport("Arch");
        AddObjectReport("Ledge");
        WriteReport();
    }

    private static bool InitializeSlope()
    {
        slope = GameObject.Find("Slope");
        if (slope == null)
            slope = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude)
                .FirstOrDefault(go => go.name.StartsWith("Slope"));

        if (slope == null) return false;

        Renderer slopeRenderer = slope.GetComponentInChildren<Renderer>();
        Collider slopeCollider = slope.GetComponentInChildren<Collider>();

        if (slopeRenderer != null)
            slopeBounds = slopeRenderer.bounds;
        else if (slopeCollider != null)
            slopeBounds = slopeCollider.bounds;
        else
            return false;

        centerX = slopeBounds.center.x;
        safeMinX = slopeBounds.min.x + EdgeMargin;
        safeMaxX = slopeBounds.max.x - EdgeMargin;

        float nearMinZ = slopeBounds.min.z + CourseMarginZ;
        float nearMaxZ = slopeBounds.max.z - CourseMarginZ;
        float yAtMinZ = SurfacePoint(centerX, nearMinZ).y;
        float yAtMaxZ = SurfacePoint(centerX, nearMaxZ).y;

        startZ = yAtMaxZ >= yAtMinZ ? nearMaxZ : nearMinZ;
        finishZ = yAtMaxZ >= yAtMinZ ? nearMinZ : nearMaxZ;
        downhillSign = Mathf.Sign(finishZ - startZ);

        AddReportLine("slope", $"\"center\":{Vec(slopeBounds.center)},\"size\":{Vec(slopeBounds.size)},\"startZ\":{Num(startZ)},\"finishZ\":{Num(finishZ)},\"startY\":{Num(SurfacePoint(centerX, startZ).y)},\"finishY\":{Num(SurfacePoint(centerX, finishZ).y)},\"safeMinX\":{Num(safeMinX)},\"safeMaxX\":{Num(safeMaxX)}");
        return true;
    }

    private static void PlacePlayer()
    {
        PlayerControlle player = Object.FindAnyObjectByType<PlayerControlle>();
        if (player == null) return;

        Transform t = player.transform;
        Vector3 pos = SurfacePoint(centerX, ZAt(0.035f));
        Undo.RecordObject(t, "Place Player");
        t.position = pos + Vector3.up * 2.8f;
        t.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, downhillSign), Vector3.up);
        AddReportLine("player", $"\"position\":{Vec(t.position)}");
    }

    private static void PlaceFlags()
    {
        List<Transform> flags = RootObjects("Flag").OrderBy(t => t.GetSiblingIndex()).ToList();
        int gateCount = Mathf.CeilToInt(flags.Count / 2f);
        if (gateCount == 0) return;

        float trackWidth = safeMaxX - safeMinX;
        float gateHalfWidth = Mathf.Clamp(trackWidth * 0.16f, 13f, 22f);
        float slalomWidth = Mathf.Clamp(trackWidth * 0.17f, 14f, 26f);

        for (int gate = 0; gate < gateCount; gate++)
        {
            float t = gateCount == 1 ? 0.5f : Mathf.Lerp(0.08f, 0.92f, gate / (float)(gateCount - 1));
            float centerOffset = Mathf.Sin(gate * 1.05f) * slalomWidth;
            float gateCenterX = Mathf.Clamp(centerX + centerOffset, safeMinX + gateHalfWidth, safeMaxX - gateHalfWidth);
            float z = ZAt(t);
            int leftIndex = gate * 2;
            int rightIndex = leftIndex + 1;

            if (rightIndex < flags.Count)
            {
                ScaleFlag(flags[leftIndex]);
                ScaleFlag(flags[rightIndex]);
                PlaceOnSlope(flags[leftIndex], gateCenterX - gateHalfWidth, z, flags[leftIndex].rotation, 0.05f, "Flag L");
                PlaceOnSlope(flags[rightIndex], gateCenterX + gateHalfWidth, z, flags[rightIndex].rotation, 0.05f, "Flag R");
            }
            else
            {
                ScaleFlag(flags[leftIndex]);
                PlaceOnSlope(flags[leftIndex], gateCenterX, z, flags[leftIndex].rotation, 0.05f, "Flag Finish");
            }
        }

        AddReportLine("flags", $"\"count\":{flags.Count},\"gateCount\":{gateCount}");
    }

    private static void ScaleFlag(Transform flag)
    {
        Undo.RecordObject(flag, "Scale Flag");
        flag.localScale = Vector3.one * 1.8f;
    }

    private static void PlaceTrees()
    {
        List<Transform> trees = RootObjects("Tree", "TreeLarge", "TreeMedium", "TreeSmall")
            .OrderBy(t => t.name)
            .ThenBy(t => t.GetSiblingIndex())
            .ToList();

        if (trees.Count == 0) return;

        float leftX = safeMinX + 3f;
        float rightX = safeMaxX - 3f;

        for (int i = 0; i < trees.Count; i++)
        {
            bool left = i % 2 == 0;
            float lane = (i / 2 + 1f) / (Mathf.Ceil(trees.Count / 2f) + 1f);
            float z = ZAt(Mathf.Lerp(0.08f, 0.94f, lane));
            float x = left ? leftX : rightX;
            x += Mathf.Sin(i * 2.31f) * 3f;

            Quaternion rotation = Quaternion.Euler(0f, (i * 47f) % 360f, 0f);
            PlacePivotOnSlope(trees[i], x, z, rotation, 0.15f, "Tree");
        }

        AddReportLine("trees", $"\"count\":{trees.Count},\"leftX\":{Num(leftX)},\"rightX\":{Num(rightX)}");
    }

    private static void PlaceCourseObjects()
    {
        PlaceArches();
        PlaceJumps();
        PlaceSnowmen();
        PlaceRocks();
        DisableLedges();
    }

    private static void PlaceArches()
    {
        List<Transform> arches = RootObjects("Arch").OrderBy(t => t.GetSiblingIndex()).ToList();
        float[] positions = { 0.12f, 0.38f, 0.66f, 0.9f };

        for (int i = 0; i < arches.Count; i++)
        {
            float t = positions[i % positions.Length];
            float x = CourseCenterX(t);
            float z = ZAt(t);
            PlaceOnSlope(arches[i], x, z, Quaternion.identity, 0.02f, "Arch");
        }

        AddReportLine("arches", $"\"count\":{arches.Count}");
    }

    private static void PlaceJumps()
    {
        List<Transform> jumps = RootObjects("Jump").OrderBy(t => t.GetSiblingIndex()).ToList();
        float[] positions = { 0.28f, 0.56f, 0.78f };
        Quaternion rotation = Quaternion.LookRotation(new Vector3(0f, 0f, downhillSign), Vector3.up);

        for (int i = 0; i < jumps.Count; i++)
        {
            float t = positions[i % positions.Length];
            float x = CourseCenterX(t);
            float z = ZAt(t);
            PlaceOnSlope(jumps[i], x, z, rotation, 0.01f, "Jump");
        }

        AddReportLine("jumps", $"\"count\":{jumps.Count}");
    }

    private static void PlaceSnowmen()
    {
        List<Transform> snowmen = RootObjects("Snowman").OrderBy(t => t.GetSiblingIndex()).ToList();
        float[] positions = { 0.22f, 0.45f, 0.63f, 0.82f };

        for (int i = 0; i < snowmen.Count; i++)
        {
            float t = positions[i % positions.Length];
            int side = i % 2 == 0 ? -1 : 1;
            float x = Mathf.Clamp(CourseCenterX(t) + side * 31f, safeMinX + 8f, safeMaxX - 8f);
            float z = ZAt(t);
            Vector3 lookTarget = SurfacePoint(CourseCenterX(t), z + downhillSign * 12f);
            Quaternion rotation = Quaternion.LookRotation((lookTarget - new Vector3(x, lookTarget.y, z)).normalized, Vector3.up);
            PlaceOnSlope(snowmen[i], x, z, rotation, 0.02f, "Snowman");
        }

        AddReportLine("snowmen", $"\"count\":{snowmen.Count}");
    }

    private static void PlaceRocks()
    {
        List<Transform> rocks = RootObjects("RockGroup").OrderBy(t => t.GetSiblingIndex()).ToList();
        float[] positions = { 0.18f, 0.34f, 0.52f, 0.7f, 0.86f };

        for (int i = 0; i < rocks.Count; i++)
        {
            float t = positions[i % positions.Length];
            int side = i % 2 == 0 ? 1 : -1;
            float x = Mathf.Clamp(CourseCenterX(t) + side * 12f, safeMinX + 16f, safeMaxX - 16f);
            float z = ZAt(t);
            PlaceOnSlope(rocks[i], x, z, Quaternion.Euler(0f, i * 32f, 0f), 0.02f, "RockGroup");
        }

        AddReportLine("rocks", $"\"count\":{rocks.Count}");
    }

    private static void DisableLedges()
    {
        List<Transform> ledges = RootObjects("Ledge").OrderBy(t => t.GetSiblingIndex()).ToList();

        foreach (Transform ledge in ledges)
        {
            Undo.RecordObject(ledge.gameObject, "Hide unused ledge");
            ledge.gameObject.SetActive(false);
        }

        AddReportLine("ledges", $"\"hiddenCount\":{ledges.Count}");
    }

    private static void PlaceClouds()
    {
        List<Transform> clouds = RootObjects("Cloud", "Clouds", "CloudLarge", "CloudMedium", "CloudSmall", "CloudDouble")
            .OrderBy(t => t.name)
            .ThenBy(t => t.GetSiblingIndex())
            .ToList();

        for (int i = 0; i < clouds.Count; i++)
        {
            bool left = i % 2 == 0;
            float t = (i + 1f) / (clouds.Count + 1f);
            float x = left ? slopeBounds.min.x - 70f : slopeBounds.max.x + 70f;
            float z = ZAt(Mathf.Lerp(0.08f, 0.92f, t));
            Vector3 surface = SurfacePoint(Mathf.Clamp(x, slopeBounds.min.x + 4f, slopeBounds.max.x - 4f), z);

            Undo.RecordObject(clouds[i], "Place Cloud");
            clouds[i].position = new Vector3(x, surface.y + 95f + Mathf.Sin(i * 1.7f) * 10f, z);
            clouds[i].rotation = Quaternion.identity;
            AddReportLine("cloud", $"\"name\":\"{Escape(clouds[i].name)}\",\"position\":{Vec(clouds[i].position)}");
        }

        AddReportLine("clouds", $"\"count\":{clouds.Count}");
    }

    private static void RenderPreviews()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDir = Path.Combine(projectRoot, "outputs");
        Directory.CreateDirectory(outputDir);

        RenderPreview(
            Path.Combine(outputDir, "course_top.png"),
            new Vector3(centerX, slopeBounds.max.y + 650f, (startZ + finishZ) * 0.5f),
            new Vector3(centerX, slopeBounds.center.y, (startZ + finishZ) * 0.5f),
            true);

        Vector3 downhill = new Vector3(0f, 0f, downhillSign);
        Vector3 start = SurfacePoint(centerX, startZ);
        RenderPreview(
            Path.Combine(outputDir, "course_downhill.png"),
            start - downhill * 90f + Vector3.up * 90f,
            SurfacePoint(centerX, ZAt(0.55f)) + Vector3.up * 20f,
            false);
    }

    private static void RenderPreview(string path, Vector3 cameraPosition, Vector3 lookAt, bool orthographic)
    {
        GameObject cameraObject = new GameObject("CoursePreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = new Color(0.35f, 0.38f, 0.42f);
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 2000f;
        camera.fieldOfView = 48f;
        camera.orthographic = orthographic;
        camera.orthographicSize = orthographic ? Mathf.Max(slopeBounds.size.x * 0.62f, Mathf.Abs(finishZ - startZ) * 0.52f) : 100f;
        camera.transform.position = cameraPosition;
        camera.transform.rotation = Quaternion.LookRotation(lookAt - cameraPosition, Vector3.up);

        RenderTexture renderTexture = new RenderTexture(1600, 1000, 24);
        camera.targetTexture = renderTexture;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        File.WriteAllBytes(path, image.EncodeToPNG());

        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(cameraObject);
    }

    private static IEnumerable<Transform> RootObjects(params string[] names)
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
            .Where(t => IsSceneRoot(t) && names.Any(name => t.name == name || t.name.StartsWith(name + " (")) && t.gameObject != slope);
    }

    private static bool IsSceneRoot(Transform transform)
    {
        return transform.parent == null || PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject);
    }

    private static float CourseCenterX(float t)
    {
        float width = safeMaxX - safeMinX;
        float offset = Mathf.Sin(t * Mathf.PI * 5f) * width * 0.12f;
        return Mathf.Clamp(centerX + offset, safeMinX + width * 0.2f, safeMaxX - width * 0.2f);
    }

    private static float ZAt(float t)
    {
        return Mathf.Lerp(startZ, finishZ, Mathf.Clamp01(t));
    }

    private static Vector3 SurfacePoint(float x, float z)
    {
        Physics.SyncTransforms();

        Ray ray = new Ray(new Vector3(x, slopeBounds.max.y + 200f, z), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, slopeBounds.size.y + 450f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == slope.transform || hit.collider.transform.IsChildOf(slope.transform))
                return hit.point;
        }

        return new Vector3(x, slopeBounds.center.y, z);
    }

    private static void PlaceOnSlope(Transform transform, float x, float z, Quaternion rotation, float extraLift, string label)
    {
        Undo.RecordObject(transform, "Place " + label);

        Vector3 surface = SurfacePoint(x, z);
        transform.rotation = rotation;
        transform.position = surface + Vector3.up * 2f;
        Physics.SyncTransforms();

        float baseDelta = 0f;
        if (TryGetObjectBounds(transform, out Bounds bounds))
        {
            float lift = surface.y - bounds.min.y + extraLift;
            transform.position += Vector3.up * lift;
        }
        else
        {
            transform.position = surface + Vector3.up * extraLift;
        }

        if (TryGetObjectBounds(transform, out Bounds finalBounds))
            baseDelta = finalBounds.min.y - surface.y;

        AddReportLine(label, $"\"name\":\"{Escape(transform.name)}\",\"position\":{Vec(transform.position)},\"surface\":{Vec(surface)},\"baseDelta\":{Num(baseDelta)},\"insideX\":{JsonBool(transform.position.x >= slopeBounds.min.x && transform.position.x <= slopeBounds.max.x)},\"insideZ\":{JsonBool(transform.position.z >= slopeBounds.min.z && transform.position.z <= slopeBounds.max.z)}");
    }

    private static void PlacePivotOnSlope(Transform transform, float x, float z, Quaternion rotation, float extraLift, string label)
    {
        Undo.RecordObject(transform, "Place " + label);

        Vector3 surface = SurfacePoint(x, z);
        transform.rotation = rotation;
        transform.position = surface + Vector3.up * extraLift;
        Physics.SyncTransforms();

        AddReportLine(label, $"\"name\":\"{Escape(transform.name)}\",\"position\":{Vec(transform.position)},\"surface\":{Vec(surface)},\"pivotDelta\":{Num(transform.position.y - surface.y)},\"insideX\":{JsonBool(transform.position.x >= slopeBounds.min.x && transform.position.x <= slopeBounds.max.x)},\"insideZ\":{JsonBool(transform.position.z >= slopeBounds.min.z && transform.position.z <= slopeBounds.max.z)}");
    }

    private static bool TryGetObjectBounds(Transform transform, out Bounds bounds)
    {
        Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled)
            .ToArray();

        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        Collider[] colliders = transform.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.enabled)
            .ToArray();

        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            return true;
        }

        bounds = default;
        return false;
    }

    private static void AddObjectReport(string objectName)
    {
        foreach (Transform transform in RootObjects(objectName))
            AddReportLine("object", $"\"name\":\"{Escape(transform.name)}\",\"position\":{Vec(transform.position)}");
    }

    private static void AddReportLine(string category, string jsonContent)
    {
        reportLines.Add($"{{\"category\":\"{category}\",{jsonContent}}}");
    }

    private static void WriteReport()
    {
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

        var builder = new StringBuilder();
        builder.AppendLine("[");
        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.Append("  ").Append(reportLines[i]);
            if (i < reportLines.Count - 1) builder.Append(",");
            builder.AppendLine();
        }
        builder.AppendLine("]");

        File.WriteAllText(fullPath, builder.ToString());
        AssetDatabase.Refresh();
    }

    private static string Vec(Vector3 vector)
    {
        return $"{{\"x\":{Num(vector.x)},\"y\":{Num(vector.y)},\"z\":{Num(vector.z)}}}";
    }

    private static string Num(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string JsonBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string Escape(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
