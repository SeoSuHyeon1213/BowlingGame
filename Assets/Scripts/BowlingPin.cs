using UnityEngine;

public class BowlingPin : MonoBehaviour
{
    public bool isFallen = false;

    [Tooltip("처음 세워진 방향에서 이 각도(도) 이상 기울면 쓰러진 것으로 본다.")]
    public float fallenAngle = 30f;

    [Tooltip("처음 위치보다 이만큼(m) 아래로 떨어지면(레인 밖으로 떨어짐) 쓰러진 것으로 본다.")]
    public float fallenDrop = 0.1f;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 localUpAtStart;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        // 모델 축 방향과 무관하게, 처음 세워졌을 때 월드 위쪽이던 로컬 축을 기준으로 기울기를 잰다.
        localUpAtStart = Quaternion.Inverse(startRotation) * Vector3.up;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Floor"))
        {
            if (!isFallen && SoundManager.instance != null)
            {
                SoundManager.instance.PlaySFX(SoundManager.instance.hitClip);
            }
            isFallen = true;
        }
    }

    // 점수 계산용 판정. 점수 자체는 GameManager가 투구가 끝난 뒤 한 번에 집계한다.
    public bool IsKnockedDown()
    {
        // isFallen(Floor 트리거)은 효과음 용도로만 쓴다. Floor 태그 오브젝트 배치에 따라
        // 서 있는 핀도 닿을 수 있으므로 판정은 기울기와 낙하로만 한다.
        if (!gameObject.activeInHierarchy) return true;

        Vector3 currentUp = transform.rotation * localUpAtStart;
        if (Vector3.Angle(currentUp, Vector3.up) > fallenAngle) return true;

        return transform.position.y < startPosition.y - fallenDrop;
    }

    public bool IsMoving(float speedThreshold)
    {
        if (rb == null || rb.isKinematic || !gameObject.activeInHierarchy) return false;
        return rb.linearVelocity.magnitude > speedThreshold || rb.angularVelocity.magnitude > speedThreshold;
    }

    // 쓰러진 핀 치우기(다음 투구에서 제외).
    public void Remove()
    {
        gameObject.SetActive(false);
    }

    // 처음 위치에 다시 세우기.
    public void ResetPin()
    {
        gameObject.SetActive(true);
        isFallen = false;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (rb != null)
        {
            rb.position = startPosition;
            rb.rotation = startRotation;
        }
        transform.SetPositionAndRotation(startPosition, startRotation);

        if (rb != null)
        {
            rb.Sleep();
        }
    }
}
