using UnityEngine;
using System.Collections;

public class GojoCombat : MonoBehaviour
{
    public Animator animator;
    public GojoMovement movementScript;
    public MeleeDamage meleeScript;

    [Header("Combat Settings")]
    public float comboWindow = 1.0f;
    public KeyCode buttonA = KeyCode.J;
    public KeyCode buttonB = KeyCode.K;

    private int comboStepA = 0;
    private int comboStepB = 0;
    private float lastAttackTime = 0;
    
    // NEW: Tracks if we hit something
    private bool moveContact = false; 
    
    private Coroutine attackRoutine;

    void Start()
    {
        if (meleeScript == null) meleeScript = GetComponentInChildren<MeleeDamage>();
        
        // Auto-link this script to the melee script if it wasn't done manually
        if (meleeScript != null) meleeScript.combatScript = this;
    }

    // Called by MeleeDamage.cs when we hit an enemy
    public void RegisterHit()
    {
        moveContact = true;
        // Optional: Add hit sound or small pause here
    }

    void Update()
    {
        // Reset if window expires
        if (Time.time - lastAttackTime > comboWindow)
        {
            comboStepA = 0;
            comboStepB = 0;
            moveContact = false;
        }

        // --- COMBO A (J Series) ---
        if (Input.GetKeyDown(buttonA))
        {
            // Condition: Is this the starter (0)? OR Did we hit the enemy (moveContact)?
            if (comboStepA == 0 || moveContact)
            {
                PerformComboA();
            }
            else
            {
                Debug.Log("Combo dropped: You missed!");
            }
        }

        // --- COMBO B (K Series) ---
        if (Input.GetKeyDown(buttonB))
        {
            if (comboStepB == 0 || moveContact)
            {
                PerformComboB();
            }
        }
    }

    void PerformComboA()
    {
        comboStepB = 0; // Reset other chain
        lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepA)
        {
            case 0: 
                clipName = "Action_200"; 
                SetHitStats(10, 2f, 0f); 
                ApplyForwardStep(3f); 
                comboStepA = 1; 
                break;
            case 1: 
                clipName = "Action_210"; 
                SetHitStats(10, 2f, 0f); 
                ApplyForwardStep(4f); 
                comboStepA = 2; 
                break;
            case 2: 
                clipName = "Action_220"; 
                SetHitStats(15, 3f, 0f); 
                ApplyForwardStep(5f); 
                comboStepA = 3; 
                break;
            case 3: 
                clipName = "Action_230"; 
                SetHitStats(20, 2f, 25f); // Launcher
                ApplyForwardStep(5f); 
                comboStepA = 4; 
                break; 
            case 4:
                StartCoroutine(PerformTeleportSpike());
                comboStepA = 0; 
                break;
        }
        
        // Don't play default logic for case 4 (Teleport handles it)
        if (comboStepA != 0) PlayAttack(clipName);
    }

    IEnumerator PerformTeleportSpike()
    {
        yield return new WaitForSeconds(0.25f);

        GameObject target = GameObject.FindGameObjectWithTag("Enemy");
        if (target != null)
        {
            Rigidbody2D enemyRb = target.GetComponent<Rigidbody2D>();
            if(enemyRb != null) enemyRb.linearVelocity = Vector2.zero; 

            float currentX = transform.position.x;
            float enemyX = target.transform.position.x;
            float sideMultiplier = (currentX < enemyX) ? -1f : 1f; 
            Vector3 teleportPos = target.transform.position + new Vector3(0.5f * sideMultiplier, 1.0f, 0);
            transform.position = teleportPos;

            float directionToFace = (enemyX > transform.position.x) ? 1f : -1f;
            transform.localScale = new Vector3(directionToFace, 1, 1);
            if(meleeScript) meleeScript.facingDirection = directionToFace;
        }

        SetHitStats(35, 20f, -30f); 
        
        if (movementScript && movementScript.rb)
        {
            movementScript.rb.linearVelocity = Vector2.zero; 
            movementScript.rb.gravityScale = 0;        
        }

        PlayAttack("Action_240");
    }

    void PerformComboB()
    {
        comboStepA = 0;
        lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepB)
        {
            case 0: 
                clipName = "Action_300"; 
                SetHitStats(15, 3f, 0f); 
                ApplyForwardStep(3f);
                comboStepB = 1; 
                break;
            case 1: 
                clipName = "Action_310"; 
                SetHitStats(15, 3f, 0f); 
                ApplyForwardStep(4f);
                comboStepB = 2; 
                break;
            case 2: 
                clipName = "Action_320"; 
                SetHitStats(20, 4f, 0f); 
                ApplyForwardStep(4f);
                comboStepB = 3; 
                break;
            case 3: 
                clipName = "Action_330"; 
                SetHitStats(25, 3f, 14f); // Launcher
                ApplyForwardStep(5f);
                comboStepB = 0; 
                break; 
        }
        PlayAttack(clipName);
    }

    void ApplyForwardStep(float speed)
    {
        if (movementScript && movementScript.rb)
        {
            float facing = transform.localScale.x;
            movementScript.rb.linearVelocity = new Vector2(speed * facing, 0); 
        }
    }

    void SetHitStats(float damage, float xForce, float yForce)
    {
        if (meleeScript != null)
        {
            meleeScript.damage = damage;
            meleeScript.knockback = new Vector2(xForce, yForce);
            meleeScript.facingDirection = transform.localScale.x; 
        }
    }

    void PlayAttack(string clipName)
    {
        // 1. Reset Hit Status (You haven't hit the new move yet!)
        moveContact = false;

        animator.Play(clipName, 0, 0f); 

        if (movementScript) movementScript.enabled = false;

        float duration = GetClipDuration(clipName);
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(RecoverMovement(duration));
    }

    float GetClipDuration(string clipName)
    {
        if (animator.runtimeAnimatorController == null) return 0.5f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip.length;
        }
        return 0.5f; 
    }

    IEnumerator RecoverMovement(float duration)
    {
        yield return new WaitForSeconds(0.1f); 
        if(movementScript && movementScript.rb) movementScript.rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(duration - 0.1f);
        
        if (movementScript) 
        {
            movementScript.enabled = true;
            if (movementScript.rb) movementScript.rb.gravityScale = movementScript.gravityScale;
            animator.Play("Action_0"); 
        }
    }
}