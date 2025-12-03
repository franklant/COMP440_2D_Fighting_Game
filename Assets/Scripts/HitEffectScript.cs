using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class HitEffectScript : MonoBehaviour
{
    [Header("Hit Effect Attributes")]
    public float finalScale = 2.5f;
    public float scaleSpeed = 1;
    public float scaleDuration = 1f;
    public float scaleTime = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // start with no scale
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        // or if the scale reaches it's max height
        if (scaleTime < scaleDuration)
        {

            if (transform.localScale.x < finalScale)
                transform.localScale += new Vector3(scaleSpeed, scaleSpeed, 0) * Time.deltaTime;

            scaleTime += Time.deltaTime;

        } else
        {
            scaleTime = 0;
            Destroy(gameObject);    // destroy this once time runs out
        }
    }
}
