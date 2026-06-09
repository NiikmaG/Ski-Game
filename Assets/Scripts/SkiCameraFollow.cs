using UnityEngine;

public class SkiCameraFollow : MonoBehaviour
{
    [SerializeField] private float height = 7f;
    [SerializeField] private float frontDistance = 18f;
    [SerializeField] private float pitchAngle = 16f;

    private Transform player;

    void LateUpdate()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player ");
            if (p == null) return;
            player = p.transform;
        }

        transform.position = player.position + new Vector3(0f, height, -frontDistance);

        Vector3 dir = player.position - transform.position;
        if (dir != Vector3.zero)
        {
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(pitchAngle, yaw, 0f);
        }
    }
}
