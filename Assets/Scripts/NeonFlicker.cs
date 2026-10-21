using UnityEngine;
using UnityEngine.UI;

// 80년대 네온사인 연출. UI Image, TextMeshPro 텍스트(둘 다 Graphic), SpriteRenderer에 붙인다.
// 밝기가 천천히 숨 쉬듯 변하고(pulse), 가끔 전기가 끊긴 것처럼 깜빡인다(flicker).
// 일시 정지(Time.timeScale = 0) 중에도 움직이도록 기본으로 unscaled time을 쓴다.
// Button의 Color Tint 전환은 CanvasRenderer 색을 따로 곱하므로 함께 써도 충돌하지 않는다.
public class NeonFlicker : MonoBehaviour
{
    [Header("Color")]
    [Tooltip("켜면 시작할 때 오브젝트의 현재 색을 네온 색으로 쓴다.")]
    public bool useCurrentColor = true;
    public Color neonColor = new Color(0f, 0.94f, 1f, 1f);

    [Header("Pulse")]
    [Tooltip("초당 밝기 변화 횟수.")]
    public float pulseSpeed = 0.8f;
    [Tooltip("밝기가 줄어드는 최대 비율(0 = 변화 없음).")]
    [Range(0f, 1f)] public float pulseAmount = 0.12f;

    [Header("Flicker")]
    [Tooltip("초당 깜빡임이 일어날 확률. 0이면 깜빡이지 않는다.")]
    [Range(0f, 5f)] public float flickersPerSecond = 0.15f;
    [Tooltip("깜빡일 때 밝기(0 = 완전히 꺼짐).")]
    [Range(0f, 1f)] public float flickerDimLevel = 0.2f;
    public Vector2 flickerDuration = new Vector2(0.04f, 0.12f);
    [Tooltip("한 번 깜빡일 때 연달아 깜빡이는 최대 횟수.")]
    [Range(1, 4)] public int maxBurst = 3;

    public bool useUnscaledTime = true;

    private Graphic graphic;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float phase;
    private float dimUntil;
    private float nextBurstAt;
    private int burstLeft;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (useCurrentColor)
        {
            if (graphic != null) neonColor = graphic.color;
            else if (spriteRenderer != null) neonColor = spriteRenderer.color;
        }
        baseColor = neonColor;

        // 여러 간판이 똑같이 움직이지 않게 위상을 흩뜨린다.
        phase = Random.value * 10f;
    }

    void OnDisable()
    {
        ApplyColor(baseColor);
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        float wave = 0.5f + 0.5f * Mathf.Sin((t + phase) * pulseSpeed * Mathf.PI * 2f);
        float intensity = 1f - pulseAmount * wave;

        if (t < dimUntil)
        {
            intensity *= flickerDimLevel;
        }
        else if (burstLeft > 0 && t >= nextBurstAt)
        {
            StartDim(t);
        }
        else if (burstLeft == 0 && flickersPerSecond > 0f && Random.value < flickersPerSecond * dt)
        {
            burstLeft = Random.Range(1, maxBurst + 1);
            StartDim(t);
        }

        ApplyColor(new Color(baseColor.r * intensity, baseColor.g * intensity, baseColor.b * intensity, baseColor.a));
    }

    private void StartDim(float t)
    {
        float duration = Random.Range(flickerDuration.x, flickerDuration.y);
        dimUntil = t + duration;
        nextBurstAt = dimUntil + Random.Range(flickerDuration.x, flickerDuration.y);
        burstLeft--;
    }

    // 네온 색을 바꾼다(예: 스트라이크 때 핑크로).
    public void SetNeonColor(Color color)
    {
        baseColor = color;
        neonColor = color;
    }

    private void ApplyColor(Color color)
    {
        if (graphic != null) graphic.color = color;
        else if (spriteRenderer != null) spriteRenderer.color = color;
    }
}
