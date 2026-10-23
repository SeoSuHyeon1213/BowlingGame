using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// 볼링공 복귀 처리.
// 수거 구역(BallReturnZone)에 들어오면 returnPoint로 옮긴 뒤 물리로 굴러가게 두고,
// 볼 스탠드의 XRSocketInteractor(큐브 소켓)가 공을 받아 안착시킨다.
[RequireComponent(typeof(Rigidbody))]
public class BowlingBall : MonoBehaviour
{
    [Tooltip("공이 다시 나오는 위치. 여기서 굴러가 소켓에 안착하도록 배치한다.")]
    public Transform returnPoint;

    [Tooltip("복귀 위치에 놓을 때 줄 초기 속도(월드 좌표, m/s). 0이면 중력만으로 굴러간다.")]
    public Vector3 returnVelocity = Vector3.zero;

    [Tooltip("수거 구역에 들어간 뒤 복귀까지 기다리는 시간(초).")]
    public float returnDelay = 0f;

    [Tooltip("손에서 놓은 직후 이 속도(m/s) 이상이면 투구로 본다. 그보다 느리면 그냥 내려놓은 것으로 본다.")]
    public float minThrowSpeed = 0.5f;

    private Rigidbody rb;
    private XRBaseInteractable interactable;
    private bool isReturning = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }
    }

    void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // 소켓 안착은 투구와 무관하다. 손으로 다시 잡으면 진행 중인 투구를 취소한다.
        if (args.interactorObject is XRSocketInteractor) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBallGrabbed(this);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.isCanceled || args.interactorObject is XRSocketInteractor) return;

        StartCoroutine(CheckThrowRoutine());
    }

    // 놓는 순간에는 던지기 속도가 아직 적용되지 않았을 수 있어 물리 갱신 후 속도를 확인한다.
    private IEnumerator CheckThrowRoutine()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (interactable != null && interactable.isSelected) yield break;

        if (rb.linearVelocity.magnitude >= minThrowSpeed && GameManager.Instance != null)
        {
            GameManager.Instance.OnBallThrown(this);
        }
    }

    // 투구 중 핀에 부딪히면 충돌 지점에 이펙트를 띄운다.
    private void OnCollisionEnter(Collision collision)
    {
        if (BowlingEffects.Instance == null || GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameManager.GameState.Rolling) return;
        if (collision.collider.GetComponentInParent<BowlingPin>() == null) return;

        BowlingEffects.Instance.PlayPinHit(collision.GetContact(0).point);
    }

    public void ReturnToRack()
    {
        if (isReturning)
        {
            return;
        }

        if (returnPoint == null)
        {
            Debug.LogWarning("BowlingBall: returnPoint가 지정되지 않아 공을 복귀시킬 수 없습니다.", this);
            return;
        }

        // 손이나 소켓에 잡혀 있는 동안에는 복귀시키지 않는다.
        if (interactable != null && interactable.isSelected)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBallCollected(this);
        }

        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        isReturning = true;

        if (returnDelay > 0f)
        {
            yield return new WaitForSeconds(returnDelay);
        }

        // 그 사이 플레이어가 잡았다면 취소한다.
        if (interactable != null && interactable.isSelected)
        {
            isReturning = false;
            yield break;
        }

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.position = returnPoint.position;
        transform.position = returnPoint.position;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = returnVelocity;
        }

        // 위치 이동이 물리에 반영될 때까지 중복 복귀를 막는다.
        yield return new WaitForFixedUpdate();
        isReturning = false;
    }
}
