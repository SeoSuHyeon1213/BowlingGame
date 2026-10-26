using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 온라인 1:1(1단계: 접속 + 아바타 동기화)에 필요한 프리팹과 씬 오브젝트를 만든다.
// 메뉴: Tools > Bowling > Online > Create Avatar Prefab / Add Online Lobby To Open Scene / Add Spawn Points To Open Scene
// 이미 있으면 새로 만들지 않고 선택만 한다. 실행 후 씬 저장 필요.
public static class OnlineMultiplayerMenu
{
    const string AvatarPrefabPath = "Assets/PreFab/NetworkAvatar.prefab";
    const string SessionRootName = "Online Session";
    const string LobbyRootName = "Online Lobby";
    const string SpawnRootName = "Online Spawn Points";
    const string MainGameStartName = "Main Game Start";

    static readonly Color NeonPink = new Color(1f, 0.18f, 0.53f, 1f);
    static readonly Color NeonCyan = new Color(0f, 0.94f, 1f, 1f);

    [MenuItem("Tools/Bowling/Online/Create Avatar Prefab")]
    static void CreateAvatarPrefabMenu()
    {
        Selection.activeObject = GetOrCreateAvatarPrefab();
    }

    // 로비 씬(튜토리얼 씬)에 NetworkManager + 세션 매니저 + 온라인 버튼 패널을 만든다.
    [MenuItem("Tools/Bowling/Online/Add Online Lobby To Open Scene")]
    static void AddOnlineLobby()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject avatarPrefab = GetOrCreateAvatarPrefab();

