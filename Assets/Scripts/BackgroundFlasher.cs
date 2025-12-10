using System.Collections;
using UnityEngine;

public class BackgroundFlasher : MonoBehaviour
{
    public SpriteRenderer bgA;
    public SpriteRenderer bgB;

    public float switchDelay = 5f;
    public float fadeSpeed = 2f;
    public float flashDuration = 0.1f;

    void Start()
    {
        StartCoroutine(DoSingleSwap());
    }

    IEnumerator DoSingleSwap()
    {
        // Wait before starting transition
        yield return new WaitForSeconds(switchDelay);

        // FLASH EFFECT
        yield return StartCoroutine(FlashScreen());

        // FADE A → B
        yield return StartCoroutine(FadeToB());
    }

    IEnumerator FlashScreen()
    {
        bgA.color = Color.white;
        bgB.color = Color.white;

        yield return new WaitForSeconds(flashDuration);
    }

    IEnumerator FadeToB()
    {
        bgB.gameObject.SetActive(true);

        float fade = 0f;

        while (fade < 1f)
        {
            fade += Time.deltaTime * fadeSpeed;

            bgA.color = new Color(1, 1, 1, 1 - fade);
            bgB.color = new Color(1, 1, 1, fade);

            yield return null;
        }

        // Ensure final state is locked
        bgA.gameObject.SetActive(false);
        bgA.color = new Color(1,1,1,1);
        bgB.color = new Color(1,1,1,1);
    }
}