using System.Collections;
using UnityEngine;

// CPU 상대. 자기 차례가 되면 전용 공을 투구 위치에 놓고, 조준값(출발 위치·방향·속도)에
// 난이도만큼 오차를 섞어 굴린다. 결과를 미리 정하지 않으며, 점수는 플레이어와 똑같이
// 물리 결과(공 회수 → 핀 정지 → 쓰러진 핀 집계)로 GameManager가 기록한다.
public class CpuBowler : MonoBehaviour
{
    [Tooltip("CPU 전용 공. 플레이어가 잡지 못하도록 XRGrabInteractable 없이 Rigidbody + BowlingBall만 붙인다. returnPoint는 레인 밖 대기 위치로 둔다.")]
    public BowlingBall ball;
    [Tooltip("공을 놓을 위치. 파울 라인 앞 레인 중앙에 두고, 파란 축(forward)이 핀 쪽을 향하게 한다.")]
    public Transform throwPoint;
    [Tooltip("CPU 레인의 핀. GameManager도 CPU 투구를 이 핀으로 집계한다. 비우면 GameManager.pinSetter(플레이어 레인)를 같이 쓴다.")]
    public PinSetter pinSetter;

    [Header("Aim")]
    [Tooltip("1구 조준점: 맨 앞 핀에서 오른쪽(throwPoint 기준)으로 이만큼(m) 옮긴 곳(1-3번 핀 사이 포켓).")]
    public float pocketOffset = 0.06f;
    [Tooltip("투구 속도 범위(m/s).")]
    public float minSpeed = 6.5f;
    public float maxSpeed = 8.5f;

    [Header("Difficulty")]
    [Tooltip("방향 오차의 표준편차(도). 레인 길이 약 18m 기준 1도는 핀에서 약 0.3m 차이. 상급 0.3, 중급 0.7, 초급 1.5 정도.")]
    public float aimErrorDegrees = 0.7f;
    [Tooltip("출발 위치 좌우 오차의 표준편차(m).")]
    public float positionError = 0.03f;

    [Header("Timing")]
    [Tooltip("차례가 된 뒤 투구까지 기다리는 시간(초).")]
    public float thinkTime = 1.5f;

    private Rigidbody ballBody;
    private float ballRadius;

    void Awake()
    {
        if (ball == null) return;

        ballBody = ball.GetComponent<Rigidbody>();
        SphereCollider sphere = ball.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Vector3 scale = ball.transform.lossyScale;
            ballRadius = sphere.radius * Mathf.Max(scale.x, scale.y, scale.z);
        }
    }

    // GameManager가 CPU 차례이고 투구를 받을 수 있는 상태(Ready)일 때 호출한다.
    public void TakeTurn()
    {
        if (ball == null || throwPoint == null)
        {
            Debug.LogWarning("CpuBowler: ball 또는 throwPoint가 지정되지 않아 투구할 수 없습니다.", this);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ThrowRoutine());
    }

    // 새 게임 시작 등으로 대기 중인 투구를 취소한다.
    public void CancelTurn()
    {
        StopAllCoroutines();
    }

    private IEnumerator ThrowRoutine()
    {
        yield return new WaitForSeconds(thinkTime);

        Vector3 forward = Vector3.ProjectOnPlane(throwPoint.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 start = throwPoint.position + right * (Gaussian() * positionError);
        Vector3 dir = Vector3.ProjectOnPlane(GetAimPoint(forward, right) - start, Vector3.up).normalized;
        dir = Quaternion.AngleAxis(Gaussian() * aimErrorDegrees, Vector3.up) * dir;
        float speed = Random.Range(minSpeed, maxSpeed);

        ballBody.linearVelocity = Vector3.zero;
        ballBody.angularVelocity = Vector3.zero;
        ballBody.position = start;
        ball.transform.position = start;

        // 위치 이동이 물리에 반영된 뒤 던진다.
        yield return new WaitForFixedUpdate();

        if (GameManager.Instance == null || !GameManager.Instance.OnBallThrown(ball)) yield break;

        ballBody.linearVelocity = dir * speed;
        if (ballRadius > 0f)
        {
            // 미끄러지지 않고 굴러가는 회전. maxAngularVelocity에 걸리면 처음엔 미끄러지다 마찰로 구르게 된다.
            ballBody.angularVelocity = Vector3.Cross(Vector3.up, dir) * (speed / ballRadius);
        }
    }

    // 1구(10개 모두 서 있음): 맨 앞 핀 옆 포켓. 2구: 남은 핀들의 중심.
    private Vector3 GetAimPoint(Vector3 forward, Vector3 right)
    {
        PinSetter pins = pinSetter != null ? pinSetter : GameManager.Instance != null ? GameManager.Instance.pinSetter : null;
        if (pins == null) return throwPoint.position + forward * 18f;

        Vector3 sum = Vector3.zero;
        int count = 0;
        BowlingPin front = null;
        float frontDistance = float.MaxValue;

        foreach (BowlingPin pin in pins.pins)
        {
            if (pin == null || pin.IsKnockedDown()) continue;

            Vector3 position = pin.transform.position;
            sum += position;
            count++;

            float distance = Vector3.Dot(position - throwPoint.position, forward);
            if (distance < frontDistance)
            {
                frontDistance = distance;
                front = pin;
            }
        }

        if (count == 0) return pins.GetRackCenter();
        if (count == BowlingScoreCard.PinCount) return front.transform.position + right * pocketOffset;
        return sum / count;
    }

    // 표준정규분포 난수(Box-Muller).
    private static float Gaussian()
    {
        float u1 = Mathf.Max(1e-6f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
