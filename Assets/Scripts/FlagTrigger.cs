using UnityEngine;

public class FlagTrigger : MonoBehaviour
{
    private bool passed = false;
    private Renderer[] renderers;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        // Extend trigger inward toward gate center (slope center x≈80)
        bool isLeftFlag = transform.position.x < 80f;

        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(70f, 10f, 8f);
        box.center = new Vector3(isLeftFlag ? 35f : -35f, 3f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (passed || !other.CompareTag("Player")) return;
        passed = true;
        TurnGreen();
    }

    private void TurnGreen()
    {
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty(BaseColorID))
                    mat.SetColor(BaseColorID, Color.green);
                else
                    mat.color = Color.green;

                if (mat.HasProperty(EmissionColorID))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(EmissionColorID, Color.green * 2f);
                }
            }
        }
    }
}
