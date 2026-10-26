using System;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

// 온라인 플레이어 아바타(NetworkManager의 Player Prefab). 소유자는 자기 XR 리그의 머리·양손 위치를
// 일정 간격으로 보내고, 상대 기기는 받은 위치로 머리·손 모델을 부드럽게 옮긴다.
// 자기 아바타는 화면에 그리지 않는다(실제 손 모델과 겹치므로).
public class NetworkedXRAvatar : NetworkBehaviour
{
    [Header("Visuals")]
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Sync")]
    [Tooltip("초당 위치 전송 횟수.")]
    public float sendRate = 20f;
    [Tooltip("상대 아바타가 받은 위치를 따라가는 속도. 클수록 빠르게 붙는다.")]
    public float smoothing = 15f;

    [Header("Local Rig")]
    [Tooltip("XR Origin 아래에서 왼손으로 쓸 오브젝트 이름. 앞쪽부터 켜져 있는 것을 쓴다.")]
    public string[] leftSourceNames = { "Left Controller", "Left Hand" };
    public string[] rightSourceNames = { "Right Controller", "Right Hand" };

    private readonly NetworkVariable<AvatarPose> pose = new NetworkVariable<AvatarPose>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private XROrigin origin;
    private float nextSendTime;
    private bool hasPose;

    // 게임 씬의 자리 번호. 호스트가 0번, 참가자가 1번.
    public int Slot => OwnerClientId == NetworkManager.ServerClientId ? 0 : 1;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            SceneManager.sceneLoaded += OnSceneLoaded;
            PlaceLocalRig();
        }
        else
        {
            pose.OnValueChanged += OnPoseChanged;
            if (pose.Value.IsSet) OnPoseChanged(default, pose.Value);
            SetHandVisible(leftHand, false);
            SetHandVisible(rightHand, false);
        }
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        pose.OnValueChanged -= OnPoseChanged;
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            if (Time.unscaledTime >= nextSendTime && TrySamplePose(out AvatarPose sample))
            {
                nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
                pose.Value = sample;
            }
            return;
        }

        if (!hasPose) return;

        AvatarPose p = pose.Value;
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        Follow(head, p.headPosition, p.headRotation, t);
        Follow(leftHand, p.leftPosition, p.leftRotation, t);
        Follow(rightHand, p.rightPosition, p.rightRotation, t);
    }

    private void OnPoseChanged(AvatarPose previous, AvatarPose current)
    {
        // 첫 수신 때는 원점에서 날아오지 않게 바로 그 자리에 둔다.
        if (!hasPose)
        {
            hasPose = true;
            Follow(head, current.headPosition, current.headRotation, 1f);
            Follow(leftHand, current.leftPosition, current.leftRotation, 1f);
            Follow(rightHand, current.rightPosition, current.rightRotation, 1f);
        }

        SetHandVisible(leftHand, current.leftTracked);
        SetHandVisible(rightHand, current.rightTracked);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        origin = null;
        PlaceLocalRig();
    }

    // 게임 씬에 OnlineSpawnPoints가 있으면 내 자리로 XR 리그를 옮긴다(두 사람이 겹치지 않게).
    private void PlaceLocalRig()
    {
        if (OnlineSpawnPoints.Instance == null) return;
        XROrigin rig = FindOrigin();
        if (rig != null) OnlineSpawnPoints.Instance.PlaceRig(rig.transform, Slot);
    }

    private bool TrySamplePose(out AvatarPose sample)
    {
        sample = default;
        XROrigin rig = FindOrigin();
        if (rig == null || rig.Camera == null) return false;

        Transform cam = rig.Camera.transform;
        Transform left = FindActiveChild(rig.transform, leftSourceNames);
        Transform right = FindActiveChild(rig.transform, rightSourceNames);

        sample.headPosition = cam.position;
        sample.headRotation = cam.rotation;
        sample.leftTracked = left != null;
        sample.rightTracked = right != null;
        if (left != null)
        {
            sample.leftPosition = left.position;
            sample.leftRotation = left.rotation;
        }
        if (right != null)
        {
            sample.rightPosition = right.position;
            sample.rightRotation = right.rotation;
        }
        return true;
    }

    private XROrigin FindOrigin()
    {
        if (origin == null) origin = FindAnyObjectByType<XROrigin>();
        return origin;
    }

    private static Transform FindActiveChild(Transform root, string[] names)
    {
        foreach (string name in names)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(false))
            {
                if (child.name == name) return child;
            }
        }
        return null;
    }

    private static void Follow(Transform target, Vector3 position, Quaternion rotation, float t)
    {
        if (target == null) return;
        target.SetPositionAndRotation(
            Vector3.Lerp(target.position, position, t),
            Quaternion.Slerp(target.rotation, rotation, t));
    }

    private static void SetHandVisible(Transform hand, bool visible)
    {
        if (hand != null && hand.gameObject.activeSelf != visible) hand.gameObject.SetActive(visible);
    }

    public struct AvatarPose : INetworkSerializable, IEquatable<AvatarPose>
    {
        public Vector3 headPosition;
        public Quaternion headRotation;
        public Vector3 leftPosition;
        public Quaternion leftRotation;
        public Vector3 rightPosition;
        public Quaternion rightRotation;
        public bool leftTracked;
        public bool rightTracked;

        // 아직 한 번도 보내지 않은 기본값(회전이 모두 0)인지 구분한다.
        public bool IsSet => headRotation.x != 0f || headRotation.y != 0f || headRotation.z != 0f || headRotation.w != 0f;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref headPosition);
            serializer.SerializeValue(ref headRotation);
            serializer.SerializeValue(ref leftPosition);
            serializer.SerializeValue(ref leftRotation);
            serializer.SerializeValue(ref rightPosition);
            serializer.SerializeValue(ref rightRotation);
            serializer.SerializeValue(ref leftTracked);
            serializer.SerializeValue(ref rightTracked);
        }

        public bool Equals(AvatarPose other)
        {
            return headPosition == other.headPosition && headRotation == other.headRotation
                && leftPosition == other.leftPosition && leftRotation == other.leftRotation
                && rightPosition == other.rightPosition && rightRotation == other.rightRotation
                && leftTracked == other.leftTracked && rightTracked == other.rightTracked;
        }
    }
}
