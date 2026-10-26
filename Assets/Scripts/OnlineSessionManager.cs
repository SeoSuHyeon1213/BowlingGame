using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

// 온라인 1:1 접속. Unity Gaming Services에 익명 로그인한 뒤 2인 세션(Relay 네트워크)에 들어가고,
// 두 명이 모이면 호스트가 메인 게임 씬을 네트워크 씬 로드로 함께 연다.
// 같은 오브젝트의 NetworkManager는 세션 생성/참가 때 패키지가 자동으로 Host/Client로 시작한다.
// 1단계 범위: 접속과 아바타 동기화만 한다. 투구·점수는 아직 각 기기에서 따로 진행된다.
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(NetworkManager))]
public class OnlineSessionManager : MonoBehaviour
{
    public enum ConnectionState { Offline, Connecting, Waiting, InGame }

    public static OnlineSessionManager Instance;

    [Tooltip("두 명이 모이면 호스트가 네트워크로 불러올 씬. Build Settings에 들어 있어야 한다.")]
    public string gameSceneName = "BowlingScene";
    [Tooltip("세션을 나가거나 연결이 끊겼을 때 돌아갈 씬(로비 UI가 있는 씬).")]
    public string lobbySceneName = "TutorialScene";
    [Tooltip("세션 인원(호스트 포함).")]
    public int maxPlayers = 2;

    public ConnectionState State { get; private set; } = ConnectionState.Offline;
    public string StatusMessage { get; private set; } = "";
    public string SessionCode => session?.Code;
    public bool IsOnline => State != ConnectionState.Offline;

    public event Action StateChanged;

    // 로비로 돌아오며 오브젝트가 새로 만들어져도 마지막 안내 문구를 이어서 보여 준다.
    private static string pendingMessage;

    private NetworkManager networkManager;
    private ISession session;
    private bool leaving;

    void Awake()
    {
        // 로비 씬을 다시 열면 씬에 있는 사본이 또 만들어진다. 살아 있는 쪽을 남기고 사본은 바로 지워
        // NetworkManager가 둘이 되지 않게 한다.
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Instance = this;
        networkManager = GetComponent<NetworkManager>();
        if (!string.IsNullOrEmpty(pendingMessage))
        {
            StatusMessage = pendingMessage;
            pendingMessage = null;
        }
    }

