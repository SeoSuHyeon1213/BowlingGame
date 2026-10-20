using System.Collections.Generic;
using System.Text;

// 10프레임 볼링 점수 계산(표준 규칙). MonoBehaviour가 아닌 순수 데이터 클래스.
// 투구마다 AddRoll(쓰러뜨린 핀 수)을 호출하면 프레임 진행, 10프레임 보너스, 게임 종료를 처리한다.
public class BowlingScoreCard
{
    public const int FrameCount = 10;
    public const int PinCount = 10;

    private readonly List<List<int>> frames = new List<List<int>>();
    private int pinsStanding;

    public bool IsGameOver { get; private set; }

    // 현재(또는 마지막) 프레임 번호, 1부터 시작.
    public int CurrentFrame => frames.Count;

    // 현재 프레임에서 다음에 던질 투구 번호, 1부터 시작.
    public int CurrentRollInFrame => frames[frames.Count - 1].Count + 1;

    // 다음 투구 때 서 있을 핀 수. 10이면 핀을 전부 다시 세워야 한다.
    public int PinsStanding => pinsStanding;

    public BowlingScoreCard()
    {
        Reset();
    }

    public void Reset()
    {
        frames.Clear();
        frames.Add(new List<int>());
        pinsStanding = PinCount;
        IsGameOver = false;
    }

    public void AddRoll(int pins)
    {
        if (IsGameOver)
        {
            return;
        }

        if (pins < 0) pins = 0;
        if (pins > pinsStanding) pins = pinsStanding;

        List<int> frame = frames[frames.Count - 1];
        frame.Add(pins);
        pinsStanding -= pins;

        if (frames.Count < FrameCount)
        {
            // 1~9프레임: 스트라이크거나 2구를 던지면 다음 프레임.
            if ((frame.Count == 1 && pins == PinCount) || frame.Count == 2)
            {
                frames.Add(new List<int>());
                pinsStanding = PinCount;
            }
            return;
        }

        // 10프레임: 스트라이크/스페어면 최대 3구.
        if (frame.Count == 1)
        {
            if (pinsStanding == 0) pinsStanding = PinCount;
        }
        else if (frame.Count == 2)
        {
            bool earnedBonus = frame[0] == PinCount || frame[0] + frame[1] == PinCount;
            if (!earnedBonus)
            {
                IsGameOver = true;
            }
            else if (pinsStanding == 0)
            {
                pinsStanding = PinCount;
            }
        }
        else
        {
            IsGameOver = true;
        }
    }

    // 프레임별 누적 점수. 보너스 투구가 아직 없어 확정되지 않은 프레임은 null.
    public int?[] GetCumulativeScores()
    {
        int?[] result = new int?[FrameCount];
        List<int> rolls = new List<int>();
        foreach (List<int> f in frames) rolls.AddRange(f);

        int total = 0;
        int i = 0;
        for (int frameIndex = 0; frameIndex < FrameCount && frameIndex < frames.Count; frameIndex++)
        {
            List<int> f = frames[frameIndex];

            if (frameIndex == FrameCount - 1)
            {
                bool tenthDone = IsGameOver;
                if (!tenthDone) break;
                foreach (int r in f) total += r;
                result[frameIndex] = total;
                break;
            }

            if (f.Count >= 1 && f[0] == PinCount)
            {
                if (i + 2 >= rolls.Count) break;
                total += PinCount + rolls[i + 1] + rolls[i + 2];
                i += 1;
            }
            else if (f.Count == 2)
            {
                if (f[0] + f[1] == PinCount)
                {
                    if (i + 2 >= rolls.Count) break;
                    total += PinCount + rolls[i + 2];
                }
                else
                {
                    total += f[0] + f[1];
                }
                i += 2;
            }
            else
            {
                break;
            }

            result[frameIndex] = total;
        }

        return result;
    }

    // 확정된 점수 중 마지막 누적값.
    public int GetTotalScore()
    {
        int total = 0;
        foreach (int? s in GetCumulativeScores())
        {
            if (s.HasValue) total = s.Value;
        }
        return total;
    }

    // 프레임 표기: 스트라이크 X, 스페어 /, 0점 -. 예) "X", "7/", "9-", "X X 8".
    public string GetFrameMarks(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= frames.Count) return "";

        StringBuilder sb = new StringBuilder();
        int rackFirst = -1;
        foreach (int r in frames[frameIndex])
        {
            if (sb.Length > 0) sb.Append(' ');

            if (rackFirst < 0)
            {
                if (r == PinCount)
                {
                    sb.Append('X');
                }
                else
                {
                    sb.Append(r == 0 ? "-" : r.ToString());
                    rackFirst = r;
                }
            }
            else
            {
                sb.Append(rackFirst + r == PinCount ? "/" : (r == 0 ? "-" : r.ToString()));
                rackFirst = -1;
            }
        }
        return sb.ToString();
    }
}
