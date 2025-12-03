using UnityEngine;
using System.Collections;

public class MadaraCombat : MonoBehaviour
{
    // ==================================================
    // 1. VARIABLES
    // ==================================================

    [Header("Components")]
    public Animator animator;
    public MadaraMovement movementScript;
    public MeleeDamage meleeScript;
    public BoxCollider2D bodyCollider; // DRAG MADARA'S COLLIDER HERE

    [Header("Combat Inputs")]
    public KeyCode buttonA = KeyCode.J; // Light Attack
    public KeyCode buttonB = KeyCode.K; // Heavy Attack
    public float comboWindow = 1.0f;

    [Header("Jutsu Inputs")]
    public KeyCode bunshinKey = KeyCode.Q;   // Wood Clone
    public KeyCode chargeKey = KeyCode.C;    // Chakra Charge
    public KeyCode transformKey = KeyCode.T; // Perfect Susanoo

    [Header("Clone Settings")]
    public GameObject shadowClonePrefab; 
    public GameObject smokeVFX;          

    [Header("Susanoo Charge (Stage 1)")]
    public GameObject susanooAuraObject; // Drag "SusanooAura" Child Object here
    public Animator susanooAnimator;     // Drag "SusanooAura" Child Object here

    [Header("Perfect Susanoo (Stage 2)")]
    public float susanooDuration = 15.0f;    
    public float susanooWalkSpeed = 4.0f;
    public Vector2 normalSize = new Vector2(1f, 2f);      
    public Vector2 susanooSize = new Vector2(4f, 6f);     
    public Vector2 susanooOffset = new Vector2(0f, 3f);   

    // --- INTERNAL STATE ---
    public bool isSusanooActive = false; // True = Perfect Susanoo Mode
    public bool isCharging = false;
    private bool isAttacking = false;
    private int comboStepA = 0;
    private int comboStepB = 0;
    private float lastAttackTime = 0;
    private bool moveContact = false; 
    private Coroutine attackRoutine;
    private Rigidbody2D rb;

    // ==================================================
    // 2. UNITY EVENTS
    // ==================================================

    void Start()
    {
        if (meleeScript == null) meleeScript = GetComponentInChildren<MeleeDamage>();
        if (meleeScript != null) meleeScript.combatScript = this; 
        if (movementScript == null) movementScript = GetComponent<MadaraMovement>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        // Record original collider size
        if (bodyCollider) normalSize = bodyCollider.size;

        // Ensure Susanoo Aura is hidden
        if (susanooAuraObject != null) susanooAuraObject.SetActive(false);
    }

    public void RegisterHit()
    {
        moveContact = true;
    }

    void Update()
    {
        // 1. Handle Charge
        HandleCharging();

        // If busy, stop here
        if (isCharging || isAttacking) return;

        // 2. Handle Susanoo Movement (Overrides Normal Movement)
        if (isSusanooActive)
        {
            HandleSusanooMovement();
        }

        // 3. Reset Combo Timer
        if (Time.time - lastAttackTime > comboWindow)
        {
            comboStepA = 0;
            comboStepB = 0;
            moveContact = false;
        }

        // 4. Inputs
        if (Input.GetKeyDown(transformKey) && !isSusanooActive)
            StartCoroutine(ActivateSusanoo());

        if (Input.GetKeyDown(bunshinKey)) 
            StartCoroutine(SummonClone());

        if (Input.GetKeyDown(buttonA))
        {
            if (isSusanooActive) PerformSusanooSlash();
            else if (comboStepA == 0 || moveContact) PerformComboA();
        }

        if (Input.GetKeyDown(buttonB))
        {
            if (isSusanooActive) PerformSusanooSlash(); 
            else if (comboStepB == 0 || moveContact) PerformComboB();
        }
    }

    // ==================================================
    // 3. PERFECT SUSANOO LOGIC
    // ==================================================

    void HandleSusanooMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");

