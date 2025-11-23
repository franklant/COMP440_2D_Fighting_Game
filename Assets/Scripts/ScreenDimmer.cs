using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenDimmer : MonoBehaviour
{
    public Image dimPanel;
    public float fadeSpeed = 5f;
    public float maxDarkness = 0.7f; // 0 is clear, 1 is pitch black

    void Start()
    {
        if (dimPanel == null) dimPanel = GetComponent<Image>();
        
        // Ensure invisible on start
        Color c = dimPanel.color;
        c.a = 0;
        dimPanel.color = c;
    }

    // Call this with how long you want the screen to stay dark (e.g., 1.0 second)
    public void TriggerDim(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(DimRoutine(duration));
    }

    IEnumerator DimRoutine(float duration)
    {
        Color c = dimPanel.color;

        // 1. Fade In (Darken)
        while (c.a < maxDarkness)
        {
            c.a += Time.deltaTime * fadeSpeed;
            dimPanel.color = c;
            yield return null;
        }

        // 2. Hold the Darkness (Wait for the super move to happen)
        yield return new WaitForSeconds(duration);

        // 3. Fade Out (Brighten)
        while (c.a > 0)
        {
            c.a -= Time.deltaTime * fadeSpeed;
            dimPanel.color = c;
            yield return null;
        }
    }
}