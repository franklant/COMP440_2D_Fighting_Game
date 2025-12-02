using UnityEngine;
using UnityEngine.Video;

public class VideoEndDeactivate : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer; // Assign your VideoPlayer component here

    void Start()
    {
        // If not assigned, automatically find the VideoPlayer on this GameObject
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to the event that triggers when the video finishes
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoEnd;
    }

    // Called automatically when the video finishes playing
    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video finished! Deactivating object...");
        gameObject.SetActive(false);
    }
}
