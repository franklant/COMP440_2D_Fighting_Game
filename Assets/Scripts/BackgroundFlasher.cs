using System.Collections;
using UnityEngine;

public class BackgroundFlasher : MonoBehaviour
{
    public SpriteRenderer bgA;
    public SpriteRenderer bgB;

    public float switchDelay = 5f;
    public float fadeSpeed = 2f;
    public float flashDuration = 0.1f;

    private bool showingA = true;

    void Start()
    {
        StartCoroutine(SwapLoop());
    }

    IEnumerator SwapLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchDelay);

            // FLASH EFFECT
            yield return StartCoroutine(FlashScreen());

            // FADE SWITCH
            yield return StartCoroutine(FadeSwap());
        }
    }

    IEnumerator FlashScreen()
    {
        bgA.color = Color.white;
        bgB.color = Color.white;

        yield return new WaitForSeconds(flashDuration);
    }

    IEnumerator FadeSwap()
    {
        SpriteRenderer from = showingA ? bgA : bgB;
        SpriteRenderer to = showingA ? bgB : bgA;

        to.gameObject.SetActive(true);

        float fade = 0f;

        while (fade < 1f)
        {
            fade += Time.deltaTime * fadeSpeed;

            from.color = new Color(1, 1, 1, 1 - fade);
            to.color = new Color(1, 1, 1, fade);

            yield return null;
        }

        from.gameObject.SetActive(false);
        showingA = !showingA;
    }
}