    void OnEnable()
    {
        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkManager.OnClientStopped += OnClientStopped;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        networkManager.OnClientStopped -= OnClientStopped;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 빈 자리가 있는 세션에 들어가고, 없으면 새로 만들어 상대를 기다린다.
    public async void QuickMatch()
    {
        if (State != ConnectionState.Offline) return;
        SetState(ConnectionState.Connecting, "Connecting...");

        try
        {
            await EnsureSignedInAsync();
            var quickJoin = new QuickJoinOptions { CreateSession = true };
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            session = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoin, options);
            OnSessionJoined();
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    // 비공개 방을 만들고 방 코드(SessionCode)를 상대에게 알려 준다.
    public async void CreatePrivateRoom()
    {
        if (State != ConnectionState.Offline) return;
        SetState(ConnectionState.Connecting, "Creating room...");

        try
        {
            await EnsureSignedInAsync();
            var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            OnSessionJoined();
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    public async void JoinByCode(string code)
    {
        if (State != ConnectionState.Offline || string.IsNullOrWhiteSpace(code)) return;
        SetState(ConnectionState.Connecting, "Joining room...");

        try
        {
            await EnsureSignedInAsync();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim());
            OnSessionJoined();
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    // 대기 취소 또는 게임 중 나가기. 세션을 떠나고 로비 씬으로 돌아간다.
    public void Leave()
    {
        LeaveAndReturnToLobby("Left the match.");
    }

    private async Task EnsureSignedInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions();
#if UNITY_EDITOR
            // 같은 PC에서 Editor 두 개(또는 Multiplayer Play Mode)로 시험할 때 같은 익명 계정으로
            // 같은 세션에 두 번 들어가지 않게, Editor에서는 실행마다 다른 프로필을 쓴다.
            options.SetProfile("editor" + UnityEngine.Random.Range(0, 1000000));
#endif
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void OnSessionJoined()
    {
        session.RemovedFromSession += OnRemovedFromSession;
        session.Deleted += OnRemovedFromSession;

        if (session.IsHost)
        {
            SetState(ConnectionState.Waiting, "Waiting for opponent...");
            TryStartGame();
        }
        else if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            // 참가 처리가 끝나기 전에 호스트의 게임 씬 동기화가 먼저 끝난 경우.
            SetState(ConnectionState.InGame, "Match started!");
        }
        else
        {
            // 클라이언트는 호스트가 게임 씬을 열면 함께 넘어간다.
            SetState(ConnectionState.Waiting, "Joined. Starting soon...");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager.IsServer) TryStartGame();
    }

    private void TryStartGame()
    {
        if (State != ConnectionState.Waiting || !networkManager.IsServer) return;
        if (networkManager.ConnectedClientsIds.Count < maxPlayers) return;

        SetState(ConnectionState.InGame, "Opponent found!");
        networkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (leaving) return;

        // 호스트: 상대가 나가면 1단계에서는 대전을 끝내고 로비로 돌아간다.
        if (networkManager.IsServer && clientId != NetworkManager.ServerClientId && State == ConnectionState.InGame)
        {
            LeaveAndReturnToLobby("Opponent left the match.");
        }
    }

    private void OnClientStopped(bool wasHost)
    {
        // 클라이언트: 호스트와의 연결이 끊김.
        if (!leaving && State != ConnectionState.Offline)
        {
            LeaveAndReturnToLobby("Disconnected from the match.");
        }
    }

    // 일시정지 메뉴 등으로 네트워크를 거치지 않고 로비 씬에 돌아오면 세션도 나간다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 클라이언트는 호스트가 연 게임 씬에 들어온 시점부터 게임 중이다.
        if (State == ConnectionState.Waiting && scene.name == gameSceneName)
        {
            SetState(ConnectionState.InGame, "Match started!");
            return;
        }

        if (!leaving && State != ConnectionState.Offline && scene.name == lobbySceneName && mode == LoadSceneMode.Single)
        {
            LeaveAndReturnToLobby("Left the match.");
        }
    }

    private void OnRemovedFromSession()
    {
        if (!leaving && State != ConnectionState.Offline)
        {
            LeaveAndReturnToLobby("The match was closed.");
        }
    }

    private async void LeaveAndReturnToLobby(string message)
    {
        if (leaving) return;
        leaving = true;

        ISession leavingSession = session;
        session = null;
        if (leavingSession != null)
        {
            leavingSession.RemovedFromSession -= OnRemovedFromSession;
            leavingSession.Deleted -= OnRemovedFromSession;
            try
            {
                await leavingSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OnlineSessionManager: 세션 나가기 실패 — {e.Message}", this);
            }
        }

        if (networkManager != null && networkManager.IsListening) networkManager.Shutdown();

        leaving = false;
        SetState(ConnectionState.Offline, message);

        if (SceneManager.GetActiveScene().name != lobbySceneName && Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            // 네트워크 오브젝트는 DontDestroyOnLoad로 남아 있으므로 지우고, 로비 씬의 새 사본이 이어받게 한다.
            pendingMessage = message;
            Time.timeScale = 1f;
            Destroy(gameObject);
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private void Fail(Exception e)
    {
        Debug.LogWarning($"OnlineSessionManager: 접속 실패 — {e}", this);
        session = null;
        // 먼저 Offline으로 바꿔 Shutdown이 부르는 OnClientStopped가 로비 복귀로 이어지지 않게 한다.
        SetState(ConnectionState.Offline, "Connection failed. Try again.");
        if (networkManager != null && networkManager.IsListening) networkManager.Shutdown();
    }

    private void SetState(ConnectionState state, string message)
    {
        State = state;
        StatusMessage = message;
        StateChanged?.Invoke();
    }
}
