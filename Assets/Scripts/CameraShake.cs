using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Store the camera's starting position so we can return to it later
    private Vector3 originalPos;

    public IEnumerator Shake(float duration, float magnitude)
    {
        originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 1. Generate a random offset
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // 2. Move the camera
            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            // 3. Wait for next frame
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4. Reset to original position when done
        transform.localPosition = originalPos;
    }
}