using UnityEngine;

// 공 수거 구역. 핀 뒤 수거 콜라이더와 맵 아래 낙하 감지 콜라이더에 붙인다.
// 트리거에 BowlingBall이 들어오면 공을 복귀 위치로 보낸다.
[RequireComponent(typeof(Collider))]
public class BallReturnZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        BowlingBall ball = null;

        if (other.attachedRigidbody != null)
        {
            ball = other.attachedRigidbody.GetComponent<BowlingBall>();
        }

        if (ball == null)
        {
            ball = other.GetComponentInParent<BowlingBall>();
        }

        if (ball != null)
        {
            ball.ReturnToRack();
        }
    }
}
