using System.Collections;
using UnityEngine;

public class SnowmanBreakable : MonoBehaviour
{
    [SerializeField] private int chunkCount = 8;
    [SerializeField] private float burstForce = 5f;
    [SerializeField] private float shrinkDuration = 0.45f;

    private bool broken;
    private Vector3 originalScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Setup()
    {
        AddToSceneSnowmen();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => AddToSceneSnowmen();
    }

    static void AddToSceneSnowmen()
    {
        foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!transform.name.StartsWith("Snowman")) continue;
            if (transform.GetComponent<SnowmanBreakable>() == null)
                transform.gameObject.AddComponent<SnowmanBreakable>();
        }
    }

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsPlayer(collision.gameObject))
            Break();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.gameObject))
            Break();
    }

    public void Break()
    {
        if (broken) return;
        broken = true;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;

        Bounds bounds = GetVisualBounds();
        SpawnChunks(bounds);

        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void SpawnChunks(Bounds bounds)
    {
        Material sourceMaterial = GetComponentInChildren<Renderer>()?.material;

        for (int i = 0; i < chunkCount; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chunk.name = "SnowmanChunk";
            chunk.transform.position = bounds.center + Random.insideUnitSphere * Mathf.Max(0.4f, bounds.extents.magnitude * 0.25f);
            chunk.transform.localScale = Vector3.one * Random.Range(0.28f, 0.62f);

            if (sourceMaterial != null)
                chunk.GetComponent<Renderer>().material = sourceMaterial;

            Rigidbody rb = chunk.AddComponent<Rigidbody>();
            rb.mass = 0.2f;

            Vector3 direction = (chunk.transform.position - bounds.center + Vector3.up * 0.6f).normalized;
            rb.AddForce(direction * Random.Range(burstForce * 0.65f, burstForce * 1.25f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * burstForce, ForceMode.Impulse);

            Destroy(chunk, 2.4f);
        }
    }

    private Bounds GetVisualBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(transform.position, Vector3.one * 2f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private bool IsPlayer(GameObject other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerControlle>() != null;
    }
}
