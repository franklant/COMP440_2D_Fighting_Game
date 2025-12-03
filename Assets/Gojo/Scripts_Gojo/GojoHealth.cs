using UnityEngine;
using System.Collections;

public class GojoHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Header("Components")]
    public Animator animator;
    public GojoMovement movementScript;
    public GojoCombat combatScript;
    public Rigidbody2D rb;

    private bool isDead = false;
    private Coroutine hurtRoutine;

    void Start()
    {
        currentHealth = maxHealth;
        if(rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // --- DEBUG KEY ---
        // Press Z to simulate getting hit
        if (Input.GetKeyDown(KeyCode.Z))
        {
            TakeDamage(50f); // Deal 50 damage to self
        }
    }

    // Call this function when an enemy hitbox touches Gojo
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // 1. Subtract Health
        currentHealth -= damage;
        Debug.Log($"Gojo took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 2. Determine Which Animation to Play
        // Action 5000 is Stand Hit, Action 5030 is Air Hit
        string hitAnim = "Action_5000"; 

        if (movementScript != null)
        {
            if (!movementScript.isGrounded)
            {
                hitAnim = "Action_5030"; // Air Hit
                
                // Optional: Add a little knockback
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    float facing = transform.localScale.x;
                    rb.AddForce(new Vector2(-3f * facing, 5f), ForceMode2D.Impulse);
                }
            }
        }

        // 3. Play Animation & Disable Controls
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtSequence(hitAnim));
    }

    IEnumerator HurtSequence(string clipName)
    {
        // Disable Controls
        if (movementScript) movementScript.enabled = false;
        if (combatScript) combatScript.enabled = false;

        // Stop momentum (unless in air where we applied knockback)
        if (movementScript && movementScript.isGrounded) 
        {
            if(rb) rb.linearVelocity = Vector2.zero;
        }

        // Play Hit Anim
        if(animator) animator.Play(clipName, 0, 0f);

        // Get Duration
        float duration = GetClipDuration(clipName);
        
        // Wait for stun to end
        yield return new WaitForSeconds(duration);

        // Recover
        if (!isDead)
        {
            if (movementScript) movementScript.enabled = true;
            if (combatScript) combatScript.enabled = true;
            if (animator) animator.Play("Action_0"); // Back to Idle
        }
    }

    void Die()
    {
        isDead = true;
        if (movementScript) movementScript.enabled = false;
        if (combatScript) combatScript.enabled = false;
        if (rb) rb.linearVelocity = Vector2.zero;

        // Action 5150 is the "Dead/Lying Down" loop
        if(animator) animator.Play("Action_5150"); 
        Debug.Log("Gojo Defeated.");
    }

    float GetClipDuration(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0.5f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip.length;
        }
        return 0.5f;
    }
}