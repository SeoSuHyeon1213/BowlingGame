using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// 열려 있는 씬에서 게임 스크립트 연결을 채운다. 메뉴: Tools > Bowling > Wire Open Scene
// - 비어 있는 필드만 채우고, 이미 연결된 값은 덮어쓰지 않는다. 여러 번 실행해도 안전하다.
// - 모든 변경은 Undo(Ctrl+Z) 한 번으로 되돌릴 수 있다. 결과는 Console 로그로 확인한 뒤 씬을 저장(Ctrl+S)한다.
public static class BowlingSceneWiring
{
    const string IconFolder = "Assets/ThirdParty/Neon/PackCyanBlue/PackCyanBlue/Png Files/Icons/";
    const string CfxrFolder = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/";
    static readonly string[] ReturnZoneNames = { "Cube", "Cube (2)", "BallCollector" };

    [MenuItem("Tools/Bowling/Wire Open Scene")]
    static void WireOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        StringBuilder log = new StringBuilder($"[BowlingSceneWiring] {scene.name}\n");

        Undo.SetCurrentGroupName("Wire Bowling Scene");
        int undoGroup = Undo.GetCurrentGroup();

        List<BowlingBall> balls = WireBalls(scene, log);
        List<PinSetter> pinSetters = FindInScene<PinSetter>(scene);
        Vector3 laneDir = GetLaneDirection(balls, pinSetters);

