using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

[RequireComponent(typeof(CinemachineCamera))]
public class CinemachineCameraSetup : MonoBehaviour
{
    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var cam = GetComponent<CinemachineCamera>();
        cam.Follow = player.transform;
        cam.LookAt = player.transform;

        var follow = GetComponent<CinemachineFollow>();
        if (follow != null)
        {
            // 10 units above, 15 units behind in world space (player faces -Z)
            follow.FollowOffset = new Vector3(0, 10, 15);
            var s = follow.TrackerSettings;
            s.BindingMode = BindingMode.WorldSpace;
            s.PositionDamping = new Vector3(2f, 1f, 2f);
            follow.TrackerSettings = s;
        }
    }
}
