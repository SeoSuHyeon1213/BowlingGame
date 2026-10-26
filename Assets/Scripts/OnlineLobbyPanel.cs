using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로비 화면(월드 스페이스 Canvas)의 온라인 1:1 버튼. 빠른 매칭 / 취소와 접속 상태 문구를 보여 준다.
// 버튼 클릭 연결은 코드에서 하므로 Inspector의 OnClick은 비워 둔다.
public class OnlineLobbyPanel : MonoBehaviour
{
    public Button quickMatchButton;
    public Button cancelButton;
    public TextMeshProUGUI statusText;
    [Tooltip("다른 화면으로 넘어가는 버튼(예: 메인 게임 시작). 접속 중에는 누르지 못하게 막는다.")]
    public Button[] disableWhileOnline;

    private OnlineSessionManager manager;

    void OnEnable()
    {
        if (quickMatchButton != null) quickMatchButton.onClick.AddListener(OnQuickMatch);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
    }

    void OnDisable()
    {
        if (quickMatchButton != null) quickMatchButton.onClick.RemoveListener(OnQuickMatch);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
        Bind(null);
    }

    void Update()
    {
        // 로비로 돌아오면 매니저 사본이 바뀌므로 매 프레임 현재 인스턴스에 붙어 있는지 확인한다.
        if (manager != OnlineSessionManager.Instance) Bind(OnlineSessionManager.Instance);
    }

    private void Bind(OnlineSessionManager next)
    {
        if (manager != null) manager.StateChanged -= Refresh;
        manager = next;
        if (manager != null) manager.StateChanged += Refresh;
        Refresh();
    }

    private void OnQuickMatch()
    {
        if (manager != null) manager.QuickMatch();
    }

    private void OnCancel()
    {
        if (manager != null) manager.Leave();
    }

    private void Refresh()
    {
        bool available = manager != null;
        bool offline = !available || manager.State == OnlineSessionManager.ConnectionState.Offline;
        bool waiting = available && manager.State == OnlineSessionManager.ConnectionState.Waiting;

        if (quickMatchButton != null) quickMatchButton.interactable = available && offline;
        // 접속 요청 중(Connecting)에는 취소할 세션이 아직 없으므로 대기 중일 때만 취소를 허용한다.
        if (cancelButton != null) cancelButton.interactable = waiting;
        if (disableWhileOnline != null)
        {
            foreach (Button b in disableWhileOnline)
            {
                if (b != null) b.interactable = offline;
            }
        }

        if (statusText != null)
        {
            if (!available) statusText.text = "Online unavailable";
            else if (!string.IsNullOrEmpty(manager.StatusMessage)) statusText.text = manager.StatusMessage;
            else statusText.text = "Play 1:1 online";
        }
    }
}