        // Move
        rb.linearVelocity = new Vector2(moveX * susanooWalkSpeed, rb.linearVelocity.y);

        // Animate & Flip
        if (Mathf.Abs(moveX) > 0.1f)
        {
            PlayAnimSafe(animator, "Action_39020"); // Giant Walk
            transform.localScale = new Vector3(Mathf.Sign(moveX), 1, 1);
        }
        else
        {
            PlayAnimSafe(animator, "Action_39001"); // Giant Idle/Stance
        }
    }

    IEnumerator ActivateSusanoo()
    {
        isSusanooActive = true;
        isAttacking = true;
        
        // DISABLE Normal Movement Script (So it doesn't force normal animations)
        if (movementScript)
        {
            movementScript.rb.linearVelocity = Vector2.zero;
            movementScript.enabled = false; 
        }

        // Play Transformation/Stance
        PlayAnimSafe(animator, "Action_39001");

        // Resize Collider to be GIANT
        if (bodyCollider)
        {
            bodyCollider.size = susanooSize;
            bodyCollider.offset = susanooOffset;
        }

        // Wait for transformation feel
        yield return new WaitForSeconds(1.0f); 
        
        isAttacking = false; // Allow moving as giant
        
        // Auto-Revert after duration
        StartCoroutine(SusanooTimer());
    }

    IEnumerator SusanooTimer()
    {
        yield return new WaitForSeconds(susanooDuration);
        StartCoroutine(DeactivateSusanoo());
    }

    IEnumerator DeactivateSusanoo()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // Play Smoke or Revert Anim
        if(smokeVFX) Instantiate(smokeVFX, transform.position, Quaternion.identity);
        PlayAnimSafe(animator, "Action_0"); 

        // Reset Collider
        if (bodyCollider)
        {
            bodyCollider.size = normalSize;
            bodyCollider.offset = Vector2.zero;
        }

        yield return new WaitForSeconds(0.5f);

        isSusanooActive = false;
        isAttacking = false;
        
        // RE-ENABLE Normal Movement
        if (movementScript) movementScript.enabled = true;
    }

    void PerformSusanooSlash()
    {
        // Plays Action 39300 (Giant Slash B-1)
        StartCoroutine(PerformAttackRoutine("Action_39300", 1.5f, 60f, 10f, 10f));
    }

    // ==================================================
    // 4. CHARGE LOGIC (Stage 1)
    // ==================================================

    void HandleCharging()
    {
        if (isSusanooActive) return; // Cannot charge while in Perfect form

        if (Input.GetKeyDown(chargeKey))
        {
            isCharging = true;
            if (movementScript)
            {
                movementScript.rb.linearVelocity = Vector2.zero;
                movementScript.enabled = false;
            }

            PlayAnimSafe(animator, "Action_730"); // Charge Pose
            
            if (susanooAuraObject != null)
            {
                susanooAuraObject.SetActive(true);
                PlayAnimSafe(susanooAnimator, "Action_730");
            }
        }

        if (Input.GetKeyUp(chargeKey))
        {
            isCharging = false;
            if (movementScript) movementScript.enabled = true;
            
            PlayAnimSafe(animator, "Action_0");

            if (susanooAuraObject != null) susanooAuraObject.SetActive(false);
        }
    }

    // ==================================================
    // 5. WOOD CLONE & COMBOS
    // ==================================================

    IEnumerator SummonClone()
    {
        if (isSusanooActive) yield break; // No clones while Giant

        isAttacking = true;
        if (movementScript)
        {
            movementScript.enabled = false;
            movementScript.rb.linearVelocity = Vector2.zero;
        }

        PlayAnimSafe(animator, "Action_650"); // Hand Signs

        yield return new WaitForSeconds(0.5f);

        if (shadowClonePrefab != null)
        {
            float direction = transform.localScale.x;
            Vector3 spawnPos = transform.position + new Vector3(2.0f * direction, 0, 0);
            
            GameObject clone = Instantiate(shadowClonePrefab, spawnPos, Quaternion.identity);
            
            Vector3 newScale = clone.transform.localScale;
            newScale.x = Mathf.Abs(newScale.x) * direction;
            clone.transform.localScale = newScale;

            if(smokeVFX != null) Instantiate(smokeVFX, spawnPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.3f);
        EndAttack();
    }

    void PerformComboA()
    {
        comboStepB = 0; lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepA)
        {
            case 0: clipName = "Action_200"; SetHitStats(10, 2f, 0f); comboStepA = 1; break;
            case 1: clipName = "Action_210"; SetHitStats(10, 2f, 0f); comboStepA = 2; break;
            case 2: clipName = "Action_220"; SetHitStats(15, 3f, 0f); comboStepA = 3; break;
            case 3: clipName = "Action_230"; SetHitStats(20, 4f, 2f); comboStepA = 4; break;
            case 4: clipName = "Action_235"; SetHitStats(30, 8f, 2f); comboStepA = 0; break;
        }
        StartCoroutine(PerformAttackRoutine(clipName, GetClipDuration(clipName), meleeScript.damage, meleeScript.knockback.x, meleeScript.knockback.y));
    }

    void PerformComboB()
    {
        comboStepA = 0; lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepB)
        {
            case 0: clipName = "Action_300"; SetHitStats(15, 2f, 0f); comboStepB = 1; break;
            case 1: clipName = "Action_305"; SetHitStats(15, 2f, 0f); comboStepB = 2; break;
            case 2: clipName = "Action_310"; SetHitStats(15, 2f, 0f); comboStepB = 3; break;
            case 3: clipName = "Action_315"; SetHitStats(15, 2f, 0f); comboStepB = 4; break;
            case 4: clipName = "Action_320"; SetHitStats(20, 3f, 0f); comboStepB = 5; break;
            case 5: clipName = "Action_325"; SetHitStats(20, 3f, 0f); comboStepB = 6; break;
            case 6: clipName = "Action_330"; SetHitStats(20, 3f, 0f); comboStepB = 7; break;
            case 7: clipName = "Action_340"; SetHitStats(25, 4f, 5f); comboStepB = 0; break;
        }
        StartCoroutine(PerformAttackRoutine(clipName, GetClipDuration(clipName), meleeScript.damage, meleeScript.knockback.x, meleeScript.knockback.y));
    }

    // ==================================================
    // 6. HELPERS
    // ==================================================

    IEnumerator PerformAttackRoutine(string animName, float duration, float damage, float xKnock, float yKnock)
    {
        isAttacking = true;
        moveContact = false;
        
        PlayAnimSafe(animator, animName);

        if (movementScript) movementScript.enabled = false;
        if (rb) rb.linearVelocity = Vector2.zero;

        SetHitStats(damage, xKnock, yKnock);
        
        yield return new WaitForSeconds(duration * 0.2f); // Windup
        if (meleeScript) meleeScript.enabled = true;
        yield return new WaitForSeconds(duration * 0.3f); // Active
        if (meleeScript) meleeScript.enabled = false;
        yield return new WaitForSeconds(duration * 0.5f); // Recovery

        EndAttack();
    }

    void EndAttack()
    {
        isAttacking = false;
        
        if (isSusanooActive)
        {
            // Stay in Susanoo Mode, manual movement will take over
            PlayAnimSafe(animator, "Action_39001"); 
        }
        else
        {
            // Re-enable Normal Movement
            if (movementScript) 
            {
                movementScript.enabled = true;
                PlayAnimSafe(animator, "Action_0");
            }
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

    void PlayAnimSafe(Animator anim, string name)
    {
        if(anim != null) anim.Play(name);
    }

    float GetClipDuration(string clipName)
    {
        if (!animator || animator.runtimeAnimatorController == null) return 0.5f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip.length;
        }
        return 0.5f; 
    }
}