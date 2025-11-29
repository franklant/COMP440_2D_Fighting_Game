using UnityEngine;

public class MugenVfxObject : MonoBehaviour
{
    private SpriteRenderer sr;
    private Animator anim;

    public void Setup(AnimationClip clip, bool flipX, int sortingOrder)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        anim = gameObject.AddComponent<Animator>();

        // 1. Setup Visuals
        sr.flipX = flipX;
        sr.sortingOrder = sortingOrder; // Higher number = Layers on top

        // 2. Create a temporary override to play the specific clip
        // We create a new Controller at runtime to avoid making 100 controllers manually
        AnimatorOverrideController aoc = new AnimatorOverrideController();
        aoc.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("VFX_EmptyController");
        aoc["Default"] = clip;
        
        anim.runtimeAnimatorController = aoc;
    }

    void Update()
    {
        // 3. Destroy self when animation finishes
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
        {
            Destroy(gameObject);
        }
    }
}