using UnityEngine;

// 온라인 대전에서 두 플레이어가 설 자리. 게임 씬에 하나 두고 points[0](호스트), points[1](참가자)를 지정한다.
// 각 기기는 자기 자리로 자기 XR 리그만 옮긴다. 오프라인 게임에서는 아무 것도 하지 않는다.
public class OnlineSpawnPoints : MonoBehaviour
{
    public static OnlineSpawnPoints Instance;

    [Tooltip("0번: 호스트 자리, 1번: 참가자 자리. 비어 있는 자리는 리그를 옮기지 않는다.")]
    public Transform[] points = new Transform[2];

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlaceRig(Transform rig, int slot)
    {
        if (rig == null || points == null || slot < 0 || slot >= points.Length || points[slot] == null) return;

        // 바닥 높이는 리그 기준을 그대로 두고, 위치와 바라보는 방향(수평)만 맞춘다.
        Transform point = points[slot];
        Vector3 forward = Vector3.ProjectOnPlane(point.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = rig.forward;
        rig.SetPositionAndRotation(point.position, Quaternion.LookRotation(forward, Vector3.up));
    }
}
