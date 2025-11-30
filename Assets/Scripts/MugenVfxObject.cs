using UnityEngine;
using System.Collections.Generic;

public class MugenVfxObject : MonoBehaviour
{
    private SpriteRenderer sr;
    private Animator anim;
    private bool isInitialized = false;
    
    // New physics variables
    private Vector3 moveVelocity;
    
    public void Setup(AnimationClip clip, bool flipX, int sortingOrder, Vector3 velocity, bool isAdditive)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        anim = gameObject.AddComponent<Animator>();

        // 1. Controller Setup
        RuntimeAnimatorController baseController = Resources.Load<RuntimeAnimatorController>("VFX_EmptyController");
        if (baseController == null || baseController.animationClips.Length == 0)
        {
            Debug.LogError("CRITICAL: Missing or Empty 'VFX_EmptyController'!");
            Destroy(gameObject); 
            return;
        }

        AnimatorOverrideController aoc = new AnimatorOverrideController(baseController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(baseController.animationClips[0], clip));
        aoc.ApplyOverrides(overrides);
        anim.runtimeAnimatorController = aoc;
        
        // 2. Visual Setup
        sr.flipX = flipX;
        sr.sortingOrder = sortingOrder; 

        // 3. Apply Additive Transparency (Glow)
        if (isAdditive)
        {
            // Uses the standard Particles shader for additive glow
            sr.material = new Material(Shader.Find("Particles/Standard Unlit"));
            sr.material.SetFloat("_Mode", 1); // Additive mode often varies by shader, this is a generic attempt
            // A safer fallback for 2D sprites is usually just the default "Sprites/Default" 
            // but changing color/alpha. For true MUGEN "Add", you might need a custom material.
            // For now, let's tint it slightly to show it's special.
            sr.color = new Color(1, 1, 1, 0.8f); 
        }

        // 4. Physics Setup
        // If player is flipped, flip the X velocity too
        moveVelocity = flipX ? new Vector3(-velocity.x, velocity.y, 0) : velocity;

        isInitialized = true;
    }

    public void SpawnVFX(string id) { } // Dummy to catch nested events

    void Update()
    {
        if (!isInitialized) return;

        // Apply Movement (Velocity)
        transform.position += moveVelocity * Time.deltaTime;

        // Destroy when animation ends
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
        {
            Destroy(gameObject);
        }
    }
}