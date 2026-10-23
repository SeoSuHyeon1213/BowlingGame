using UnityEngine;

// 투구 결과와 충돌에 맞춰 파티클 이펙트(Cartoon FX Remaster 프리팹 등)를 띄운다.
// GameManager와 같은 오브젝트에 붙이고 프리팹을 지정한다. 비어 있는 이펙트는 건너뛴다.
public class BowlingEffects : MonoBehaviour
{
    public static BowlingEffects Instance;

    [Header("Prefabs")]
    [Tooltip("스트라이크 때 핀 위치에 띄우는 이펙트.")]
    public GameObject strikeEffect;
    [Tooltip("스페어 때 핀 위치에 띄우는 이펙트.")]
    public GameObject spareEffect;
    [Tooltip("공이 핀에 부딪힌 지점에 띄우는 이펙트.")]
    public GameObject pinHitEffect;

    [Header("Placement")]
    [Tooltip("스트라이크/스페어 이펙트를 핀 중심에서 이만큼(m) 위에 띄운다.")]
    public float resultEffectHeight = 0.6f;
    [Tooltip("핀 충돌 이펙트가 연달아 나오지 않도록 두는 최소 간격(초).")]
    public float pinHitCooldown = 0.3f;
    [Tooltip("이펙트가 스스로 지워지지 않을 때를 대비해 이 시간(초) 뒤에 삭제한다.")]
    public float maxEffectLifetime = 5f;

    private float lastPinHitTime = -999f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void PlayStrike(Vector3 pinCenter)
    {
        Spawn(strikeEffect, pinCenter + Vector3.up * resultEffectHeight);
    }

    public void PlaySpare(Vector3 pinCenter)
    {
        Spawn(spareEffect, pinCenter + Vector3.up * resultEffectHeight);
    }

    public void PlayPinHit(Vector3 point)
    {
        if (Time.time - lastPinHitTime < pinHitCooldown) return;
        lastPinHitTime = Time.time;
        Spawn(pinHitEffect, point);
    }

    private void Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, position, prefab.transform.rotation);
        // CFXR_Effect가 붙은 프리팹은 끝나면 스스로 삭제되지만, 텍스트 이펙트 등은 그렇지 않다.
        Destroy(effect, maxEffectLifetime);
    }
}
