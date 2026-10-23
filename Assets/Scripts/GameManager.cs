using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

// 솔로 1게임(10프레임) 진행.
// 투구(BowlingBall.OnBallThrown) → 공 회수(OnBallCollected) → 핀이 멈출 때까지 대기
// → 쓰러진 핀 집계 → 점수 기록 → 핀 정리/재세팅 → 다음 투구, 10프레임이 끝나면 결과 표시.
public class GameManager : MonoBehaviour
{
    public enum GameState { Ready, Rolling, Settling, GameOver }

    public static GameManager Instance;

    [Header("Lane")]
    public PinSetter pinSetter;
    [Tooltip("이 레인의 공. 지정하면 다른 레인의 공 투구는 무시한다. 비우면 모든 공을 받는다.")]
    public BowlingBall ball;

    [Header("UI (선택 사항)")]
    public TextMeshProUGUI scoreText;
    [Tooltip("프레임별 표기와 누적 점수를 보여 주는 점수판 텍스트.")]
    public TextMeshProUGUI scoreBoardText;
    [Tooltip("\"Frame 3 - Ball 1\", \"STRIKE!\" 같은 안내 문구.")]
    public TextMeshProUGUI statusText;
    [Tooltip("게임 종료 때 켜는 결과 패널.")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Timing")]
    [Tooltip("공이 수거된 뒤 핀 판정 전 최소 대기 시간(초).")]
    public float minSettleTime = 1.5f;
    [Tooltip("핀이 계속 흔들려도 이 시간(초)이 지나면 판정한다.")]
    public float maxSettleTime = 5f;
    [Tooltip("이 속도보다 느리면 핀이 멈춘 것으로 본다.")]
    public float pinRestSpeed = 0.05f;
    [Tooltip("공이 수거 구역에 닿지 않아도 투구 후 이 시간(초)이 지나면 공을 회수하고 판정한다.")]
    public float rollTimeout = 12f;

    public GameState State { get; private set; } = GameState.Ready;

    private readonly BowlingScoreCard scoreCard = new BowlingScoreCard();
    private BowlingBall currentBall;
    private float rollStartTime;
    private int standingBeforeRoll;

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
        StartNewGame();
    }

    void Update()
    {
        if (State == GameState.Rolling && Time.time - rollStartTime > rollTimeout && currentBall != null)
        {
            // 공이 레인 어딘가에 멈춘 경우. 회수 처리에서 OnBallCollected가 호출된다.
            currentBall.ReturnToRack();
        }
    }

    // UI 버튼(다시 하기)에서도 호출한다.
    public void StartNewGame()
    {
        StopAllCoroutines();
        scoreCard.Reset();
        if (pinSetter != null) pinSetter.ResetAllPins();
        if (resultPanel != null) resultPanel.SetActive(false);
        currentBall = null;
        State = GameState.Ready;
        UpdateUI();
    }

    public void OnBallThrown(BowlingBall thrownBall)
    {
        if (State != GameState.Ready) return;
        if (ball != null && thrownBall != ball) return;

        if (pinSetter == null)
        {
            Debug.LogWarning("GameManager: pinSetter가 지정되지 않아 투구를 집계할 수 없습니다.", this);
            return;
        }

        currentBall = thrownBall;
        rollStartTime = Time.time;
        standingBeforeRoll = pinSetter.CountStanding();
        State = GameState.Rolling;
        SetStatus($"Frame {scoreCard.CurrentFrame} - Ball {scoreCard.CurrentRollInFrame}");
    }

    // 굴러가던 공을 다시 손으로 잡으면 이번 투구는 없던 것으로 한다.
    public void OnBallGrabbed(BowlingBall grabbedBall)
    {
        if (State == GameState.Rolling && grabbedBall == currentBall)
        {
            currentBall = null;
            State = GameState.Ready;
            UpdateUI();
        }
    }

    public void OnBallCollected(BowlingBall collectedBall)
    {
        if (State != GameState.Rolling || collectedBall != currentBall) return;

        State = GameState.Settling;
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
        int frameBefore = scoreCard.CurrentFrame;
        int rollBefore = scoreCard.CurrentRollInFrame;
        scoreCard.AddRoll(knocked);

        string message = GetRollMessage(knocked, frameBefore, rollBefore);
        PlayRollEffect(message);

        if (scoreCard.IsGameOver)
        {
            State = GameState.GameOver;
            UpdateUI();
            SetStatus(message);
            ShowResult();
            yield break;
        }

        // 다음 투구가 새 랙이면 10개를 다시 세우고, 아니면 쓰러진 핀만 치운다.
        if (scoreCard.PinsStanding == BowlingScoreCard.PinCount)
        {
            pinSetter.ResetAllPins();
        }
        else
        {
            pinSetter.ClearFallenPins();
        }

        currentBall = null;
        State = GameState.Ready;
        UpdateUI();
        if (!string.IsNullOrEmpty(message)) SetStatus(message);
    }

    private string GetRollMessage(int knocked, int frame, int roll)
    {
        bool freshRack = standingBeforeRoll == BowlingScoreCard.PinCount;
        if (knocked == BowlingScoreCard.PinCount && freshRack) return "STRIKE!";
        if (knocked == standingBeforeRoll && !freshRack) return "SPARE!";
        if (knocked == 0) return roll == 1 && frame < BowlingScoreCard.FrameCount ? "Gutter" : "Miss";
        return $"{knocked} pin{(knocked == 1 ? "" : "s")}";
    }

    private void PlayRollEffect(string message)
    {
        if (BowlingEffects.Instance == null) return;
        if (message == "STRIKE!") BowlingEffects.Instance.PlayStrike(pinSetter.GetRackCenter());
        else if (message == "SPARE!") BowlingEffects.Instance.PlaySpare(pinSetter.GetRackCenter());
    }

    private void ShowResult()
    {
        int total = scoreCard.GetTotalScore();
        if (resultText != null) resultText.text = $"Game Over\nScore : {total}";
        if (resultPanel != null) resultPanel.SetActive(true);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score : " + scoreCard.GetTotalScore().ToString();
        }

        if (scoreBoardText != null)
        {
            scoreBoardText.text = BuildScoreBoard();
        }

        if (State == GameState.Ready)
        {
            SetStatus($"Frame {scoreCard.CurrentFrame} - Ball {scoreCard.CurrentRollInFrame}");
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private string BuildScoreBoard()
    {
        int?[] totals = scoreCard.GetCumulativeScores();
        StringBuilder marks = new StringBuilder();
        StringBuilder scores = new StringBuilder();
        StringBuilder header = new StringBuilder();

        for (int i = 0; i < BowlingScoreCard.FrameCount; i++)
        {
            header.Append((i + 1).ToString().PadRight(6));
            marks.Append(scoreCard.GetFrameMarks(i).PadRight(6));
            scores.Append((totals[i].HasValue ? totals[i].Value.ToString() : "").PadRight(6));
        }

        return "<mspace=0.6em>" + header + "\n" + marks + "\n" + scores + "</mspace>";
    }
}
