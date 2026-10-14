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



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
