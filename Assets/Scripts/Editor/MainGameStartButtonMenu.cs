using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 튜토리얼 씬에 "메인 게임 시작" 버튼을 만들고, 빌드 씬 순서를 튜토리얼 → 메인 게임으로 맞춘다.
// 메뉴: Tools > Bowling > Create Main Game Start Button / Set Build Order (Tutorial → Bowling)
public static class MainGameStartButtonMenu
{
    const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
    const string MainScenePath = "Assets/Scenes/BowlingScene.unity";
    const string MainSceneName = "BowlingScene";
    const string StartSpritePath = "Assets/ThirdParty/Neon/PackCyanBlue/PackCyanBlue/Png Files/Buttons/Medium/Start_button.png";
    const string RootName = "Main Game Start";

    static readonly Color NeonPink = new Color(1f, 0.18f, 0.53f, 1f);
    static readonly Color NeonCyan = new Color(0f, 0.94f, 1f, 1f);

    [MenuItem("Tools/Bowling/Create Main Game Start Button")]
    static void CreateStartButton()
    {
        Scene scene = SceneManager.GetActiveScene();

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log($"[MainGameStartButton] '{RootName}' already exists in {scene.name} — selected it.", existing);
            return;
        }

        // World Space Canvas (폭 0.6m). VR 레이로 누를 수 있게 TrackedDeviceGraphicRaycaster를 붙인다.
        GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create Main Game Start Button");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(600f, 260f);
        rootRect.localScale = Vector3.one * 0.001f;
        PlaceInFrontOfCamera(rootRect);

        // 위쪽 안내 문구.
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(NeonFlicker));
        label.transform.SetParent(root.transform, false);
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(0f, 0.6f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        labelText.text = "MAIN GAME";
        labelText.fontSize = 72f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = NeonPink;

        // 시작 버튼.
        GameObject buttonObject = new GameObject("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(root.transform, false);
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(512f, 128f);
        buttonRect.anchoredPosition = new Vector2(0f, 10f);

        Image image = buttonObject.GetComponent<Image>();
        Sprite startSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StartSpritePath);
        if (startSprite != null)
        {
            image.sprite = startSprite;
            image.preserveAspect = true;
        }
        else
        {
            // 스프라이트가 아직 Sprite 타입으로 임포트되지 않았으면 기본 사각형 + 글자로 만든다.
            image.color = new Color(0.04f, 0.02f, 0.13f, 0.9f);
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = NeonCyan;
            outline.effectDistance = new Vector2(4f, -4f);

            GameObject textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "START";
            text.fontSize = 64f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = NeonCyan;
            Debug.LogWarning($"[MainGameStartButton] Sprite not found at '{StartSpritePath}' (set Texture Type to 'Sprite (2D and UI)'). Used a plain button instead.");
        }

        // 호버/클릭 때 밝기 변화.
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = NeonPink;
        colors.selectedColor = Color.white;
        button.colors = colors;

        SceneStartButton starter = buttonObject.AddComponent<SceneStartButton>();
        starter.sceneName = MainSceneName;

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[MainGameStartButton] Created '{RootName}' in {scene.name} in front of the Main Camera. Move it where the player can reach it, then save the scene (Ctrl+S).", root);

        if (!IsInBuild(MainScenePath))
        {
            Debug.LogWarning("[MainGameStartButton] BowlingScene is not enabled in Build Settings — run Tools > Bowling > Set Build Order (Tutorial → Bowling).");
        }
    }

    // 튜토리얼을 첫 씬(앱 시작 씬)으로, 메인 게임을 두 번째로 둔다. 나머지 씬은 그 뒤에 원래 상태대로 남긴다.
    [MenuItem("Tools/Bowling/Set Build Order (Tutorial → Bowling)")]
    static void SetBuildOrder()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(TutorialScenePath, true),
            new EditorBuildSettingsScene(MainScenePath, true),
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != TutorialScenePath && s.path != MainScenePath));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[MainGameStartButton] Build Settings scene list: " +
                  string.Join(", ", scenes.Select((s, i) => $"{i}:{System.IO.Path.GetFileNameWithoutExtension(s.path)}{(s.enabled ? "" : "(off)")}")));
    }

    static bool IsInBuild(string path)
    {
        return EditorBuildSettings.scenes.Any(s => s.enabled && s.path == path);
    }

    static void PlaceInFrontOfCamera(RectTransform rect)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        // 레인을 가리지 않게 정면에서 약간 오른쪽, 눈높이보다 조금 아래.
        rect.SetPositionAndRotation(cam.transform.position + forward * 1.5f + right * 0.7f + Vector3.down * 0.3f,
                                    Quaternion.LookRotation(forward, Vector3.up));
    }
}
