using UnityEngine;
using System.Collections;

public class GojoCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public GojoMovement movementScript;
    public MeleeDamage meleeScript;
    public Transform firePoint;   
    public Transform enemyTarget; 

    [Header("Melee Settings")]
    public float comboWindow = 1.0f;
    public KeyCode buttonAttack = KeyCode.J;
    public KeyCode buttonKick = KeyCode.K;

    [Header("Special Move Inputs")]
    public KeyCode buttonBlue = KeyCode.I;  
    public KeyCode buttonRed = KeyCode.O;   
    public KeyCode buttonPurple = KeyCode.P;

    [Header("Projectile Prefabs")]
    public GameObject blueOrbPrefab;
    public GameObject redProjectilePrefab;
    public GameObject purpleBlastPrefab;

    [Header("Projectile Settings")]
    public float redSpawnOffset = 1.5f; 
    public float dmgBlue = 200f;
    public float dmgRed = 400f;
    public float dmgPurple = 600f;

    // Internal State
    private int comboStepA = 0;
    private int comboStepB = 0;
    private float lastAttackTime = 0;
    private bool moveContact = false; 
    private Coroutine attackRoutine;

    void Start()
    {
        if (meleeScript == null) meleeScript = GetComponentInChildren<MeleeDamage>();
        if (meleeScript != null) meleeScript.combatScript = this;

        // Auto-find FirePoint 
        if (firePoint == null) 
        {
            Transform foundFP = transform.Find("FirePoint");
            if (foundFP != null) firePoint = foundFP;
            else firePoint = transform;
        }

        // Auto-find Enemy (Prioritize Player2)
        if (enemyTarget == null)
        {
            GameObject enemy = GameObject.FindGameObjectWithTag("Player2"); 
            if (enemy == null) enemy = GameObject.FindGameObjectWithTag("Enemy");
            if (enemy != null) enemyTarget = enemy.transform;
        }
    }

    public void RegisterHit()
    {
        moveContact = true;
    }

    void Update()
    {
        if (Time.time - lastAttackTime > comboWindow)
        {
            comboStepA = 0;
            comboStepB = 0;
            moveContact = false;
        }

        if (movementScript && !movementScript.enabled) return;

        // --- INPUT HANDLING ---
        if (Input.GetKeyDown(buttonAttack))
        {
            if (comboStepA == 0 || moveContact) PerformComboA();
        }
        else if (Input.GetKeyDown(buttonKick))
        {
            if (comboStepB == 0 || moveContact) PerformComboB();
        }
        else if (Input.GetKeyDown(buttonBlue))
        {
            StartCoroutine(PerformBlue());
        }
        else if (Input.GetKeyDown(buttonRed))
        {
            StartCoroutine(PerformRed());
        }
        else if (Input.GetKeyDown(buttonPurple))
        {
            StartCoroutine(PerformPurple());
        }
    }

    // =========================================================
    //               MELEE LOGIC
    // =========================================================

    void PerformComboA()
    {
        comboStepB = 0; 
        lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepA)
        {
            case 0: 
                clipName = "Action_200"; 
                SetHitStats(10, 2f, 0f); 
                ApplyForwardStep(3f); 
                comboStepA = 1; break;
            case 1: 
                clipName = "Action_210"; 
                SetHitStats(10, 2f, 0f); 
                ApplyForwardStep(4f); 
                comboStepA = 2; break;
            case 2: 
                clipName = "Action_220"; 
                SetHitStats(15, 3f, 0f); 
                ApplyForwardStep(5f); 
                comboStepA = 3; break;
            case 3: 
                clipName = "Action_230"; 
                SetHitStats(20, 2f, 25f); // High Y force for Launch
                ApplyForwardStep(5f); 
                comboStepA = 4; break; 
            case 4:
                StartCoroutine(PerformTeleportSpike());
                comboStepA = 0; break;
        }
        if (comboStepA != 0) PlayAttack(clipName);
    }

    void PerformComboB()
    {
        comboStepA = 0;
        lastAttackTime = Time.time;
        string clipName = "Action_300";

        switch (comboStepB)
        {
            case 0: 
                clipName = "Action_300"; 
                SetHitStats(15, 3f, 0f); 
                ApplyForwardStep(3f);
                comboStepB = 1; break;
            case 1: 
                clipName = "Action_310"; 
                SetHitStats(15, 3f, 0f); 
                ApplyForwardStep(4f);
                comboStepB = 2; break;
            case 2: 
                clipName = "Action_320"; 
                SetHitStats(20, 4f, 0f); 
                ApplyForwardStep(4f);
                comboStepB = 3; break;
            case 3: 
                clipName = "Action_330"; 
                SetHitStats(25, 3f, 14f);
                ApplyForwardStep(5f);
                comboStepB = 0; break; 
        }
        PlayAttack(clipName);
    }

    // =========================================================
    //               TELEPORT SPIKE
    // =========================================================
    IEnumerator PerformTeleportSpike()
    {
        yield return new WaitForSeconds(0.25f);

        // 1. Find Target
        Transform target = enemyTarget;
        if (target == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player2");
            if (found) target = found.transform;
        }

        if (target != null)
        {
            // Stop enemy velocity
            Rigidbody2D enemyRb = target.GetComponent<Rigidbody2D>();
            if(enemyRb != null) enemyRb.linearVelocity = Vector2.zero; 
            
            // Teleport BEHIND (or above)
            float currentX = transform.position.x;
            float enemyX = target.position.x;
            float sideMultiplier = (currentX < enemyX) ? -1f : 1f; 
            
            // Teleport 1.5 units ABOVE enemy
            Vector3 teleportPos = target.position + new Vector3(0.5f * sideMultiplier, 1.5f, 0); 
            transform.position = teleportPos;

            // Face Enemy
            float directionToFace = (enemyX > transform.position.x) ? 1f : -1f;
            transform.localScale = new Vector3(directionToFace, 1, 1);
            if(meleeScript) meleeScript.facingDirection = directionToFace;
        }

        // Downward Spike Force
        SetHitStats(35, 5f, -30f); 
        
        // FREEZE PHYSICS (Gravity 0)
        if (movementScript && movementScript.rb)
        {
            movementScript.rb.linearVelocity = Vector2.zero; 
            movementScript.rb.gravityScale = 0;        
        }
        
        PlayAttack("Action_240");
    }

    // =========================================================
    //               SPECIAL MOVE LOGIC
    // =========================================================

    IEnumerator PerformBlue()
    {
        LockMovement(true);
        animator.Play("Action_1300"); 
        yield return new WaitForSeconds(0.4f); 
        SpawnProjectile(blueOrbPrefab, dmgBlue, firePoint.position);
        yield return new WaitForSeconds(0.5f); 
        LockMovement(false);
    }

    IEnumerator PerformRed()
    {
        LockMovement(true);
        animator.Play("Action_1600"); 
        yield return new WaitForSeconds(0.1f);
        transform.position += new Vector3(-2.0f * transform.localScale.x, 0, 0);
        yield return new WaitForSeconds(0.5f); 

        Vector3 spawnPos;
        float facingDir = transform.localScale.x;

        if (enemyTarget != null)
        {
            spawnPos = enemyTarget.position + new Vector3(redSpawnOffset * facingDir, 0, 0);
            spawnPos.y = firePoint.position.y;
        }
        else
        {
            spawnPos = firePoint.position + new Vector3(2.0f * facingDir, 0, 0);
        }

        SpawnProjectile(redProjectilePrefab, dmgRed, spawnPos);
        yield return new WaitForSeconds(0.6f); 
        LockMovement(false);
    }

    IEnumerator PerformPurple()
    {
        LockMovement(true);
        animator.Play("Action_1900"); 
        yield return new WaitForSeconds(0.8f); 
        SpawnProjectile(purpleBlastPrefab, dmgPurple, firePoint.position);
        yield return new WaitForSeconds(0.5f); 
        LockMovement(false);
    }

    // =========================================================
    //               HELPERS
    // =========================================================

    void SpawnProjectile(GameObject prefab, float damage, Vector3 pos)
    {
        if (prefab == null) return;
        GameObject proj = Instantiate(prefab, pos, Quaternion.identity);
        var projScript = proj.GetComponent<ProjectileController>(); 
        if (projScript != null) projScript.damage = damage;

        if (transform.localScale.x < 0)
        {
            proj.transform.Rotate(0, 180, 0);
            Rigidbody2D pRb = proj.GetComponent<Rigidbody2D>();
            if (pRb != null) pRb.linearVelocity = new Vector2(-pRb.linearVelocity.x, pRb.linearVelocity.y);
        }
    }

    void LockMovement(bool isLocked)
    {
        if (movementScript)
        {
            movementScript.enabled = !isLocked;
            
            if (isLocked && movementScript.rb) 
            {
                movementScript.rb.linearVelocity = Vector2.zero;
            }
            
            // --- FIX: RESTORE GRAVITY WHEN UNLOCKING ---
            if (!isLocked && movementScript.rb)
            {
                movementScript.rb.gravityScale = movementScript.gravityScale;
            }
        }

        if (!isLocked)
        {
            animator.Play("Action_0"); 
            lastAttackTime = Time.time; 
        }
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
        LockMovement(false);
    }
}