        GameObject sessionRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == SessionRootName);
        if (sessionRoot == null)
        {
            sessionRoot = new GameObject(SessionRootName);
            Undo.RegisterCreatedObjectUndo(sessionRoot, "Add Online Session");
            NetworkManager networkManager = sessionRoot.AddComponent<NetworkManager>();
            UnityTransport transport = sessionRoot.AddComponent<UnityTransport>();
            sessionRoot.AddComponent<OnlineSessionManager>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = avatarPrefab;
            networkManager.NetworkConfig.EnableSceneManagement = true;
            Debug.Log($"[OnlineMultiplayer] Created '{SessionRootName}' (NetworkManager + UnityTransport + OnlineSessionManager).", sessionRoot);
        }
        else
        {
            NetworkManager networkManager = sessionRoot.GetComponent<NetworkManager>();
            if (networkManager != null && networkManager.NetworkConfig.PlayerPrefab == null)
            {
                Undo.RecordObject(networkManager, "Assign Player Prefab");
                networkManager.NetworkConfig.PlayerPrefab = avatarPrefab;
            }
            Debug.Log($"[OnlineMultiplayer] '{SessionRootName}' already exists — kept it.", sessionRoot);
        }

        GameObject lobbyRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == LobbyRootName);
        if (lobbyRoot == null)
        {
            lobbyRoot = CreateLobbyCanvas(scene);
            Debug.Log($"[OnlineMultiplayer] Created '{LobbyRootName}' panel. Move it where the player can reach it.", lobbyRoot);
        }
        else
        {
            Debug.Log($"[OnlineMultiplayer] '{LobbyRootName}' already exists — kept it.", lobbyRoot);
        }

        EnsureScenesInBuild();
        Selection.activeGameObject = lobbyRoot;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // 게임 씬에 두 플레이어 자리를 만든다. 0번은 현재 XR Origin 자리, 1번은 그 오른쪽 1m.
    [MenuItem("Tools/Bowling/Online/Add Spawn Points To Open Scene")]
    static void AddSpawnPoints()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == SpawnRootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log($"[OnlineMultiplayer] '{SpawnRootName}' already exists in {scene.name} — selected it.", existing);
            return;
        }

        Transform rig = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>()?.transform;
        Vector3 basePosition = rig != null ? rig.position : Vector3.zero;
        Quaternion baseRotation = rig != null ? Quaternion.LookRotation(Vector3.ProjectOnPlane(rig.forward, Vector3.up), Vector3.up) : Quaternion.identity;

        GameObject root = new GameObject(SpawnRootName);
        Undo.RegisterCreatedObjectUndo(root, "Add Online Spawn Points");
        OnlineSpawnPoints spawnPoints = root.AddComponent<OnlineSpawnPoints>();

        Transform host = new GameObject("Host Spot").transform;
        host.SetParent(root.transform, false);
        host.SetPositionAndRotation(basePosition, baseRotation);
        Transform guest = new GameObject("Guest Spot").transform;
        guest.SetParent(root.transform, false);
        guest.SetPositionAndRotation(basePosition + baseRotation * Vector3.right * 1f, baseRotation);
        spawnPoints.points = new[] { host, guest };

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[OnlineMultiplayer] Created '{SpawnRootName}' in {scene.name}. Adjust 'Guest Spot' so the two players do not overlap, then save the scene.", root);
    }

    static GameObject GetOrCreateAvatarPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabPath);
        if (existing != null) return existing;

        GameObject root = new GameObject("NetworkAvatar");
        root.AddComponent<NetworkObject>();
        NetworkedXRAvatar avatar = root.AddComponent<NetworkedXRAvatar>();

        // 충돌체가 공·핀을 건드리지 않게 모양만 남긴다.
        Transform head = CreateShape(PrimitiveType.Sphere, "Head", root.transform, Vector3.one * 0.22f);
        Transform visor = CreateShape(PrimitiveType.Cube, "Visor", head, new Vector3(0.8f, 0.3f, 0.25f));
        visor.localPosition = new Vector3(0f, 0.05f, 0.42f);
        avatar.head = head;
        avatar.leftHand = CreateShape(PrimitiveType.Sphere, "Left Hand", root.transform, Vector3.one * 0.08f);
        avatar.rightHand = CreateShape(PrimitiveType.Sphere, "Right Hand", root.transform, Vector3.one * 0.08f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AvatarPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[OnlineMultiplayer] Created avatar prefab at {AvatarPrefabPath}.", prefab);
        return prefab;
    }

    static Transform CreateShape(PrimitiveType type, string name, Transform parent, Vector3 scale)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        Object.DestroyImmediate(shape.GetComponent<Collider>());
        shape.transform.SetParent(parent, false);
        shape.transform.localScale = scale;
        return shape.transform;
    }

    static GameObject CreateLobbyCanvas(Scene scene)
    {
        GameObject root = new GameObject(LobbyRootName, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster), typeof(OnlineLobbyPanel));
        Undo.RegisterCreatedObjectUndo(root, "Add Online Lobby");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(600f, 420f);
        rootRect.localScale = Vector3.one * 0.001f;
        Place(scene, rootRect);

        CreateText(root.transform, "Label", "ONLINE 1:1", 72f, NeonPink, new Vector2(0f, 0.78f), new Vector2(1f, 1f));
        TextMeshProUGUI status = CreateText(root.transform, "Status", "Play 1:1 online", 36f, Color.white, new Vector2(0f, 0.6f), new Vector2(1f, 0.78f));

        OnlineLobbyPanel panel = root.GetComponent<OnlineLobbyPanel>();
        panel.statusText = status;
        panel.quickMatchButton = CreateButton(root.transform, "Quick Match Button", "QUICK MATCH", new Vector2(0f, 120f));
        panel.cancelButton = CreateButton(root.transform, "Cancel Button", "CANCEL", new Vector2(0f, 10f));

        // 접속 중에 혼자 메인 게임으로 넘어가지 않게 기존 시작 버튼을 막는다.
        GameObject mainStart = scene.GetRootGameObjects().FirstOrDefault(g => g.name == MainGameStartName);
        Button startButton = mainStart != null ? mainStart.GetComponentInChildren<Button>(true) : null;
        if (startButton != null) panel.disableWhileOnline = new[] { startButton };

        return root;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(480f, 96f);
        rect.anchoredPosition = position;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.04f, 0.02f, 0.13f, 0.9f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = NeonCyan;
        outline.effectDistance = new Vector2(4f, -4f);

        CreateText(buttonObject.transform, "Text (TMP)", label, 48f, NeonCyan, Vector2.zero, Vector2.one);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = NeonPink;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        button.colors = colors;
        return button;
    }

    // 메인 게임 시작 버튼이 있으면 그 오른쪽, 없으면 카메라 앞에 둔다.
    static void Place(Scene scene, RectTransform rect)
    {
        GameObject mainStart = scene.GetRootGameObjects().FirstOrDefault(g => g.name == MainGameStartName);
        if (mainStart != null)
        {
            Transform t = mainStart.transform;
            rect.SetPositionAndRotation(t.position + t.right * 0.75f, t.rotation);
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        rect.SetPositionAndRotation(cam.transform.position + forward * 1.5f + Vector3.down * 0.3f,
                                    Quaternion.LookRotation(forward, Vector3.up));
    }

    // 네트워크 씬 로드는 Build Settings에 있는 씬만 열 수 있다. 빠져 있으면 기존 빌드 순서 메뉴로 맞춘다.
    static void EnsureScenesInBuild()
    {
        bool tutorial = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/TutorialScene.unity");
        bool main = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/BowlingScene.unity");
        if (!tutorial || !main)
        {
            EditorApplication.ExecuteMenuItem("Tools/Bowling/Set Build Order (Tutorial → Bowling)");
        }
    }
}
