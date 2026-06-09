using UnityEngine;

public class RaceGateTrigger : MonoBehaviour
{
    [SerializeField] private float crossingCatchDistance = 4f;
    [SerializeField] private float horizontalMargin = 6f;

    private RaceFlag[] flags;
    private Transform player;
    private float gateZ;
    private float minX;
    private float maxX;
    private float previousPlayerZ;
    private bool hasPreviousPlayerZ;

    public void Initialize(RaceFlag[] gateFlags)
    {
        Initialize(gateFlags, transform.position.z, transform.position.x - 65f, transform.position.x + 65f);
    }

    public void Initialize(RaceFlag[] gateFlags, float gateCenterZ, float gateMinX, float gateMaxX)
    {
        flags = gateFlags;
        gateZ = gateCenterZ;
        minX = gateMinX;
        maxX = gateMaxX;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerControlle>() == null)
            return;

        PassGate();
    }

    void Update()
    {
        if (AllFlagsPassed()) return;

        if (player == null)
            player = FindPlayer();

        if (player == null) return;

        float currentZ = player.position.z;
        bool isNearGateLine = Mathf.Abs(currentZ - gateZ) <= crossingCatchDistance;
        bool crossedGateLine = hasPreviousPlayerZ && IsBetween(gateZ, previousPlayerZ, currentZ);
        bool isInsideGateWidth = player.position.x >= minX - horizontalMargin
                              && player.position.x <= maxX + horizontalMargin;

        if ((isNearGateLine || crossedGateLine) && isInsideGateWidth)
            PassGate();

        previousPlayerZ = currentZ;
        hasPreviousPlayerZ = true;
    }

    private void PassGate()
    {
        foreach (RaceFlag flag in flags)
            if (flag != null)
            {
                flag.Pass();
                flag.RefreshPassedVisual();
            }
    }

    private bool AllFlagsPassed()
    {
        if (flags == null || flags.Length == 0)
            return true;

        foreach (RaceFlag flag in flags)
            if (flag != null && !flag.Passed)
                return false;

        return true;
    }

    private Transform FindPlayer()
    {
        var controller = FindAnyObjectByType<PlayerControlle>();
        if (controller != null)
            return controller.transform;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        return taggedPlayer != null ? taggedPlayer.transform : null;
    }

    private bool IsBetween(float value, float a, float b)
    {
        return value >= Mathf.Min(a, b) && value <= Mathf.Max(a, b);
    }
}
