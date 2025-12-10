using UnityEngine;

public class OneShotEffect : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayEffect(int actionID, bool facingRight)
    {
        // 1. Flip sprite if character is facing Left
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        // 2. Play the clip (Must match Animator State Name exactly)
        animator.Play($"Action_{actionID}");
    }

    void Update()
    {
        // 3. Destroy whenever the animation finishes
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime >= 1.0f)
        {
            Destroy(gameObject);
        }
    }
}