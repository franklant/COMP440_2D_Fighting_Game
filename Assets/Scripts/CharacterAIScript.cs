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
    private bool applyGravity = false;
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

        // 5. Gravity
        if (isGrounded)
        {
            velocity.y = 0;
        }
        else
        {
            velocity.y -= GRAVITY;
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

        // Reset inputs
        hDirection = 0f;
        isJumping = false;
        isBlocking = false;

        bool canAttack = Time.time >= lastAttackTime + attackCooldown;

        // Decide attack / super when in range
        if (distance <= attackRange && canAttack)
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

        // Jump intent will be handled in state logic (Idle/Walking -> Jumping)
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
        velocity.x = 0;

        if (hDirection != 0) SetState(STATE.WALKING);
        if (isJumping && isGrounded) SetState(STATE.JUMPING);
        if (isAttacking) SetState(STATE.ATTACKING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void WalkingState()
    {
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime;
        float absVelocity = Math.Abs(velocity.x);
        velocity.x = Math.Clamp(absVelocity, 0, maxSpeed) * Math.Sign(hDirection == 0 ? velocity.x : hDirection);

        if (hDirection == 0 && isGrounded) SetState(STATE.IDLE);
        if (isJumping && isGrounded) SetState(STATE.JUMPING);
        if (isAttacking) SetState(STATE.ATTACKING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void BlockingState()
    {
        velocity.x = 0;
        if (!isBlocking) SetState(STATE.IDLE);
    }

    void AttackingState()
    {
        velocity.x = 0;

        attackTimer += Time.deltaTime;

        if (attackTimer > attackCoolDownDuration)
        {
            SetState(STATE.IDLE);
            attackTimer = 0;
            isAttacking = false;
        }
    }

    void JumpingState()
    {
        isGrounded = false;

        if (!applyGravity)
        {
            velocity.y = jumpHeight;
            applyGravity = true;
        }
        else
        {
            velocity.y -= GRAVITY;
        }

        if (velocity.y <= 2)
        {
            SetState(STATE.FALLING);
            applyGravity = false;
        }
    }

    void FallingState()
    {
        if (isBlocking)
        {
            velocity.y -= GRAVITY * 5;
        }

        if (isGrounded)
        {
            if (hDirection != 0) SetState(STATE.WALKING);
            else SetState(STATE.IDLE);
        }
    }

    void DizziedState()
    {
        velocity.x = 0;
        if (mySpriteRenderer) mySpriteRenderer.color = Color.gray;
    }

    void DeadState()
    {
        velocity.x = 0;
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
            Instantiate(redProjectilePrefab, firePoint.position, firePoint.rotation);
        }
    }

    void PerformSuperMove()
    {
        velocity.x = 0;
        SetState(STATE.ATTACKING);
        myAnimator.SetTrigger("Meter1");

        attackCoolDownDuration = 1.5f; // match super anim length
        attackTimer = 0;
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

        float facingMult = mySpriteRenderer.flipX ? -1 : 1;
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
