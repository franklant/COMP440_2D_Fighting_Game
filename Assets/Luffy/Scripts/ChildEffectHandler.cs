using UnityEngine;

public class ChildEffectHandler : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer sr;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Start invisible
        sr.enabled = false;
    }

    public void PlayEffect(int actionID, Vector3 localOffset)
    {
        // 1. Move to the right spot (relative to Luffy)
        transform.localPosition = localOffset;

        // 2. Turn on the visual
        sr.enabled = true;

        // 3. Play the animation from the start (0f)
        anim.Play($"Action_{actionID}", -1, 0f);
    }

    void Update()
    {
        // If the sprite is visible, check if animation is done
        if (sr.enabled)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            
            // If animation finished (normalizedTime >= 1.0)
            if (info.normalizedTime >= 1.0f)
            {
                // Hide again
                sr.enabled = false;
            }
        }
    }
}