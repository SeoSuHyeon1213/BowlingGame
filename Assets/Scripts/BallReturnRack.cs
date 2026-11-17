using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// 볼 스탠드의 소켓 목록. 공이 복귀할 때(BowlingBall.ReturnToRack) 빈 소켓을 골라 그쪽으로 공을 보낸다.
// sockets 순서가 우선순위다(앞이 먼저 채워짐). 가는 길이 비어 있으면 살짝 굴려 보내고,
// 앞쪽 소켓에 공이 있어 길이 막혀 있으면 그 공을 넘어가도록 작은 포물선으로 띄워 보낸다.
// 공이 목표가 아닌 소켓을 지나갈 때 붙잡히지 않도록 BowlingBall의 선택 필터가 목표 소켓만 허용한다.
public class BallReturnRack : MonoBehaviour
{
    [Tooltip("공을 받을 소켓. 앞에 있는 것부터 채운다(예: socket1, socket2).")]
    public XRSocketInteractor[] sockets;

    [Tooltip("길이 비어 있을 때 소켓 쪽으로 굴려 보내는 속도(m/s).")]
    public float rollSpeed = 0.8f;

    [Tooltip("앞 소켓의 공을 넘어갈 때, 그 공 꼭대기보다 이만큼(m) 더 높게 띄운다.")]
    public float hopClearance = 0.08f;

    [Tooltip("목표 소켓에 이 시간(초) 안에 들어가지 못하면 목표를 풀어 어느 소켓이든 받을 수 있게 한다.")]
    public float targetTimeout = 3f;

    private readonly Dictionary<XRSocketInteractor, BowlingBall> reserved = new Dictionary<XRSocketInteractor, BowlingBall>();

    // 비어 있고 다른 공이 가는 중이 아닌 첫 소켓. 없으면 null.
    public XRSocketInteractor PickSocket(BowlingBall ball)
    {
        Release(ball);

        foreach (XRSocketInteractor socket in sockets)
        {
            if (socket == null || !socket.isActiveAndEnabled) continue;
            if (socket.hasSelection) continue;
            if (reserved.TryGetValue(socket, out BowlingBall other) && other != null) continue;

            reserved[socket] = ball;
            return socket;
        }

        return null;
    }

    public void Release(BowlingBall ball)
    {
        XRSocketInteractor found = null;
        foreach (var pair in reserved)
        {
            if (pair.Value == ball)
            {
                found = pair.Key;
                break;
            }
        }

        if (found != null) reserved.Remove(found);
    }

    // from에서 socket까지 보낼 초기 속도. 사이에 공이 꽂힌 소켓이 있으면 넘어가는 포물선 속도를 준다.
    public Vector3 GetLaunchVelocity(Vector3 from, XRSocketInteractor socket, float ballRadius)
    {
        Vector3 target = socket.GetAttachTransform(null).position;
        Vector3 flat = target - from;
        flat.y = 0f;
        float distance = flat.magnitude;
        if (distance < 0.001f) return Vector3.zero;

        float obstacleTop = GetObstacleTop(from, target, socket, ballRadius);
        if (float.IsNegativeInfinity(obstacleTop))
        {
            return flat / distance * rollSpeed;
        }

        // 꼭대기가 장애물 공 위를 지나는 포물선. 올라가는 시간 + 내려오는 시간 동안 수평 거리를 간다.
        float g = Mathf.Abs(Physics.gravity.y);
        float apex = Mathf.Max(obstacleTop + ballRadius + hopClearance, from.y, target.y);
        float up = Mathf.Sqrt(2f * g * (apex - from.y));
        float time = up / g + Mathf.Sqrt(2f * (apex - target.y) / g);

        Vector3 velocity = flat / time;
        velocity.y = up;
        return velocity;
    }

    // from → target 사이(수평으로)에 공이 꽂혀 있는 다른 소켓이 있으면 그 공의 꼭대기 높이, 없으면 -Infinity.
    private float GetObstacleTop(Vector3 from, Vector3 target, XRSocketInteractor targetSocket, float ballRadius)
    {
        float top = float.NegativeInfinity;
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(target.x, target.z);

        foreach (XRSocketInteractor socket in sockets)
        {
            if (socket == null || socket == targetSocket || !socket.hasSelection) continue;

            Vector3 held = socket.interactablesSelected[0].transform.position;
            Vector2 p = new Vector2(held.x, held.z);
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            if (t <= 0f || t >= 1f) continue;
            if (Vector2.Distance(a + ab * t, p) > ballRadius * 2f) continue;

            top = Mathf.Max(top, held.y + ballRadius);
        }

        return top;
    }
}
