using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

// 1게임(10프레임) 진행. cpuBowler를 지정하면 플레이어와 CPU가 프레임마다 번갈아 던지고, 비우면 솔로.
// 투구(BowlingBall.OnBallThrown / CpuBowler) → 공 회수(OnBallCollected) → 핀이 멈출 때까지 대기
// → 쓰러진 핀 집계 → 점수 기록 → 핀 정리/재세팅 → 다음 투구, 10프레임이 끝나면 결과 표시.
public class GameManager : MonoBehaviour
{
    public enum GameState { Ready, Rolling, Settling, GameOver }

    public static GameManager Instance;

    [Header("Lane")]
    public PinSetter pinSetter;
    [Tooltip("이 레인의 공. 지정하면 다른 레인의 공 투구는 무시한다. 비우면 모든 공을 받는다.")]
    public BowlingBall ball;

    [Header("Opponent (선택 사항)")]
    [Tooltip("지정하면 플레이어와 CPU가 같은 레인에서 프레임마다 번갈아 던진다. 비우면 솔로 게임.")]
    public CpuBowler cpuBowler;

    [Header("UI (선택 사항)")]
    public TextMeshProUGUI scoreText;
    [Tooltip("CPU 점수 텍스트. 비우면 CPU 점수도 scoreText에 함께 표시한다.")]
    public TextMeshProUGUI cpuScoreText;
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

    public bool IsCpuTurn => cpuTurn;

    private readonly BowlingScoreCard scoreCard = new BowlingScoreCard();
    private readonly BowlingScoreCard cpuCard = new BowlingScoreCard();
    private bool cpuTurn;
    private BowlingBall currentBall;
    private float rollStartTime;
    private int standingBeforeRoll;

    private bool HasCpu => cpuBowler != null;
    private BowlingScoreCard ActiveCard => cpuTurn ? cpuCard : scoreCard;

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
        if (HasCpu) cpuBowler.CancelTurn();
        scoreCard.Reset();
        cpuCard.Reset();
        cpuTurn = false;
        if (pinSetter != null) pinSetter.ResetAllPins();
        if (resultPanel != null) resultPanel.SetActive(false);
        currentBall = null;
        State = GameState.Ready;
        UpdateUI();
    }

    // 플레이어가 공을 손으로 잡을 수 있는지(BowlingBall의 선택 필터에서 사용). CPU 차례에는 잡지 못한다.
    public bool CanPlayerGrab()
    {
        return !cpuTurn;
    }

    // 투구로 받아들였으면 true. 차례가 아닌 쪽의 공이거나 투구를 받을 상태가 아니면 false.
    public bool OnBallThrown(BowlingBall thrownBall)
    {
        if (State != GameState.Ready) return false;

        BowlingBall expected = cpuTurn ? cpuBowler.ball : ball;
        if (expected != null && thrownBall != expected) return false;
        if (!cpuTurn && HasCpu && thrownBall == cpuBowler.ball) return false;

        if (pinSetter == null)
        {
            Debug.LogWarning("GameManager: pinSetter가 지정되지 않아 투구를 집계할 수 없습니다.", this);
            return false;
        }

        currentBall = thrownBall;
        rollStartTime = Time.time;
        standingBeforeRoll = pinSetter.CountStanding();
        State = GameState.Rolling;
        SetStatus(GetTurnStatus());
        return true;
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

        BowlingScoreCard card = ActiveCard;
        int knocked = standingBeforeRoll - pinSetter.CountStanding();
        int frameBefore = card.CurrentFrame;
        int rollBefore = card.CurrentRollInFrame;
        card.AddRoll(knocked);

        string message = GetRollMessage(knocked, frameBefore, rollBefore);
        PlayRollEffect(message);

        if (scoreCard.IsGameOver && (!HasCpu || cpuCard.IsGameOver))
        {
            State = GameState.GameOver;
            UpdateUI();
            SetStatus(message);
            ShowResult();
            yield break;
        }

        // 대전이면 프레임이 끝날 때마다 차례를 넘긴다. 플레이어 10프레임 뒤에는 CPU 10프레임이 이어진다.
        bool frameDone = card.IsGameOver || card.CurrentFrame != frameBefore;
        bool turnChanged = HasCpu && frameDone;
        if (turnChanged) cpuTurn = !cpuTurn;

        // 다음 투구가 새 랙이면 10개를 다시 세우고, 아니면 쓰러진 핀만 치운다.
        if (turnChanged || ActiveCard.PinsStanding == BowlingScoreCard.PinCount)
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

        if (cpuTurn) cpuBowler.TakeTurn();
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
        if (resultText != null)
        {
            if (HasCpu)
            {
                int cpuTotal = cpuCard.GetTotalScore();
                string winner = total > cpuTotal ? "You Win!" : total < cpuTotal ? "CPU Wins" : "Draw";
                resultText.text = $"Game Over\nYou : {total}\nCPU : {cpuTotal}\n{winner}";
            }
            else
            {
                resultText.text = $"Game Over\nScore : {total}";
            }
        }
        if (resultPanel != null) resultPanel.SetActive(true);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            if (!HasCpu)
            {
                scoreText.text = "Score : " + scoreCard.GetTotalScore().ToString();
            }
            else if (cpuScoreText != null)
            {
                scoreText.text = "You : " + scoreCard.GetTotalScore().ToString();
            }
            else
            {
                scoreText.text = $"You : {scoreCard.GetTotalScore()}   CPU : {cpuCard.GetTotalScore()}";
            }
        }

        if (cpuScoreText != null)
        {
            cpuScoreText.text = HasCpu ? "CPU : " + cpuCard.GetTotalScore().ToString() : "";
        }

        if (scoreBoardText != null)
        {
            scoreBoardText.text = BuildScoreBoard();
        }

        if (State == GameState.Ready)
        {
            SetStatus(GetTurnStatus());
        }
    }

    private string GetTurnStatus()
    {
        BowlingScoreCard card = ActiveCard;
        string prefix = !HasCpu ? "" : cpuTurn ? "CPU - " : "You - ";
        return $"{prefix}Frame {card.CurrentFrame} - Ball {card.CurrentRollInFrame}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // 솔로: 프레임 번호 / 표기 / 누적 점수. 대전: 프레임 번호 아래에 YOU, CPU 두 줄씩.
    private string BuildScoreBoard()
    {
        if (!HasCpu)
        {
            return "<mspace=0.6em>" + BuildHeader("") + "\n" + BuildRows(scoreCard, "") + "</mspace>";
        }

        return "<mspace=0.6em>" + BuildHeader("    ") + "\n" + BuildRows(scoreCard, "YOU ") + "\n" + BuildRows(cpuCard, "CPU ") + "</mspace>";
    }

    private static string BuildHeader(string prefix)
    {
        StringBuilder header = new StringBuilder(prefix);
        for (int i = 0; i < BowlingScoreCard.FrameCount; i++)
        {
            header.Append((i + 1).ToString().PadRight(6));
        }
        return header.ToString();
    }

    private static string BuildRows(BowlingScoreCard card, string label)
    {
        int?[] totals = card.GetCumulativeScores();
        StringBuilder marks = new StringBuilder(label);
        StringBuilder scores = new StringBuilder(new string(' ', label.Length));

        for (int i = 0; i < BowlingScoreCard.FrameCount; i++)
        {
            marks.Append(card.GetFrameMarks(i).PadRight(6));
            scores.Append((totals[i].HasValue ? totals[i].Value.ToString() : "").PadRight(6));
        }

        return marks + "\n" + scores;
    }
}
