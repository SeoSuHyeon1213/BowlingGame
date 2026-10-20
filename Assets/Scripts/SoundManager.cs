using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource bgmAudioSource;
    public AudioSource sfxAudioSource;

    public AudioClip bgmClip;
    public AudioClip hitClip;

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

    public void ToggleBGM()
    {
        if (bgmAudioSource.isPlaying)
            bgmAudioSource.Pause();
        else
            bgmAudioSource.Play();

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



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
