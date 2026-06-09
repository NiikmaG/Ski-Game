using UnityEngine;
using System.Collections;

public class PlayerKnockback : MonoBehaviour
{
    [SerializeField] private float knockbackDuration = 1.5f;
    [SerializeField] private float knockbackForce = 350f;
    [SerializeField] private AudioClip hitSound;

    public bool IsKnockedBack { get; private set; }

    private PlayerControlle playerControls;
    private Rigidbody rb;
    private AudioSource audioSource;
    private AudioClip fallbackHitSound;

    void Awake()
    {
        playerControls = GetComponent<PlayerControlle>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (hitSound == null)
            hitSound = Resources.Load<AudioClip>("Audio/Hit");
        if (hitSound == null)
            hitSound = CreateHitClip();
    }

    void OnEnable() => GameManager.OnObstacleHit += HandleObstacleHit;
    void OnDisable() => GameManager.OnObstacleHit -= HandleObstacleHit;

    private void HandleObstacleHit()
    {
        if (!IsKnockedBack) StartCoroutine(KnockbackRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        IsKnockedBack = true;
        if (playerControls != null) playerControls.enabled = false;

        AudioClip clip = hitSound != null ? hitSound : fallbackHitSound;
        if (clip != null)
            audioSource.PlayOneShot(clip, GameAudio.GameVolume);

        bool wasKinematic = rb.isKinematic;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 start = transform.position;
        float recoveryDistance = Mathf.Clamp(knockbackForce / 50f, 5f, 10f);
        Vector3 target = start - transform.forward * recoveryDistance;
        target.x = Mathf.Lerp(target.x, RaceCourseLayout.CenterX, 0.35f);
        target = RaceCourseLayout.ClampToTrack(target, 3f);

        float recoveryTime = Mathf.Min(0.55f, knockbackDuration);
        float elapsed = 0f;
        while (elapsed < recoveryTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / recoveryTime);
            rb.MovePosition(Vector3.Lerp(start, target, t));
            yield return null;
        }

        rb.MovePosition(target);
        rb.isKinematic = wasKinematic;

        yield return new WaitForSeconds(Mathf.Max(0f, knockbackDuration - recoveryTime));

        if (playerControls != null) playerControls.enabled = true;
        IsKnockedBack = false;
    }

    private AudioClip CreateHitClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.22f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-12f * t);
            float tone = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.35f;
            float noise = Random.Range(-1f, 1f) * 0.18f;
            samples[i] = (tone + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("GeneratedHit", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
