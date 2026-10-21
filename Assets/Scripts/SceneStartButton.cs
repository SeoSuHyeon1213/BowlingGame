using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 버튼을 누르면 지정한 씬으로 넘어간다. 튜토리얼(연습)에서 메인 게임(BowlingScene)을 시작할 때 쓴다.
// Button이 있는 오브젝트에 붙이면 클릭 연결은 코드에서 하므로 Inspector의 OnClick은 비워 둔다.
[RequireComponent(typeof(Button))]
public class SceneStartButton : MonoBehaviour
{
    [Tooltip("불러올 씬 이름. Build Settings의 Scene List에 들어 있어야 한다.")]
    public string sceneName = "BowlingScene";

    private Button button;
    private bool isLoading = false;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadScene);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(LoadScene);
    }

    public void LoadScene()
    {
        if (isLoading) return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneStartButton: '{sceneName}' 씬이 Build Settings에 없어 불러올 수 없습니다.", this);
            return;
        }

        // 두 번 눌러 중복 로딩되지 않게 막는다.
        isLoading = true;
        button.interactable = false;

        // 일시 정지 중에 눌렸더라도 다음 씬이 멈춘 채 시작하지 않게 한다.
        Time.timeScale = 1f;
        PlayerPrefs.Save();

        // VR에서 화면이 멈추는 시간을 줄이려고 비동기로 불러온다.
        SceneManager.LoadSceneAsync(sceneName);
    }
}
