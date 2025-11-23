using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFlashEffect : MonoBehaviour
{
    public Image flashPanel;
    public float flashSpeed = 10f;

    void Start()
    {
        if (flashPanel == null) flashPanel = GetComponent<Image>();
        // Ensure it starts invisible
        Color c = flashPanel.color;
        c.a = 0;
        flashPanel.color = c;
    }

    public void TriggerFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // 1. Instant Red
        Color c = flashPanel.color;
        c.a = 0.6f; // 60% opacity (Don't go to 1 or you can't see the game!)
        flashPanel.color = c;

        // 2. Fade out fast
        while (c.a > 0)
        {
            c.a -= Time.deltaTime * flashSpeed;
            flashPanel.color = c;
            yield return null;
        }
    }
}