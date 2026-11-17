using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource bgmAudioSource;
    public AudioSource sfxAudioSource;

    public AudioClip bgmClip;
    [Tooltip("핀이 넘어질 때 나는 소리(BowlingPin이 재생).")]
    public AudioClip hitClip;

    [Tooltip("핀이 동시에 여러 개 넘어질 때 소리가 겹쳐 커지지 않도록 두는 최소 간격(초).")]
    public float pinFallInterval = 0.06f;

    public static SoundManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            LoadAudioSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 배경음악 켜기/끄기. 일시 정지 창의 배경음악 토글에서 쓰고, PlayerPrefs에 저장해 다음 실행에도 유지한다.
    private const string BgmKey = "BgmEnabled";

    public bool BgmEnabled { get; private set; } = true;

    public void ToggleBGM()
    {
        SetBgmEnabled(!BgmEnabled);
    }

    public void SetBgmEnabled(bool enabled)
    {
        BgmEnabled = enabled;
        PlayerPrefs.SetInt(BgmKey, enabled ? 1 : 0);
        ApplyBgm();
    }

    private void ApplyBgm()
    {
        if (bgmAudioSource == null) return;

        if (bgmClip != null && bgmAudioSource.clip != bgmClip) bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;

        if (BgmEnabled && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
        else if (!BgmEnabled && bgmAudioSource.isPlaying) bgmAudioSource.Stop();
    }

    private float lastPinFallTime = -999f;

    public void PlayPinFall()
    {
        if (Time.time - lastPinFallTime < pinFallInterval) return;
        lastPinFallTime = Time.time;
        PlaySFX(hitClip);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    // 전체 볼륨/음소거. AudioListener에 적용하므로 BGM, 효과음, 그 밖의 모든 소리에 함께 반영된다.
    // 값은 PlayerPrefs에 저장해 다음 실행에도 유지한다.
    private const string VolumeKey = "MasterVolume";
    private const string MutedKey = "MasterMuted";

    public float MasterVolume { get; private set; } = 1f;
    public bool IsMuted { get; private set; } = false;

    public void LoadAudioSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        IsMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        BgmEnabled = PlayerPrefs.GetInt(BgmKey, 1) == 1;
        ApplyAudioSettings();
    }

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        ApplyAudioSettings();
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        ApplyAudioSettings();
    }

    public void ToggleMute()
    {
        SetMuted(!IsMuted);
    }

    private void ApplyAudioSettings()
    {
        AudioListener.volume = IsMuted ? 0f : MasterVolume;
    }


    void Start()
    {
        if (instance == this) ApplyBgm();
    }
}
