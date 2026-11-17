using System.Collections;
using UnityEngine;

// 레인 양옆 거터(구석 홈) 감지 구역. 거터를 덮는 트리거 콜라이더에 붙인다.
// 투구 중인 공이 들어오면 BAD! 이미지(RollResultPopup)를 잠시 띄우고, returnDelay 뒤에 공을 복귀 위치로 보낸다(BowlingBall.ReturnToRack).
// 복귀 시 투구 집계는 기존 흐름(GameManager/TutorialManager.OnBallCollected)을 그대로 따른다.
[RequireComponent(typeof(Collider))]
public class GutterZone : MonoBehaviour
{
    [Tooltip("거터에 들어간 뒤 공을 복귀시키기까지 기다리는 시간(초).")]
    public float returnDelay = 1f;

    [Tooltip("거터에 빠졌을 때 재생할 효과음. 비우면 재생하지 않는다.")]
    public AudioClip badClip;

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

        if (ball == null) return;

        // 투구 중이 아니면(손에 든 채 거터에 넣는 등) 이미지 없이 복귀만 한다.
        if (IsRolling())
        {
            ShowBad();
        }

        StartCoroutine(ReturnRoutine(ball));
    }

    private IEnumerator ReturnRoutine(BowlingBall ball)
    {
        bool wasRolling = IsRolling();

        if (returnDelay > 0f)
        {
            yield return new WaitForSeconds(returnDelay);
        }

        // 그 사이 핀 뒤 수거 구역에 먼저 들어가 이미 집계 중이면 다시 보내지 않는다.
        if (wasRolling && !IsRolling()) yield break;

        ball.ReturnToRack();
    }

    private bool IsRolling()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.State == GameManager.GameState.Rolling;
        }

        if (TutorialManager.Instance != null)
        {
            return TutorialManager.Instance.State == TutorialManager.RollState.Rolling;
        }

        return false;
    }

    private void ShowBad()
    {
        if (badClip != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(badClip);
        }

        // 지금 던지는 사람의 레인 팝업에 띄운다.
        PinSetter lane = GameManager.Instance != null ? GameManager.Instance.ActiveLane
            : TutorialManager.Instance != null ? TutorialManager.Instance.pinSetter : null;
        RollResultPopup popup = RollResultPopup.For(lane);
        if (popup != null) popup.ShowBad();
    }
}
