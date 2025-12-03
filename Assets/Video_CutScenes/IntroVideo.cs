using UnityEngine;
using UnityEngine.Video;

public class VideoEndDeactivate : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;

    [Header("Audio Settings")]
    public AudioSource mainMenuMusic; // Assign the separate music AudioSource here

    void Start()
    {
        // 1. Setup Video
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoEnd;

        // 2. Ensure music isn't playing while video is running
        if (mainMenuMusic != null)
        {
            mainMenuMusic.Stop(); // Ensures silence at the start
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video finished! Switching to Music...");

        // 1. Start the music
        if (mainMenuMusic != null)
        {
            mainMenuMusic.Play();
        }

        // 2. Deactivate the video object
        // (Since the music is on a different object, it keeps playing)
        gameObject.SetActive(false);
    }
}