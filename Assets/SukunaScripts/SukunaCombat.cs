using UnityEngine;

public class SukunaCombat : MonoBehaviour
{
    public Animator animator;
    
    [Header("Combat State")]
    public bool debugMode = true; // Uncheck this later! acts as "Always Hit" for testing
    public bool hasHitEnemy = false; // Becomes TRUE when hitbox touches enemy
    public bool isAttacking = false;

    void Update()
    {
        // 1. Check for Light Attack Input (Mac Key 'A')
        if (Input.GetKeyDown(KeyCode.A)) 
        {
            AttemptAttack();
        }
    }

    void AttemptAttack()
    {
        // LOGIC: If we are already attacking, we only continue if we hit something.
        // If we are Idle, we can always start the first punch.
        
        bool isFirstPunch = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") || 
                            animator.GetCurrentAnimatorStateInfo(0).IsName("WalkForward");

        if (isFirstPunch)
        {
            // Always allow the starter
            TriggerAttack();
        }
        else if (hasHitEnemy || debugMode)
        {
            // Allow the combo chain ONLY if we connected (or are debugging)
            TriggerAttack();
        }
        else
        {
            Debug.Log("Combo Whiffed! Cannot continue.");
        }
    }

    void TriggerAttack()
    {
        animator.SetTrigger("AttackInput");
        
        // Reset the hit confirmation immediately so we have to earn the NEXT hit
        hasHitEnemy = false; 
    }

    // This function will be called by the Hitbox script later
    public void RegisterHit()
    {
        hasHitEnemy = true;
        Debug.Log("HIT CONFIRMED! Combo unlocked.");
    }
}