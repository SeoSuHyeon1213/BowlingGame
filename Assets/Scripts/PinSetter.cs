using System.Collections.Generic;
using UnityEngine;

// 한 레인의 핀 10개를 관리한다. 핀 묶음의 부모 오브젝트(예: "Pin")에 붙인다.
// pins를 비워 두면 자식에 있는 BowlingPin을 자동으로 모은다.
public class PinSetter : MonoBehaviour
{
    public List<BowlingPin> pins = new List<BowlingPin>();

    void Awake()
    {
        if (pins.Count == 0)
        {
            pins.AddRange(GetComponentsInChildren<BowlingPin>(true));
        }

        if (pins.Count != BowlingScoreCard.PinCount)
        {
            Debug.LogWarning($"PinSetter: 핀이 {pins.Count}개입니다. 10개여야 점수가 정확합니다.", this);
        }
    }

    public int CountStanding()
    {
        int count = 0;
        foreach (BowlingPin pin in pins)
        {
            if (pin != null && !pin.IsKnockedDown()) count++;
        }
        return count;
    }

    public bool AnyPinMoving(float speedThreshold)
    {
        foreach (BowlingPin pin in pins)
        {
            if (pin != null && pin.IsMoving(speedThreshold)) return true;
        }
        return false;
    }

    // 1구 후: 쓰러진 핀만 치우고 서 있는 핀은 그대로 둔다.
    public void ClearFallenPins()
    {
        foreach (BowlingPin pin in pins)
        {
            if (pin != null && pin.gameObject.activeSelf && pin.IsKnockedDown()) pin.Remove();
        }
    }

    // 새 프레임: 10개 전부 다시 세운다.
    public void ResetAllPins()
    {
        foreach (BowlingPin pin in pins)
        {
            if (pin != null) pin.ResetPin();
        }
    }
}
