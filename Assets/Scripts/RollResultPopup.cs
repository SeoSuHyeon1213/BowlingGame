using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 투구 결과 이미지(STRIKE! / SPARE! / BAD!)와 차례 이미지(1P / 2P)를 잠시 띄운다.
// 월드 공간 Canvas 아래 Image에 붙이거나, resultImage에 그 Image를 지정한다. 평소에는 숨겨 둔다.
// GameManager/TutorialManager가 스트라이크·스페어 때, GutterZone이 거터에 빠졌을 때, GameManager가 차례가 바뀔 때 호출한다.
// 여러 개가 이어서 오면(예: SPARE! 직후 차례 변경) 순서대로 하나씩 보여 준다.
public class RollResultPopup : MonoBehaviour
{
    public static RollResultPopup Instance;

    [Tooltip("결과 이미지를 표시할 Image. 비우면 이 오브젝트의 Image를 쓴다.")]
    public Image resultImage;

    [Header("Sprites")]
    public Sprite strikeSprite;
    public Sprite spareSprite;
    public Sprite badSprite;
    [Tooltip("플레이어 차례가 될 때 보여 줄 이미지(1P).")]
    public Sprite player1Sprite;
    [Tooltip("CPU 차례가 될 때 보여 줄 이미지(2P).")]
    public Sprite player2Sprite;

    [Tooltip("이미지 하나를 보여 주는 시간(초).")]
    public float showDuration = 1.5f;

    // 차례 이미지를 띄울 수 있으면 GameManager가 상태 문구에서 "You - "/"CPU - "를 뺀다.
    public bool CanShowTurn => resultImage != null && player1Sprite != null && player2Sprite != null;

    private readonly Queue<Sprite> queue = new Queue<Sprite>();
    private Coroutine playRoutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        if (resultImage == null) resultImage = GetComponent<Image>();
        if (resultImage != null)
        {
            resultImage.preserveAspect = true;
            resultImage.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowStrike() { Show(strikeSprite); }
    public void ShowSpare() { Show(spareSprite); }
    public void ShowBad() { Show(badSprite); }
    public void ShowTurn(bool cpuTurn) { Show(cpuTurn ? player2Sprite : player1Sprite); }

    // 새 게임 등으로 대기 중인 이미지를 버린다.
    public void Clear()
    {
        queue.Clear();
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        if (resultImage != null) resultImage.enabled = false;
    }

    private void Show(Sprite sprite)
    {
        if (resultImage == null || sprite == null) return;

        queue.Enqueue(sprite);
        if (playRoutine == null) playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        while (queue.Count > 0)
        {
            resultImage.sprite = queue.Dequeue();
            resultImage.enabled = true;
            yield return new WaitForSeconds(showDuration);
        }

        resultImage.enabled = false;
        playRoutine = null;
    }
}
