using UnityEngine;

public class RaceFlag : MonoBehaviour
{
    public enum FlagType { Start, Gate, Finish }

    [SerializeField] public FlagType flagType = FlagType.Gate;

    public bool Passed { get; private set; }

    private Renderer[] renderers;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        EnsureRenderers();

        if (flagType == FlagType.Gate)
            GameManager.Instance?.RegisterGateFlag(this);

        float gateCenterX = FindGateCenterX();
        bool isLeftFlag = transform.position.x < gateCenterX;
        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(70f, 10f, 8f);
        box.center = new Vector3(isLeftFlag ? 35f : -35f, 3f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerControlle>() == null) return;
        Pass();
    }

    public void Pass()
    {
        EnsureRenderers();
        if (Passed)
        {
            RefreshPassedVisual();
            return;
        }

        var manager = GameManager.Instance;
        if (flagType == FlagType.Finish && manager != null && manager.State == GameManager.GameState.Finished)
        {
            Passed = true;
            SetColor(Color.green);
            return;
        }

        if (flagType != FlagType.Start && manager != null && manager.State != GameManager.GameState.Racing)
            return;

        Passed = true;

        switch (flagType)
        {
            case FlagType.Start:
                manager?.StartRace();
                SetColor(Color.yellow);
                break;
            case FlagType.Gate:
                SetColor(Color.green);
                break;
            case FlagType.Finish:
                SetColor(Color.green);
                manager?.FinishRace();
                break;
        }
    }

    public void RefreshPassedVisual()
    {
        if (!Passed) return;
        EnsureRenderers();

        switch (flagType)
        {
            case FlagType.Start:
                SetColor(Color.yellow);
                break;
            case FlagType.Gate:
            case FlagType.Finish:
                SetColor(Color.green);
                break;
        }
    }

    private void EnsureRenderers()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void SetColor(Color color)
    {
        EnsureRenderers();

        foreach (var r in renderers)
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty(BaseColorID))
                    mat.SetColor(BaseColorID, color);
                else
                    mat.color = color;

                if (mat.HasProperty(EmissionColorID))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(EmissionColorID, color * 2f);
                }
            }
    }

    private float FindGateCenterX()
    {
        float centerX = transform.position.x;
        int count = 1;

        foreach (Transform flag in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (flag == transform || !IsSceneFlag(flag)) continue;
            if (Mathf.Abs(flag.position.z - transform.position.z) > 4f) continue;

            centerX += flag.position.x;
            count++;
        }

        return centerX / count;
    }

    private bool IsSceneFlag(Transform flag)
    {
        if (!flag.name.StartsWith("Flag"))
            return false;

        foreach (Transform child in flag)
            if (child.name.StartsWith("Flag"))
                return false;

        return flag.GetComponentInChildren<Renderer>(true) != null;
    }
}
