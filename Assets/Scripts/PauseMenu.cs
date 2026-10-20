using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// VR 일시 정지 창 제어: 이어하기, 나가기, 소리 끄기(토글), 소리 조절(슬라이더).
// 이 스크립트는 항상 켜져 있는 오브젝트(예: GameManager)에 붙이고, pausePanel에 일시 정지 창(Paused)을 연결한다.
// 창 자체에 붙이면 창이 꺼질 때 스크립트도 꺼져 다시 열 수 없다.
public class PauseMenu : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("일시 정지 창(World Space Canvas). 열고 닫을 때 켜고 끈다.")]
    public GameObject pausePanel;

    [Tooltip("창을 열 때 이 대상(보통 Main Camera) 앞에 창을 띄운다. 비우면 Camera.main.")]
    public Transform playerHead;
    [Tooltip("머리에서 창까지의 거리(m).")]
    public float panelDistance = 1.5f;
    [Tooltip("눈높이 기준 창의 높이 보정(m).")]
    public float panelHeightOffset = -0.1f;

    [Header("Input")]
    [Tooltip("일시 정지 열기/닫기 액션(예: XRI Left/Menu). 비우면 UI 버튼과 Editor의 Esc 키로만 연다.")]
    public InputActionReference pauseAction;

    [Header("Buttons")]
    public Button resumeButton;
    public Button quitButton;

    [Header("Sound")]
    [Tooltip("소리 끄기 버튼. 누를 때마다 음소거가 켜지고 꺼진다. Toggle을 쓰면 비워 두고 muteToggle을 연결한다.")]
    public Button muteButton;
    [Tooltip("Toggle 컴포넌트를 쓰는 경우. 체크(isOn) = 음소거.")]
    public Toggle muteToggle;
    [Tooltip("음소거 상태에 따라 바꿀 아이콘(선택).")]
    public Image muteIcon;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    [Tooltip("음소거 상태 문구를 보여 줄 텍스트(선택).")]
    public TextMeshProUGUI muteLabel;
    public string soundOnText = "Sound On";
    public string soundOffText = "Sound Off";

    public Slider volumeSlider;

    [Header("Quit")]
    [Tooltip("나가기 때 불러올 씬 이름(예: 로비). 비우면 앱을 종료한다.")]
    public string quitSceneName = "";

    public bool IsPaused { get; private set; }

    void Start()
    {
        if (pausePanel == gameObject)
        {
            Debug.LogWarning("PauseMenu: 일시 정지 창 자체에 붙어 있으면 창을 닫은 뒤 다시 열 수 없습니다. 항상 켜져 있는 오브젝트로 옮기세요.", this);
        }

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
        if (muteButton != null) muteButton.onClick.AddListener(OnMuteButton);
        if (muteToggle != null) muteToggle.onValueChanged.AddListener(OnMuteToggle);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        RefreshSoundUI();
        SetPanelVisible(false);
    }

    void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed += OnPauseAction;
            pauseAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPauseAction;
        }
    }

    void OnDestroy()
    {
        // 일시 정지 상태로 씬이 바뀌거나 종료되어도 시간이 멈춘 채 남지 않게 한다.
        if (IsPaused) Time.timeScale = 1f;
    }

#if UNITY_EDITOR
    void Update()
    {
        // 헤드셋 없이 Editor에서 테스트할 때 Esc로 열고 닫는다.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }
#endif

    private void OnPauseAction(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;
        PlacePanelInFrontOfPlayer();
        RefreshSoundUI();
        SetPanelVisible(true);
    }

    // 이어하기 버튼.
    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;
        SetPanelVisible(false);
        PlayerPrefs.Save();
    }

    // 나가기 버튼.
    public void Quit()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(quitSceneName))
        {
            SceneManager.LoadScene(quitSceneName);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnMuteButton()
    {
        if (SoundManager.instance == null) return;
        SoundManager.instance.ToggleMute();
        RefreshSoundUI();
    }

    private void OnMuteToggle(bool muted)
    {
        if (SoundManager.instance == null) return;
        SoundManager.instance.SetMuted(muted);
        RefreshSoundUI();
    }

    private void OnVolumeChanged(float value)
    {
        if (SoundManager.instance == null) return;

        SoundManager.instance.SetMasterVolume(value);
        // 음소거 중에 볼륨을 올리면 음소거를 푼다.
        if (SoundManager.instance.IsMuted && value > 0f)
        {
            SoundManager.instance.SetMuted(false);
        }
        RefreshSoundUI();
    }

    // UI를 현재 소리 설정에 맞춘다. 이벤트가 다시 호출되지 않도록 WithoutNotify를 쓴다.
    private void RefreshSoundUI()
    {
        if (SoundManager.instance == null) return;

        bool muted = SoundManager.instance.IsMuted;

        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(SoundManager.instance.MasterVolume);
        if (muteToggle != null) muteToggle.SetIsOnWithoutNotify(muted);

        if (muteIcon != null)
        {
            Sprite sprite = muted ? soundOffSprite : soundOnSprite;
            if (sprite != null) muteIcon.sprite = sprite;
        }

        if (muteLabel != null) muteLabel.text = muted ? soundOffText : soundOnText;
    }

    private void SetPanelVisible(bool visible)
    {
        if (pausePanel != null) pausePanel.SetActive(visible);
    }

    // 창을 플레이어가 보는 방향 앞에, 수평을 유지해서 띄운다.
    private void PlacePanelInFrontOfPlayer()
    {
        if (pausePanel == null) return;

        Transform head = playerHead;
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (head == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
        forward.Normalize();

        Vector3 position = head.position + forward * panelDistance + Vector3.up * panelHeightOffset;
        // World Space Canvas는 +Z 방향에서 바라볼 때 정면이 보이므로 플레이어 시선 방향을 그대로 쓴다.
        pausePanel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
    }
}
