using UnityEngine;
using UnityEngine.Events;
using System;

public class CharacterScript : MonoBehaviour
{
    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public GameObject myGroundCheck;
    public Animator myAnimator; 
    public FighterStatsManager myStats; 

    [Header("--- Combat References ---")]
    public GameObject midJabHitBox;      // The red box for punching
    public Transform enemyTarget;        // DRAG PLAYER 2 HERE! (To face them)
    
    [Header("--- Projectile Settings (Red) ---")]
    public GameObject redProjectilePrefab; // Drag your 'Red_Projectile' prefab here
    public Transform firePoint;            // Drag the 'FirePoint' child object here

    [Header("--- Attributes ---")]
    public float GRAVITY = 0.098f;
    public Vector3 velocity = Vector3.zero;
    public float movementSpeed = 4f;
    public float maxSpeed = 8f;
    public float acceleration = 3f;
    public float jumpHeight = 7f;
    
    [Header("--- State Tracking ---")]
    public int currentState;
    public float attackCoolDownDuration = 0.4f; // Tweak this to match animation length
    public float attackTimer = 0;
    
    private float hDirection = 0;

    [Header("--- Status Flags ---")]
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    public bool isBlocking; 

    // State Machine Enum
    private enum STATE {
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
        currentState = (int) state;
    }

    void Start()
    {
        // Get Components automatically
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>(); 
        myStats = GetComponent<FighterStatsManager>();
        
        // Find objects by tag if not assigned
        if (myGroundCheck == null) myGroundCheck = GameObject.FindGameObjectWithTag("GroundCheck");
        if (midJabHitBox == null) midJabHitBox = GameObject.FindGameObjectWithTag("MidJabHitBox");

        // Subscribe to Stats Events
        if (myStats != null)
        {
            myStats.OnDizzyStart.AddListener(EnableDizzyState);
            myStats.OnDeath.AddListener(EnableDeadState);
            myStats.OnDizzyEnd.AddListener(() => SetState(STATE.IDLE));
        }
        else
        {
            Debug.LogError("FighterStatsManager is missing! Please attach it to Gojo.");
        }

        SetState(STATE.IDLE);
        isGrounded = false;
    }

    void Update()
    {
        // 1. Dead/Dizzy Check (Input disabled)
        if (currentState == (int)STATE.DIZZIED || currentState == (int)STATE.DEAD)
        {
            HandleStates(); 
            myRigidBody.linearVelocity = velocity;
            return; 
        }

        // 2. Input Processing
        hDirection = Input.GetAxisRaw("Horizontal");
        isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        isBlocking = Input.GetKey(KeyCode.S); 

        // --- FIXED ATTACK LOGIC (J Key) ---
        // We use an 'if' statement so we can set specific timers/triggers immediately
        if (Input.GetKeyDown(KeyCode.J)) 
        {
            isAttacking = true;
            
            // A. Fire the specific animation trigger HERE
            myAnimator.SetTrigger("Attack"); 
            
            // B. Set the duration for a normal punch (Short)
            attackCoolDownDuration = 0.4f; 
            attackTimer = 0;
            
            // C. Lock the state
            SetState(STATE.ATTACKING);
        }

        // --- KICK INPUT (K Key) ---
        if (Input.GetKeyDown(KeyCode.K)) 
        {
            isAttacking = true;
            
            // 1. Fire the Kick Trigger
            myAnimator.SetTrigger("Kick"); 
            
            // 2. Set Duration (Kicks are usually slightly slower than jabs, e.g., 0.5s)
            attackCoolDownDuration = 0.5f; 
            attackTimer = 0;
            
            // 3. Lock State
            SetState(STATE.ATTACKING);
        }

        // 3. Facing Logic (Always Face Enemy)
        if (enemyTarget != null)
        {
            if (transform.position.x > enemyTarget.position.x)
                mySpriteRenderer.flipX = true;  
            else
                mySpriteRenderer.flipX = false; 
        }

        // 4. Gravity Check
        if (!isGrounded)
        {
            SetState(STATE.FALLING);
        } 

        // 5. Execute Logic
        HandleStates();
        UpdateAnimator();
        myRigidBody.linearVelocity = velocity;

        // --- SUPER INPUT (I Key) ---
        if (Input.GetKeyDown(KeyCode.I)) 
        {
            if (myStats.TrySpendMeter(100f)) 
            {
                PerformSuperMove();
            }
            else
            {
                Debug.Log("Not enough Meter!");
            }
        }
    }

    // --- ANIMATION HANDLING ---
    void UpdateAnimator()
    {
        if (myAnimator != null)
        {
            // Calculate if we are walking Forward or Backward (Moonwalk)
            // If facing Right (FlipX false) and moving Right (+), speed is Positive.
            // If facing Right (FlipX false) and moving Left (-), speed is Negative.
            float facingMult = mySpriteRenderer.flipX ? -1 : 1;
            float directionalSpeed = velocity.x * facingMult;

            myAnimator.SetFloat("Speed", Mathf.Abs(velocity.x));     // For transitioning to Walk
            myAnimator.SetFloat("WalkDirection", directionalSpeed);  // For reversing the animation
            myAnimator.SetBool("IsBlocking", currentState == (int)STATE.BLOCKING);
        }
    }

    // --- COMBAT EVENTS ---
    
