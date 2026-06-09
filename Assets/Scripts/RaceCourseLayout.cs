using UnityEngine;

public static class RaceCourseLayout
{
    private const float FallbackCenterX = -20f;
    private const float FallbackMinTrackX = -110f;
    private const float FallbackMaxTrackX = 70f;

    public static float CenterX => TryGetSlopeBounds(out Bounds bounds) ? bounds.center.x : FallbackCenterX;
    public static float MinTrackX => TryGetSlopeBounds(out Bounds bounds) ? bounds.min.x + 8f : FallbackMinTrackX;
    public static float MaxTrackX => TryGetSlopeBounds(out Bounds bounds) ? bounds.max.x - 8f : FallbackMaxTrackX;

    public static Vector3 ClampToTrack(Vector3 position, float yOffset = 2.5f)
    {
        position.x = Mathf.Clamp(position.x, MinTrackX, MaxTrackX);

        if (TryGetSlopePoint(position.x, position.z, out Vector3 surfacePoint))
            position.y = surfacePoint.y + yOffset;

        return position;
    }

    private static bool TryGetSlopeBounds(out Bounds bounds)
    {
        GameObject slope = GameObject.Find("Slope");
        if (slope == null)
        {
            bounds = default;
            return false;
        }

        Renderer renderer = slope.GetComponentInChildren<Renderer>();
        Collider collider = slope.GetComponentInChildren<Collider>();

        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private static bool TryGetSlopePoint(float x, float z, out Vector3 point)
    {
        point = default;

        GameObject slope = GameObject.Find("Slope");
        if (slope == null || !TryGetSlopeBounds(out Bounds bounds))
            return false;

        Ray ray = new Ray(new Vector3(x, bounds.max.y + 150f, z), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, bounds.size.y + 300f);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == slope.transform || hit.collider.transform.IsChildOf(slope.transform))
            {
                point = hit.point;
                return true;
            }
        }

        return false;
    }
}
