using UnityEngine;

public class ObstacleDetector : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (GetComponentInParent<SnowmanBreakable>() != null) return;

        if (collision.gameObject.CompareTag("Player") ||
            collision.gameObject.GetComponentInParent<PlayerControlle>() != null)
        {
            GameManager.Instance?.TriggerObstacleHit();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (GetComponentInParent<SnowmanBreakable>() != null) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerControlle>() != null)
        {
            GameManager.Instance?.TriggerObstacleHit();
        }
    }
}