        WireReturnZones(scene, balls, laneDir, log);
        GameManager gameManager = WireGameManager(scene, balls, pinSetters, laneDir, log);
        WirePauseMenu(scene, gameManager, log);
        WireEffects(gameManager, log);
        WireTutorialManager(scene, balls, pinSetters, log);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(log.ToString());
    }

    // 공: XRGrabInteractable이 있고 이름이 "Ball"로 시작하는 오브젝트.
    static List<BowlingBall> WireBalls(Scene scene, StringBuilder log)
    {
        List<BowlingBall> balls = new List<BowlingBall>();
        foreach (XRGrabInteractable grab in FindInScene<XRGrabInteractable>(scene).Where(g => g.name.StartsWith("Ball")).OrderBy(g => g.name))
        {
            BowlingBall ball = grab.GetComponent<BowlingBall>();
            if (ball == null)
            {
                ball = Undo.AddComponent<BowlingBall>(grab.gameObject);
                log.AppendLine($"+ BowlingBall added to '{PathOf(grab.transform)}'");
            }

            if (IsEmpty(ball, "returnPoint"))
            {
                // 공이 처음 놓인 자리(소켓 위)를 복귀 위치로 쓴다. 여기로 옮겨지면 소켓이 다시 받는다.
                GameObject point = new GameObject($"{grab.name} Return Point");
                Undo.RegisterCreatedObjectUndo(point, "Create Return Point");
                SceneManager.MoveGameObjectToScene(point, scene);
                point.transform.position = grab.transform.position + Vector3.up * 0.05f;
                SetRef(ball, "returnPoint", point.transform);
                log.AppendLine($"+ '{point.name}' created at the ball's start position → returnPoint (move it if the ball should roll in from elsewhere)");
            }

            balls.Add(ball);
        }

        if (balls.Count == 0) log.AppendLine("! No ball found (XRGrabInteractable named 'Ball…').");
        return balls;
    }

    // 수거 트리거: 루트의 "Cube"(맵 아래), "Cube (2)"(핀 뒤), 그리고 "BallCollector".
    static void WireReturnZones(Scene scene, List<BowlingBall> balls, Vector3 laneDir, StringBuilder log)
    {
        List<Collider> zones = FindInScene<Collider>(scene)
            .Where(c => c.isTrigger && ReturnZoneNames.Contains(c.name) && c.GetComponent<XRSocketInteractor>() == null)
            .Where(c => c.transform.parent == null || c.name == "BallCollector")
            .ToList();

        foreach (Collider zone in zones)
        {
            if (zone.GetComponent<BallReturnZone>() == null)
            {
                Undo.AddComponent<BallReturnZone>(zone.gameObject);
                log.AppendLine($"+ BallReturnZone added to '{PathOf(zone.transform)}'");
            }
        }

        // 맵 아래 낙하 트리거를 빼고, 핀 뒤 수거 트리거가 각 공의 레인을 가로지르는지 확인한다.
        Vector3 side = new Vector3(-laneDir.z, 0f, laneDir.x);
        foreach (BowlingBall ball in balls)
        {
            float s = Vector3.Dot(ball.transform.position, side);
            bool covered = zones.Any(z =>
                z.bounds.center.y > ball.transform.position.y - 3f && LateralMin(z.bounds, side) <= s && s <= LateralMax(z.bounds, side));
            if (!covered)
            {
                log.AppendLine($"! No collector trigger behind the pins covers '{ball.name}'s lane. Widen 'Cube (2)' or add a trigger with BallReturnZone for that lane.");
            }
        }
    }

    static GameManager WireGameManager(Scene scene, List<BowlingBall> balls, List<PinSetter> pinSetters, Vector3 laneDir, StringBuilder log)
    {
        GameManager gm = FindInScene<GameManager>(scene).FirstOrDefault();
        if (gm == null)
        {
            log.AppendLine("! No GameManager in scene.");
            return null;
        }

        BowlingBall mainBall = balls.FirstOrDefault(b => b.name == "Ball") ?? balls.FirstOrDefault();
        if (mainBall != null && SetIfEmpty(gm, "ball", mainBall))
        {
            log.AppendLine($"+ GameManager.ball = '{mainBall.name}'");
        }

        BowlingBall laneBall = gm.ball != null ? gm.ball : mainBall;
        if (IsEmpty(gm, "pinSetter"))
        {
            PinSetter lanePins = laneBall != null ? FindLanePinSetter(laneBall, pinSetters, laneDir) : pinSetters.FirstOrDefault();
            if (lanePins != null)
            {
                SetRef(gm, "pinSetter", lanePins);
                log.AppendLine($"+ GameManager.pinSetter = '{PathOf(lanePins.transform)}' (same lane as '{(laneBall != null ? laneBall.name : "?")}')");
            }
            else
            {
                log.AppendLine("! No PinSetter in scene — scoring is off. Add PinSetter to a 10-pin group's parent and run again.");
            }
        }

        if (IsEmpty(gm, "scoreText"))
        {
            TextMeshProUGUI p1 = FindInScene<TextMeshProUGUI>(scene).FirstOrDefault(t => t.name == "P1");
            if (p1 != null)
            {
                SetRef(gm, "scoreText", p1);
                log.AppendLine("+ GameManager.scoreText = 'P1'");
            }
        }

        return gm;
    }

    static void WirePauseMenu(Scene scene, GameManager gm, StringBuilder log)
    {
        GameObject paused = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Paused");
        if (paused == null)
        {
            log.AppendLine("! No root 'Paused' object — PauseMenu skipped.");
            return;
        }

        PauseMenu menu = FindInScene<PauseMenu>(scene).FirstOrDefault();
        if (menu == null)
        {
            GameObject host = gm != null ? gm.gameObject : new GameObject("PauseMenu");
            if (gm == null) Undo.RegisterCreatedObjectUndo(host, "Create PauseMenu");
            menu = Undo.AddComponent<PauseMenu>(host);
            log.AppendLine($"+ PauseMenu added to '{host.name}' (always-active object, not the panel)");
        }

        if (SetIfEmpty(menu, "pausePanel", paused)) log.AppendLine("+ PauseMenu.pausePanel = 'Paused'");

        WireChild<Button>(menu, "resumeButton", paused, "start", log);
        WireChild<Button>(menu, "quitButton", paused, "quit", log);

        Slider slider = paused.GetComponentInChildren<Slider>(true);
        if (slider != null && SetIfEmpty(menu, "volumeSlider", slider)) log.AppendLine($"+ PauseMenu.volumeSlider = '{slider.name}'");

        // 소리 끄기: Image만 있는 "Button" 오브젝트에 Button 컴포넌트를 붙여 쓴다.
        Transform muteTransform = paused.transform.Find("Button");
        if (muteTransform != null)
        {
            Image icon = muteTransform.GetComponent<Image>();
            Button muteButton = muteTransform.GetComponent<Button>();
            if (muteButton == null)
            {
                muteButton = Undo.AddComponent<Button>(muteTransform.gameObject);
                SetRef(muteButton, "m_TargetGraphic", icon);
                log.AppendLine("+ Button component added to 'Paused/Button' (mute)");
            }
            if (SetIfEmpty(menu, "muteButton", muteButton)) log.AppendLine("+ PauseMenu.muteButton = 'Paused/Button'");
            if (icon != null && SetIfEmpty(menu, "muteIcon", icon)) log.AppendLine("+ PauseMenu.muteIcon = 'Paused/Button' Image");
        }

        WireSprite(menu, "soundOnSprite", IconFolder + "Sound_On_icon.png", log);
        WireSprite(menu, "soundOffSprite", IconFolder + "Sound_Off_icon.png", log);

        // VR에서는 Screen Space Overlay가 보이지 않으므로 World Space로 바꾼다.
        Canvas canvas = paused.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            RectTransform rect = (RectTransform)paused.transform;
            Undo.RecordObject(canvas, "Paused World Space");
            Undo.RecordObject(rect, "Paused World Space");

            Vector2 size = rect.sizeDelta;
            if (size.x < 1f || size.y < 1f) size = new Vector2(1920f, 1080f);

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * (1.2f / size.x); // 폭 1.2m
            if (Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                rect.SetPositionAndRotation(cam.position + forward * 1.5f, Quaternion.LookRotation(forward, Vector3.up));
            }
            log.AppendLine($"+ 'Paused' Canvas → World Space, {size.x}x{size.y} px at scale {rect.localScale.x:0.#####} (≈1.2 m wide), Event Camera = Main Camera");
        }

        // 제목 텍스트에 네온 깜빡임.
        Transform title = paused.transform.Find("Image/Text (TMP)");
        if (title != null && title.GetComponent<NeonFlicker>() == null)
        {
            Undo.AddComponent<NeonFlicker>(title.gameObject);
            log.AppendLine("+ NeonFlicker added to 'Paused/Image/Text (TMP)'");
        }
    }

    // 튜토리얼 씬: TutorialManager의 레인과 메인 게임 버튼("Main Game Start")을 채운다.
    static void WireTutorialManager(Scene scene, List<BowlingBall> balls, List<PinSetter> pinSetters, StringBuilder log)
    {
        TutorialManager tm = FindInScene<TutorialManager>(scene).FirstOrDefault();
        if (tm == null) return;

        if (FindInScene<GameManager>(scene).Any())
        {
            log.AppendLine("! Both GameManager and TutorialManager are in this scene. Remove GameManager — balls report to it first.");
        }

        BowlingBall mainBall = balls.FirstOrDefault(b => b.name == "Ball") ?? balls.FirstOrDefault();
        if (SetIfEmpty(tm, "ball", mainBall)) log.AppendLine($"+ TutorialManager.ball = '{mainBall.name}'");

        PinSetter pins = pinSetters.FirstOrDefault();
        if (pins == null)
        {
            log.AppendLine("! No PinSetter in scene — TutorialManager can't count pins.");
        }
        else if (SetIfEmpty(tm, "pinSetter", pins))
        {
            log.AppendLine($"+ TutorialManager.pinSetter = '{PathOf(pins.transform)}'");
        }

        if (IsEmpty(tm, "mainGameButton"))
        {
            SceneStartButton start = FindInScene<SceneStartButton>(scene).FirstOrDefault();
            GameObject button = start != null ? start.gameObject : null;
            if (button != null)
            {
                SetRef(tm, "mainGameButton", button);
                log.AppendLine($"+ TutorialManager.mainGameButton = '{PathOf(button.transform)}'");
            }
        }
    }

    // 이펙트: GameManager 오브젝트에 BowlingEffects를 붙이고 Cartoon FX Remaster 프리팹을 지정한다.
    static void WireEffects(GameManager gm, StringBuilder log)
    {
        if (gm == null) return;

        BowlingEffects effects = gm.GetComponent<BowlingEffects>();
        if (effects == null)
        {
            effects = Undo.AddComponent<BowlingEffects>(gm.gameObject);
            log.AppendLine($"+ Added BowlingEffects to '{gm.name}'");
        }

        WirePrefab(effects, "strikeEffect", CfxrFolder + "Explosions/CFXR4 Firework 1 Cyan-Purple (HDR).prefab", log);
        WirePrefab(effects, "spareEffect", CfxrFolder + "Texts/CFXR3 _WOW_.prefab", log);
        WirePrefab(effects, "pinHitEffect", CfxrFolder + "Impacts/CFXR Hit D 3D (Yellow).prefab", log);
    }

    static void WirePrefab(Object target, string field, string path, StringBuilder log)
    {
        if (!IsEmpty(target, field)) return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            log.AppendLine($"! Prefab not found at '{path}'. Import Cartoon FX Remaster Free and run again.");
            return;
        }
        SetRef(target, field, prefab);
        log.AppendLine($"+ {target.GetType().Name}.{field} = {System.IO.Path.GetFileNameWithoutExtension(path)}");
    }

    static void WireChild<T>(Object target, string field, GameObject root, string childName, StringBuilder log) where T : Component
    {
        Transform child = root.transform.Find(childName);
        T component = child != null ? child.GetComponent<T>() : null;
        if (component == null)
        {
            log.AppendLine($"! '{root.name}/{childName}' with {typeof(T).Name} not found — {target.GetType().Name}.{field} left empty.");
            return;
        }
        if (SetIfEmpty(target, field, component)) log.AppendLine($"+ {target.GetType().Name}.{field} = '{root.name}/{childName}'");
    }

    static void WireSprite(Object target, string field, string path, StringBuilder log)
    {
        if (!IsEmpty(target, field)) return;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            log.AppendLine($"! Sprite not found at '{path}'. Set its Texture Type to 'Sprite (2D and UI)' and run again.");
            return;
        }
        SetRef(target, field, sprite);
        log.AppendLine($"+ {target.GetType().Name}.{field} = {System.IO.Path.GetFileName(path)}");
    }

    // 레인 방향: 공들의 평균 위치 → 핀들의 평균 위치(수평).
    static Vector3 GetLaneDirection(List<BowlingBall> balls, List<PinSetter> pinSetters)
    {
        if (balls.Count == 0 || pinSetters.Count == 0) return Vector3.right;
        Vector3 ballCenter = Average(balls.Select(b => b.transform.position));
        Vector3 pinCenter = Average(pinSetters.Select(PinCenter));
        Vector3 dir = Vector3.ProjectOnPlane(pinCenter - ballCenter, Vector3.up);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
    }

    // 레인 방향에 수직인 거리가 가장 가까운 핀 묶음.
    static PinSetter FindLanePinSetter(BowlingBall ball, List<PinSetter> pinSetters, Vector3 laneDir)
    {
        Vector3 side = new Vector3(-laneDir.z, 0f, laneDir.x);
        return pinSetters
            .OrderBy(p => Mathf.Abs(Vector3.Dot(PinCenter(p) - ball.transform.position, side)))
            .FirstOrDefault();
    }

    static Vector3 PinCenter(PinSetter setter)
    {
        BowlingPin[] pins = setter.GetComponentsInChildren<BowlingPin>(true);
        return pins.Length > 0 ? Average(pins.Select(p => p.transform.position)) : setter.transform.position;
    }

    static Vector3 Average(IEnumerable<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Vector3 p in points) { sum += p; count++; }
        return count > 0 ? sum / count : Vector3.zero;
    }

    static float LateralMin(Bounds b, Vector3 side) => Corners(b).Min(c => Vector3.Dot(c, side));
    static float LateralMax(Bounds b, Vector3 side) => Corners(b).Max(c => Vector3.Dot(c, side));

    static IEnumerable<Vector3> Corners(Bounds b)
    {
        yield return new Vector3(b.min.x, 0f, b.min.z);
        yield return new Vector3(b.min.x, 0f, b.max.z);
        yield return new Vector3(b.max.x, 0f, b.min.z);
        yield return new Vector3(b.max.x, 0f, b.max.z);
    }

    static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.gameObject.scene == scene)
            .ToList();
    }

    static bool IsEmpty(Object target, string field)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(field);
        return property != null && property.objectReferenceValue == null;
    }

    static bool SetIfEmpty(Object target, string field, Object value)
    {
        if (value == null || !IsEmpty(target, field)) return false;
        SetRef(target, field, value);
        return true;
    }

    // SerializedObject를 거쳐 설정해야 Undo와 프리팹 오버라이드가 올바르게 기록된다.
    static void SetRef(Object target, string field, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogWarning($"[BowlingSceneWiring] Field '{field}' not found on {target.GetType().Name}.", target);
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
