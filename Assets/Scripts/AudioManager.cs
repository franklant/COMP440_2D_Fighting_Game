using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    public AudioClip backgroundMusic;

    [Header("SFX Pool")]
    public int poolSize = 12;
    private List<AudioSource> sfxPool;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Punch Sounds (Randomized)")]
    public AudioClip[] punchSFX;

    [Header("Kick Sounds (Randomized)")]
    public AudioClip[] kickSFX;

    [Header("Hit Sounds")]
    public AudioClip[] hitSFX; 

    [Header("Whoosh Sounds (Whiffs)")]
    public AudioClip[] whooshSFX; 

    [Header("Block Sounds")]
    public AudioClip[] blockSFX;

    [Header("Pitch Variation")]
    public float minPitch = 0.90f;
    public float maxPitch = 1.10f;


    private void Awake()
    {
        
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        SetupMusic();
        CreateSFXPool();
    }


    private void SetupMusic()
    {
        musicSource.volume = musicVolume;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }


    private void CreateSFXPool()
    {
        sfxPool = new List<AudioSource>();

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxPool.Add(src);
        }
    }


    private AudioSource GetFreeSource()
    {
        foreach (AudioSource src in sfxPool)
            if (!src.isPlaying)
                return src;

        // Auto expand
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        sfxPool.Add(newSource);
        return newSource;
    }


    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips.Length == 0) return;

        AudioClip chosen = clips[Random.Range(0, clips.Length)];

        AudioSource src = GetFreeSource();
        src.volume = sfxVolume;
        src.pitch = Random.Range(minPitch, maxPitch);
        src.PlayOneShot(chosen);
    }

    // ---- Public API ----
    public void PlayPunch() => PlayRandomClip(punchSFX);
    public void PlayKick() => PlayRandomClip(kickSFX);
    public void PlayHit() => PlayRandomClip(hitSFX);
    public void PlayWhoosh() => PlayRandomClip(whooshSFX);
    public void PlayBlock() => PlayRandomClip(blockSFX);

    // For UI sliders
    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        musicSource.volume = v;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
    }
}

