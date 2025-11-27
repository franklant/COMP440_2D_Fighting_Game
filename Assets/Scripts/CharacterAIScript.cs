using UnityEngine;
using System;

public class CharacterAI : MonoBehaviour
{
    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public GameObject myGroundCheck;
    public Animator myAnimator;
    public FighterStatsManager myStats;

    [Header("--- Combat / Target ---")]
    public Transform enemyTarget;        // Drag Player 1 / Opponent here
    public GameObject midJabHitBox;      // Optional: link hitbox if needed

    [Header("--- Projectile Settings (Red) ---")]
    public GameObject redProjectilePrefab; // Same as CharacterScript
    public Transform firePoint;

    [Header("--- Movement & Physics ---")]
    public float GRAVITY = 0.098f;
    public Vector3 velocity = Vector3.zero;
    public float movementSpeed = 4f;
    public float maxSpeed = 8f;
    public float acceleration = 3f;
    public float jumpHeight = 7f;

    [Header("--- State Tracking ---")]
    public int currentState;
    public float attackCoolDownDuration = 0.4f;
    public float attackTimer = 0f;

    private float hDirection = 0f;

    [Header("--- Status Flags ---")]
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    public bool isBlocking;

    [Header("--- AI Behaviour ---")]
    [Tooltip("Distance where we want to start attacking.")]
    public float attackRange = 2.2f;

    [Tooltip("If farther than this, walk toward opponent.")]
    public float approachDistance = 3.5f;

    [Tooltip("If closer than this and low health, try to back away.")]
    public float retreatDistance = 1.2f;

    [Tooltip("Seconds between AI attack attempts.")]
    public float attackCooldown = 1.0f;

    [Tooltip("How often the AI 'thinks' (seconds).")]
    public float thinkInterval = 0.15f;

    [Tooltip("Chance per think to decide to block when close.")]
    [Range(0f, 1f)] public float blockChance = 0.35f;

    [Tooltip("Chance per think to jump when approaching.")]
    [Range(0f, 1f)] public float jumpChance = 0.15f;

    [Tooltip("Meter cost for Super (Meter1).")]
    public float superCost = 100f;

    [Tooltip("Chance to use Super instead of normal attack when meter available.")]
    [Range(0f, 1f)] public float superChance = 0.3f;

    // --- Internal ---
    private float lastThinkTime;
    private float lastAttackTime;
    private float myCurrentHealth;
    private float myMaxHealth;

    // State Machine Enum
    private enum STATE
    {
        IDLE = 0,
        WALKING = 1,
        JUMPING = 2,
        FALLING = 3,
        ATTACKING = 4,
        DIZZIED = 5,
        DEAD = 6,
        BLOCKING = 7
    };

    void SetState(STATE state)
    {
        currentState = (int)state;
    }

    int GetState(STATE state)
    {
        return (int)state;
    }

    void Start()
    {
        // Auto-get components if not assigned
        if (!mySpriteRenderer) mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!myRigidBody) myRigidBody = GetComponent<Rigidbody2D>();
        if (!myAnimator) myAnimator = GetComponent<Animator>();
        if (!myStats) myStats = GetComponent<FighterStatsManager>();

        // Subscribe to stats events
        if (myStats != null)
        {
            myStats.OnDizzyStart.AddListener(EnableDizzyState);
            myStats.OnDeath.AddListener(EnableDeadState);
            myStats.OnDizzyEnd.AddListener(OnDizzyEnd);
            myStats.OnHealthChanged += HandleHealthChanged;
        }

