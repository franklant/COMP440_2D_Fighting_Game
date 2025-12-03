using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    public float scrollSpeed = 0.5f; // Speed of background movement
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // Remember the original position
    }

    void Update()
    {
        // Move the background down over time, then loop
        float newY = Mathf.Repeat(Time.time * scrollSpeed, 10f); // 10 = height of background in world units
        transform.position = startPos + Vector3.down * newY;
    }
}