    // This is called by the Animation Event for "Cursed Technique Red"
    public void CastRed()
    {
        if (redProjectilePrefab != null && firePoint != null)
        {
            Instantiate(redProjectilePrefab, firePoint.position, firePoint.rotation);
        }
    }

    // This is called when we get hit by an enemy Hitbox
    public void GetHit(float damage, float stunDamage)
    {
        if (currentState == (int)STATE.DEAD) return;
        
        // Check Blocking
        if (currentState == (int)STATE.BLOCKING)
        {
            // Block Logic: Reduced damage, no stun, gain meter
            float chipDamage = damage * 0.1f; 
            if(myStats != null) myStats.BlockAttack(chipDamage, 5f);
            Debug.Log("Blocked!");
        }
        else
        {
            // Hit Logic: Full damage, full stun, gain catch-up meter
            if(myStats != null) myStats.TakeDamage(damage, 10f, stunDamage);
            
            // Visual Feedback
            StartCoroutine(FlashRed());
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        mySpriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (currentState != (int)STATE.DIZZIED) mySpriteRenderer.color = Color.white;
    }

    // --- STATE MACHINE ---

    
    void EnableDizzyState() 
    { 
        SetState(STATE.DIZZIED); 
    }

    void EnableDeadState() 
    { 
        SetState(STATE.DEAD); 
    }

    void HandleStates()
    {
        switch (currentState)
        {   
            case (int) STATE.IDLE:      IdleState(); break;
            case (int) STATE.WALKING:   WalkingState(); break;
            case (int) STATE.JUMPING:   JumpingState(); break;
            case (int) STATE.FALLING:   FallingState(); break;
            case (int) STATE.ATTACKING: AttackingState(); break; // Updated
            case (int) STATE.DIZZIED:   DizziedState(); break;
            case (int) STATE.DEAD:      DeadState(); break;
            case (int) STATE.BLOCKING:  BlockingState(); break;
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
        velocity.x = Math.Clamp(absVelocity, 0, maxSpeed) * hDirection;

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

        // --- DELETE THIS PART ---
        // if (attackTimer == 0) 
        // {
        //    myAnimator.SetTrigger("Attack");
        // }
        // ------------------------

        // Keep the timer logic!
        attackTimer += Time.deltaTime;

        if (attackTimer > attackCoolDownDuration) 
        {
            attackTimer = 0; 
            SetState(STATE.IDLE);
        }
    }

    void JumpingState()
    {
        velocity.y = jumpHeight;
        SetState(STATE.FALLING);
        isGrounded = false;
    }

    void FallingState()
    {
        velocity.y -= GRAVITY;
        if (isGrounded)
        {
            velocity.y = 0;
            if (hDirection != 0) SetState(STATE.WALKING);
            else SetState(STATE.IDLE);
        }
    }

    void DizziedState()
    {
        velocity.x = 0;
        mySpriteRenderer.color = Color.gray; 
    }

    void DeadState()
    {
        velocity.x = 0;
        myRigidBody.simulated = false; 
        mySpriteRenderer.color = Color.red; 
    }

    // --- COLLISION ---
    void OnCollisionStay2D(Collision2D collision)
    {
         if (collision.collider.CompareTag("Ground")) isGrounded = true;
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground")) isGrounded = false;
    }

    // --- DEBUGGER ---
    void OnGUI()
    {
        // Create a style to make the text big and readable
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white; // Or Color.red if background is bright

        // 1. Show the Raw Input (-1 for Left, 0 for None, 1 for Right)
        float inputVal = Input.GetAxisRaw("Horizontal");
        GUI.Label(new Rect(20, 20, 400, 40), "Input Axis: " + inputVal, style);

        // 2. Show the actual key presses (Did Unity detect the button?)
        string keysPressed = "";
        if (Input.GetKey(KeyCode.A)) keysPressed += "[A] ";
        if (Input.GetKey(KeyCode.D)) keysPressed += "[D] ";
        if (Input.GetKey(KeyCode.LeftArrow)) keysPressed += "[Left] ";
        if (Input.GetKey(KeyCode.RightArrow)) keysPressed += "[Right] ";
        GUI.Label(new Rect(20, 60, 400, 40), "Keys Held: " + keysPressed, style);

        // 3. Show the Current State (Is the script locking you in Block?)
        GUI.Label(new Rect(20, 100, 400, 40), "Current State: " + (STATE)currentState, style);
        
        // 4. Show Velocity (Is physics trying to move you?)
        GUI.Label(new Rect(20, 140, 400, 40), "Velocity X: " + velocity.x, style);
        
        // 5. Show Blocking status
        GUI.Label(new Rect(20, 180, 400, 40), "Is Blocking: " + isBlocking, style);
    }

    void PerformSuperMove()
    {
        // 1. Stop moving
        velocity.x = 0;

        // 2. Set State to ATTACKING (locks movement)
        SetState(STATE.ATTACKING);

        // 3. Trigger the specific animation
        myAnimator.SetTrigger("Meter1");

        // 4. IMPORTANT: Update the cooldown!
        // Basic punch is fast (0.4s), but Super is long (e.g., 1.5s).
        // We need to extend the timer so Gojo doesn't go back to Idle halfway through.
        attackCoolDownDuration = 1.5f; // Change this to match your Super's length in seconds!
        attackTimer = 0;
    }
}