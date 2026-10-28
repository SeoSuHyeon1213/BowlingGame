using System.Collections;
using TMPro;
using UnityEngine;

// 튜토리얼(연습) 진행. GameManager와 같은 투구 흐름을 쓰되 점수를 기록하지 않고,
// 단계별 안내(공 잡기 → 던지기 → 결과 보기)와 연습 기록(투구 수, 최고 기록)을 보여 준다.
// 정해진 횟수를 던지면 완료로 보고 메인 게임 시작 버튼을 켠다. 완료 뒤에도 계속 연습할 수 있다.
// 이 씬에는 GameManager를 두지 않는다(BowlingBall이 GameManager를 먼저 찾기 때문).
public class TutorialManager : MonoBehaviour
{
    public enum Step { GrabBall, ThrowBall, Watch, Practice }
    public enum RollState { Ready, Rolling, Settling }

    public static TutorialManager Instance;

    [Header("Lane")]
    public PinSetter pinSetter;
    [Tooltip("연습용 공. 지정하면 다른 공의 투구는 무시한다. 비우면 모든 공을 받는다.")]
    public BowlingBall ball;

    [Header("Progress")]
    [Tooltip("이만큼 던지면 튜토리얼 완료로 보고 메인 게임 버튼을 켠다.")]
    public int throwsToComplete = 3;
    [Tooltip("완료 전까지 숨겨 둘 메인 게임 시작 버튼(예: \"Main Game Start\"). 비우면 처음부터 그대로 둔다.")]
    public GameObject mainGameButton;
    [Tooltip("켜면 투구마다 핀 10개를 다시 세운다. 끄면 실제 게임처럼 2구까지 남은 핀을 노린다.")]
    public bool resetPinsEveryRoll = true;

    [Header("UI (선택 사항)")]
    [Tooltip("현재 단계 안내 문구.")]
    public TextMeshProUGUI instructionText;
    [Tooltip("직전 투구 결과(\"STRIKE!\", \"7 pins\" 등).")]
    public TextMeshProUGUI resultText;
    [Tooltip("연습 기록(투구 수, 최고 기록).")]
    public TextMeshProUGUI statsText;

    [Header("Messages")]
    [TextArea] public string grabMessage = "Step 1\nPress the GRIP button to pick up the ball";
    [TextArea] public string throwMessage = "Step 2\nSwing your arm and release GRIP to roll the ball";
    [TextArea] public string watchMessage = "Step 3\nWatch the pins fall!";
    [TextArea] public string practiceMessage = "Practice\nThrow {0} more time(s) to unlock the main game";
    [TextArea] public string completeMessage = "Well done!\nPress MAIN GAME / START when you're ready";

    [Header("Timing")]
    [Tooltip("공이 수거된 뒤 핀 판정 전 최소 대기 시간(초).")]
    public float minSettleTime = 1.5f;
    [Tooltip("핀이 계속 흔들려도 이 시간(초)이 지나면 판정한다.")]
    public float maxSettleTime = 5f;
    [Tooltip("이 속도보다 느리면 핀이 멈춘 것으로 본다.")]
    public float pinRestSpeed = 0.05f;
    [Tooltip("공이 수거 구역에 닿지 않아도 투구 후 이 시간(초)이 지나면 공을 회수하고 판정한다.")]
    public float rollTimeout = 12f;

    public Step CurrentStep { get; private set; } = Step.GrabBall;
    public RollState State { get; private set; } = RollState.Ready;
    public bool IsComplete => throwCount >= throwsToComplete;

