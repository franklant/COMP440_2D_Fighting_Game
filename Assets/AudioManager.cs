using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Audio Clip")]
    public AudioClip background;
    public AudioClip punchSFX;
    public AudioClip kickSFX;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    
    void Update()
    {
        
    }

    public void PlayPunch()
    {
        sfxSource.PlayOneShot(punchSFX);
    }

    public void PlayKick()
    {
        sfxSource.PlayOneShot(kickSFX);
    }
}