        SetState(STATE.IDLE);
        isGrounded = false;
    }

    void OnDestroy()
    {
        if (myStats != null)
        {
            myStats.OnDizzyStart.RemoveListener(EnableDizzyState);
            myStats.OnDeath.RemoveListener(EnableDeadState);
            myStats.OnDizzyEnd.RemoveListener(OnDizzyEnd);
            myStats.OnHealthChanged -= HandleHealthChanged;
        }
    }

    void HandleHealthChanged(float current, float max)
    {
        myCurrentHealth = current;
        myMaxHealth = max;
    }

    void Update()
    {
        if (!myRigidBody) return;

        // 1. Dead / Dizzy hard-lock
        if (currentState == GetState(STATE.DIZZIED) || currentState == GetState(STATE.DEAD))
        {
            HandleStates();
            myRigidBody.linearVelocity = velocity;
            return;
        }

        // 2. AI Decision (no Input)
        ThinkAI();

        // 3. Facing logic (always face enemy)
        HandleFacing();

        // 4. Run state machine
        HandleStates();
        UpdateAnimator();

        // 5. Ground clamp (vertical velocity is handled in Jumping/Falling)
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f;
        }

        myRigidBody.linearVelocity = velocity;
    }

    // ======================
    //   AI LOGIC
    // ======================
    void ThinkAI()
    {
        if (enemyTarget == null) return;
        if (Time.time < lastThinkTime + thinkInterval) return;
        lastThinkTime = Time.time;

        float distance = Mathf.Abs(enemyTarget.position.x - transform.position.x);
        float dirToEnemy = Mathf.Sign(enemyTarget.position.x - transform.position.x);

        // Reset intents
        hDirection = 0f;
        isJumping = false;
        isBlocking = false;

        bool canAttack = Time.time >= lastAttackTime + attackCooldown;

        // Decide attack / super when in range
        if (distance <= attackRange && canAttack && !isAttacking)
        {
            TryAttackOrSuper();
        }
        else
        {
            // Maybe block if close
            if (distance <= attackRange * 1.2f && UnityEngine.Random.value < blockChance)
            {
                isBlocking = true;
            }

            // Movement logic (only if not hard-blocking)
            if (!isBlocking)
            {
                // Too far → walk in
                if (distance > approachDistance)
                {
                    hDirection = dirToEnemy;

                    // Sometimes jump when closing in
                    if (isGrounded && UnityEngine.Random.value < jumpChance)
                    {
                        isJumping = true;
                    }
                }
                // Too close & low health → retreat
                else if (distance < retreatDistance && IsLowHealth())
                {
                    hDirection = -dirToEnemy;

                    // Sometimes crouch-block while backing off
                    isBlocking = UnityEngine.Random.value < 0.5f;
                }
                else
                {
                    // Poke range: sometimes inch forward, sometimes stop
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        hDirection = dirToEnemy;
                    }
                }
            }
        }

        // If AI chooses to block, ensure we go into block state
        if (isBlocking)
        {
            SetState(STATE.BLOCKING);
        }
    }

    bool IsLowHealth()
    {
        if (myMaxHealth <= 0f) return false;
        float ratio = myCurrentHealth / myMaxHealth;
        return ratio < 0.35f;
    }

    void TryAttackOrSuper()
    {
        lastAttackTime = Time.time;

        // Prefer Super if meter available
        bool didSuper = false;
        if (myStats != null && UnityEngine.Random.value < superChance)
        {
            if (myStats.TrySpendMeter(superCost))
            {
                PerformSuperMove();
                didSuper = true;
            }
        }

        if (!didSuper)
        {
            // Normal attack: choose between punch (J) and kick (K)
            isAttacking = true;

            if (UnityEngine.Random.value < 0.6f)
            {
                myAnimator.SetTrigger("Attack"); // same trigger as J key
                attackCoolDownDuration = 0.4f;
            }
            else
            {
                myAnimator.SetTrigger("Kick");   // same trigger as K key
                attackCoolDownDuration = 0.5f;
            }

            attackTimer = 0f;
            SetState(STATE.ATTACKING);
        }
    }

    // ======================
    //   FACING
    // ======================
    void HandleFacing()
    {
        if (enemyTarget == null || mySpriteRenderer == null) return;

        if (transform.position.x > enemyTarget.position.x)
            mySpriteRenderer.flipX = true;
        else
            mySpriteRenderer.flipX = false;
    }

    // ======================
    //   AIR CONTROL
    // ======================
    void HandleAirControl()
    {
        // Simple air drift toward/away
        velocity.x += hDirection * (movementSpeed / 2f) * acceleration * Time.deltaTime;

        float absVel = Mathf.Abs(velocity.x);
        float clamped = Mathf.Clamp(absVel, 0f, maxSpeed / 2f);

        float dir =
            hDirection != 0f ? Mathf.Sign(hDirection) :
            (velocity.x != 0f ? Mathf.Sign(velocity.x) : 0f);

        velocity.x = clamped * dir;
    }

    // ======================
    //   STATE MACHINE
    // ======================
    void HandleStates()
    {
        switch (currentState)
        {
            case (int)STATE.IDLE:      IdleState(); break;
            case (int)STATE.WALKING:   WalkingState(); break;
            case (int)STATE.JUMPING:   JumpingState(); break;
            case (int)STATE.FALLING:   FallingState(); break;
            case (int)STATE.ATTACKING: AttackingState(); break;
            case (int)STATE.DIZZIED:   DizziedState(); break;
            case (int)STATE.DEAD:      DeadState(); break;
            case (int)STATE.BLOCKING:  BlockingState(); break;
        }
    }

    void IdleState()
    {
        velocity.x = 0f;

        // Start jump from idle
        if (isJumping && isGrounded)
        {
            isGrounded = false;
            velocity.y = jumpHeight;
            SetState(STATE.JUMPING);
            return;
        }

        if (hDirection != 0f) SetState(STATE.WALKING);
        if (isAttacking) SetState(STATE.ATTACKING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void WalkingState()
    {
        // Ground movement
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime;
        float absVelocity = Mathf.Abs(velocity.x);
        float clamped = Mathf.Clamp(absVelocity, 0f, maxSpeed);

        float dir =
            hDirection != 0f ? Mathf.Sign(hDirection) :
            (velocity.x != 0f ? Mathf.Sign(velocity.x) : 0f);

        velocity.x = clamped * dir;

        // Jump from walk
        if (isJumping && isGrounded)
        {
            isGrounded = false;
            velocity.y = jumpHeight;
            SetState(STATE.JUMPING);
            return;
        }

        if (hDirection == 0f && isGrounded) SetState(STATE.IDLE);
        if (isAttacking) SetState(STATE.ATTACKING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void BlockingState()
    {
        // Ground block only; if we somehow block in air, drop back to falling
        if (!isGrounded)
        {
            SetState(STATE.FALLING);
            return;
        }

        velocity.x = 0f;
        if (!isBlocking) SetState(STATE.IDLE);
    }

    void AttackingState()
    {
        velocity.x = 0f;

        attackTimer += Time.deltaTime;

        if (attackTimer > attackCoolDownDuration)
        {
            SetState(STATE.IDLE);
            attackTimer = 0f;
            isAttacking = false;
        }
    }

    void JumpingState()
    {
        isGrounded = false;

        // Upward motion decays into fall
        velocity.y -= GRAVITY;
        HandleAirControl();

        if (velocity.y <= 0f)
        {
            SetState(STATE.FALLING);
        }
    }

    void FallingState()
    {
        // Extra fast-fall if "blocking" intent in air
        float gravityMultiplier = isBlocking ? 5f : 1f;
        velocity.y -= GRAVITY * gravityMultiplier;

        HandleAirControl();

        if (isGrounded)
        {
            velocity.y = 0f;
            if (hDirection != 0f) SetState(STATE.WALKING);
            else SetState(STATE.IDLE);
        }
    }

    void DizziedState()
    {
        velocity.x = 0f;
        if (mySpriteRenderer) mySpriteRenderer.color = Color.gray;
    }

    void DeadState()
    {
        velocity.x = 0f;
        if (myRigidBody) myRigidBody.simulated = false;
        if (mySpriteRenderer) mySpriteRenderer.color = Color.red;
    }

    // ======================
    //   STATS EVENTS
    // ======================
    void EnableDizzyState()
    {
        SetState(STATE.DIZZIED);
    }

    void EnableDeadState()
    {
        SetState(STATE.DEAD);
    }

    void OnDizzyEnd()
    {
        if (mySpriteRenderer) mySpriteRenderer.color = Color.white;
        SetState(STATE.IDLE);
    }

    // ======================
    //   COMBAT / SUPER
    // ======================
    public void CastRed()  // same name as in CharacterScript for animation events
    {
        if (redProjectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(redProjectilePrefab, firePoint.position, firePoint.rotation);

            // Flip projectile with facing
            if (mySpriteRenderer && mySpriteRenderer.flipX)
            {
                proj.transform.Rotate(0f, 180f, 0f);
            }
        }
    }

    void PerformSuperMove()
    {
        velocity.x = 0f;
        SetState(STATE.ATTACKING);

        if (myAnimator != null)
        {
            myAnimator.SetTrigger("Meter1");
        }

        attackCoolDownDuration = 1.5f; // match super anim length
        attackTimer = 0f;
        isAttacking = true;
    }

    // ======================
    //   COLLISIONS
    // ======================
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
            isGrounded = false;
    }

    // ======================
    //   ANIMATOR
    // ======================
    void UpdateAnimator()
    {
        if (!myAnimator || !mySpriteRenderer) return;

        float facingMult = mySpriteRenderer.flipX ? -1f : 1f;
        float directionalSpeed = velocity.x * facingMult;

        myAnimator.SetFloat("Speed", Mathf.Abs(velocity.x));
        myAnimator.SetFloat("WalkDirection", directionalSpeed);
        myAnimator.SetBool("IsBlocking", currentState == GetState(STATE.BLOCKING));
    }

    // Optional: visualize AI ranges
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, approachDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}
