using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// 내 차례가 시작되면 HMD 시야에 "YOUR TURN"을 잠깐 띄운다.
// VR에서는 Screen Space - Overlay가 헤드셋에 그려지지 않으므로 XR 카메라에 붙는 Screen Space - Camera로 띄운다.
// 내 번호(localPlayerIndex)는 GameManager.CurrentTurnIndex와 같은 규칙(0 = 1P, 1 = 2P)이다.
// -1(자동)이면 온라인일 때 호스트 0, 클라이언트 1(NetworkedXRAvatar.Slot과 같음), 오프라인(CPU 대전)이면 0.
[RequireComponent(typeof(Canvas))]
public class TurnBanner : MonoBehaviour
{
    [Tooltip("배너 전체(배경 + 문구). 이 그룹의 알파로 나타나고 사라진다.")]
    public CanvasGroup banner;
    public TextMeshProUGUI messageText;
    [TextArea] public string message = "YOUR TURN";

    [Tooltip("이 HMD 플레이어의 번호. -1이면 자동(온라인: 호스트 0, 클라이언트 1 / 오프라인: 0).")]
    public int localPlayerIndex = -1;
    [Tooltip("비우면 Camera.main(XR 카메라)을 쓴다.")]
    public Camera uiCamera;
    [Tooltip("카메라 앞 이 거리(m)에 UI를 그린다.")]
    public float planeDistance = 1f;

    [Header("Timing")]
    public float fadeInTime = 0.3f;
    public float showTime = 2f;
    public float fadeOutTime = 0.5f;

    private Canvas canvas;
    private GameManager subscribed;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (banner != null)
        {
            banner.alpha = 0f;
            banner.blocksRaycasts = false;
            banner.interactable = false;
        }
    }

    void Start()
    {
        AttachCamera();

        if (GameManager.Instance != null)
        {
            subscribed = GameManager.Instance;
            subscribed.TurnStarted += OnTurnStarted;

            // GameManager.Start가 먼저 돌아 첫 차례 알림을 놓쳤을 수 있다.
            if (subscribed.State == GameManager.GameState.Ready) OnTurnStarted(subscribed.CurrentTurnIndex);
        }
    }

    void OnDestroy()
    {
        if (subscribed != null) subscribed.TurnStarted -= OnTurnStarted;
    }

    public int GetLocalPlayerIndex()
    {
        if (localPlayerIndex >= 0) return localPlayerIndex;

        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.IsListening) return network.IsServer ? 0 : 1;
        return 0;
    }

    private void OnTurnStarted(int turnIndex)
    {
        if (turnIndex != GetLocalPlayerIndex()) return;
        Show();
    }

    public void Show()
    {
        if (banner == null) return;
        AttachCamera();
        if (messageText != null) messageText.text = message;
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return Fade(banner.alpha, 1f, fadeInTime);
        yield return new WaitForSeconds(showTime);
        yield return Fade(1f, 0f, fadeOutTime);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            banner.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        banner.alpha = to;
    }

    // 온라인에서는 씬 로드 뒤 리그가 옮겨지거나 바뀔 수 있어 띄울 때마다 카메라를 다시 확인한다.
    private void AttachCamera()
    {
        Camera target = uiCamera != null ? uiCamera : Camera.main;
        if (target == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = target;
        canvas.planeDistance = planeDistance;
    }
}