    private BowlingBall currentBall;
    private float rollStartTime;
    private int standingBeforeRoll;
    private int rollsInRack;
    private int throwCount;
    private int bestKnocked;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartTutorial();
    }

    void Update()
    {
        if (State == RollState.Rolling && Time.time - rollStartTime > rollTimeout && currentBall != null)
        {
            // 공이 레인 어딘가에 멈춘 경우. 회수 처리에서 OnBallCollected가 호출된다.
            currentBall.ReturnToRack();
        }
    }

    // UI 버튼(처음부터)에서도 호출한다.
    public void StartTutorial()
    {
        StopAllCoroutines();
        throwCount = 0;
        bestKnocked = 0;
        rollsInRack = 0;
        currentBall = null;
        if (pinSetter != null) pinSetter.ResetAllPins();
        if (mainGameButton != null) mainGameButton.SetActive(false);
        if (resultText != null) resultText.text = "";
        State = RollState.Ready;
        SetStep(Step.GrabBall);
        UpdateStats();
    }

    public void OnBallGrabbed(BowlingBall grabbedBall)
    {
        if (ball != null && grabbedBall != ball) return;

        // 굴러가던 공을 다시 손으로 잡으면 이번 투구는 없던 것으로 한다.
        if (State == RollState.Rolling && grabbedBall == currentBall)
        {
            currentBall = null;
            State = RollState.Ready;
        }

        if (State == RollState.Ready && CurrentStep == Step.GrabBall) SetStep(Step.ThrowBall);
    }

    // 투구로 받아들였으면 true.
    public bool OnBallThrown(BowlingBall thrownBall)
    {
        if (State != RollState.Ready) return false;
        if (ball != null && thrownBall != ball) return false;

        if (pinSetter == null)
        {
            Debug.LogWarning("TutorialManager: pinSetter가 지정되지 않아 투구를 집계할 수 없습니다.", this);
            return false;
        }

        currentBall = thrownBall;
        rollStartTime = Time.time;
        standingBeforeRoll = pinSetter.CountStanding();
        State = RollState.Rolling;
        if (CurrentStep != Step.Practice) SetStep(Step.Watch);
        return true;
    }

    public void OnBallCollected(BowlingBall collectedBall)
    {
        if (State != RollState.Rolling || collectedBall != currentBall) return;

        State = RollState.Settling;
        StartCoroutine(ResolveRollRoutine());
    }

    private IEnumerator ResolveRollRoutine()
    {
        float start = Time.time;
        yield return new WaitForSeconds(minSettleTime);

        while (Time.time - start < maxSettleTime && pinSetter.AnyPinMoving(pinRestSpeed))
        {
            yield return null;
        }

        int knocked = standingBeforeRoll - pinSetter.CountStanding();
        bool freshRack = standingBeforeRoll == BowlingScoreCard.PinCount;
        rollsInRack++;
        throwCount++;
        if (freshRack && knocked > bestKnocked) bestKnocked = knocked;

        string message = GetRollMessage(knocked, freshRack);
        if (resultText != null) resultText.text = message;
        PlayRollEffect(message);

        // 투구마다 다시 세우거나, 실제 게임처럼 스트라이크/2구 뒤에만 다시 세운다.
        bool rackDone = resetPinsEveryRoll || knocked == standingBeforeRoll || rollsInRack >= 2;
        if (rackDone)
        {
            pinSetter.ResetAllPins();
            rollsInRack = 0;
        }
        else
        {
            pinSetter.ClearFallenPins();
        }

        currentBall = null;
        State = RollState.Ready;
        if (IsComplete && mainGameButton != null) mainGameButton.SetActive(true);
        SetStep(Step.Practice);
        UpdateStats();
    }

    private string GetRollMessage(int knocked, bool freshRack)
    {
        if (knocked == BowlingScoreCard.PinCount && freshRack) return "STRIKE!";
        if (knocked == standingBeforeRoll && !freshRack) return "SPARE!";
        if (knocked == 0) return "Gutter";
        return $"{knocked} pin{(knocked == 1 ? "" : "s")}";
    }

    private void PlayRollEffect(string message)
    {
        if (RollResultPopup.Instance != null)
        {
            if (message == "STRIKE!") RollResultPopup.Instance.ShowStrike();
            else if (message == "SPARE!") RollResultPopup.Instance.ShowSpare();
        }

        if (BowlingEffects.Instance == null) return;
        if (message == "STRIKE!") BowlingEffects.Instance.PlayStrike(pinSetter.GetRackCenter());
        else if (message == "SPARE!") BowlingEffects.Instance.PlaySpare(pinSetter.GetRackCenter());
    }

    private void SetStep(Step step)
    {
        CurrentStep = step;
        if (instructionText == null) return;

        switch (step)
        {
            case Step.GrabBall: instructionText.text = grabMessage; break;
            case Step.ThrowBall: instructionText.text = throwMessage; break;
            case Step.Watch: instructionText.text = watchMessage; break;
            default:
                instructionText.text = IsComplete ? completeMessage : string.Format(practiceMessage, throwsToComplete - throwCount);
                break;
        }
    }

    private void UpdateStats()
    {
        if (statsText != null) statsText.text = $"Throws : {throwCount}   Best : {bestKnocked}";
    }
}
