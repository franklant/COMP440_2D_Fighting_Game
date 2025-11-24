using UnityEngine;
using System;

public class _CharacterScript : MonoBehaviour
{
    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public GameObject myGroundCheck;
    public Animator myAnimator; 
    public FighterStatsManager myStats; 

    [Header("--- Character ---")]
    public string characterName = "Gojo";

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
    public float dashDuration = 0f;
    public float dashTimer = 0f;
    
    private float hDirection = 0;

    [Header("--- Status Flags ---")]
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    public bool isBlocking; 

    [Header("--- Input Buffer ---")]
    public GameObject inputBuffer;
    private InputReaderScript inputReaderScript;

    [Header("--- Move Database ---")]
    public GameObject moveDatabase;
    private MovementDataManager moveDatabaseManagerScript;

    // Movement Database Details
    public MoveDetails lightPunch;
    public MoveDetails kick;
    public MoveDetails superMove;
    public MoveDetails forwardDash;
    public MoveDetails backDash;
    public MoveDetails meter1;
    public MoveDetails meter2;
    public MoveDetails meter3;


    // State Machine Enum
    private enum STATE {
        IDLE = 0,
        WALKING = 1,
        JUMPING = 2,
        FALLING = 3,
        ATTACKING = 4,
        DIZZIED = 5, 
        DEAD = 6,
        BLOCKING = 7,
        FDASHING = 8,
        BDASHING = 9
    };

    void SetState(STATE state)
    {
        currentState = (int) state;
    }

    int GetState(STATE state)
    {
        return (int) state;
    }

    void Start()
    {
        // Get Components automatically
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>(); 
        myStats = GetComponent<FighterStatsManager>();
        inputBuffer = GameObject.FindGameObjectWithTag("InputReader");
        moveDatabase = GameObject.FindGameObjectWithTag("MoveDB");
        
        // Find objects by tag if not assigned
        if (myGroundCheck == null) myGroundCheck = GameObject.FindGameObjectWithTag("GroundCheck");
        if (midJabHitBox == null) midJabHitBox = GameObject.FindGameObjectWithTag("MidJabHitBox");

        // Get the Input Buffer
        if (inputBuffer == null) Debug.LogError("Cannot find Input Buffer");

        inputReaderScript = inputBuffer.GetComponent<InputReaderScript>();
        
        if (inputReaderScript == null) Debug.LogError("Cannot find inputReaderScript");

        // Get the Move Database Manager
        if (moveDatabase == null) Debug.LogError("Cannot find Move Database");

        moveDatabaseManagerScript = moveDatabase.GetComponent<MovementDataManager>();
        
        if (moveDatabaseManagerScript == null) Debug.LogError("Cannot find moveDatabaseManagerScript");

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

        // Movement Database Details
        lightPunch = LoadCharacterMove(characterName, "lightPunch");
        kick = LoadCharacterMove(characterName, "kick");
        superMove = LoadCharacterMove(characterName, "superMove");
        forwardDash = LoadCharacterMove(characterName, "forwardDash");
        backDash = LoadCharacterMove(characterName, "backDash");

        SetState(STATE.IDLE);
        isGrounded = false;
    }
    
    /// <summary>
    /// Loads a character move from the move database by character and movename.
    /// </summary>
    /// <param name="characterName">The name of the character performing the move.</param>
    /// <param name="moveName">The name of the move the character is performing.</param>
    /// <returns>The move details for that specific move.</returns>
    public MoveDetails LoadCharacterMove(string characterName, string moveName)
    {
        return moveDatabaseManagerScript.GetMove(characterName, moveName);
    }

    // void HandleDash(float duration)
    // {
    //     Debug.Log("Entered dash");
    //     velocity.x = maxSpeed;

    //     dashTimer += Time.deltaTime;
    // }

    /// <summary>
    /// Handles the input given a certain input sequence from the player.
    /// -TODO- How can we register a successful input without 1. Clearing the buffer, and 2. Continuously repeating a move multiple times
    /// a frame.
    /// </summary>
    void HandleInput()
    {
        //string inputToRemove = "";

        if (Input.anyKey)
        {
            if (inputReaderScript.FindInput("jjj"))
            {
                Debug.Log("Perform <color=yellow>placeholder CHAIN ATTACK FINAL</color>.");


                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>"); 
                
                //inputToRemove = "jjj";
                //Debug.Log("Index of Input Sequence: " + inputReaderScript.inputBuffer.IndexOf("jjj"));
            } else if (inputReaderScript.FindInput("jj"))
            {
                Debug.Log("Perform <color=yellow>placeholder CHAIN ATTACK SECOND</color>.");


                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>"); 
            }
            else if (inputReaderScript.FindInput(backDash.input))
            {
                Debug.Log("Perform <color=yellow>BACK DASH</color>.");

                dashTimer = 0;

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");

                SetState(STATE.BDASHING);
                // inputReaderScript.inputBuffer = "";
            }
            else if (inputReaderScript.FindInput(forwardDash.input))
            {
                Debug.Log("Perform <color=yellow>FORWARD DASH</color>.");

                dashTimer = 0;

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");

                SetState(STATE.FDASHING);
                // inputReaderScript.inputBuffer = "";
            }
            else if (inputReaderScript.FindInput(superMove.input))
            {
                Debug.Log("Perform <color=yellow>SUPER MOVE</color>.");

                if (myStats.TrySpendMeter(100f)) 
                {
                    PerformSuperMove();
                }
                else
                {
                    Debug.Log("Not enough Meter!");
                }

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                // inputReaderScript.inputBuffer = "";
            }
            else if (inputReaderScript.FindInput(lightPunch.input))
            {
                Debug.Log("Perform <color=purple>LIGHT PUNCH</color>.");

                isAttacking = true;
                
                //A. Fire the specific animation trigger HERE
                myAnimator.SetTrigger("Attack"); 
                
                // B. Set the duration for a normal punch (Short)
                attackCoolDownDuration = lightPunch.totalFrames / 60f; 

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                // inputReaderScript.inputBuffer = "";

                attackTimer = 0;
                
                // C. Lock the state
                SetState(STATE.ATTACKING);
            }
            else if (inputReaderScript.FindInput(kick.input))
            {
                Debug.Log("Perform <color=purple>KICK</color>.");

                // 1. Fire the Kick Trigger
                myAnimator.SetTrigger("Kick"); 
                
                // 2. Set Duration (Kicks are usually slightly slower than jabs, e.g., 0.5s)
                attackCoolDownDuration = kick.totalFrames / 60f; 
                //Debug.Log(attackCoolDownDuration + "<- COOL DOWN");

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                // inputReaderScript.inputBuffer = "";
                
                attackTimer = 0;
                
                // 3. Lock State
                SetState(STATE.ATTACKING);
            }
        }

        //inputReaderScript.RemoveInputSequence(inputToRemove);
    }

    void Update()
    {
        // 1. Dead/Dizzy Check (Input disabled)
        // (int)STATE -> GetState(STATE)
        if (currentState == GetState(STATE.DIZZIED) || currentState == GetState(STATE.DEAD))
        {
            HandleStates(); 
            myRigidBody.linearVelocity = velocity;
            return; 
        }

        // 2. Input Processing
        hDirection = Input.GetAxisRaw("Horizontal");
        isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        isBlocking = Input.GetKey(KeyCode.S); 


        HandleInput();

        // 3. Facing Logic (Always Face Enemy)
        if (enemyTarget != null)
        {
            if (transform.position.x > enemyTarget.position.x)
                mySpriteRenderer.flipX = true;  
            else
                mySpriteRenderer.flipX = false; 
        }

        // 4. Gravity Check
        // if (!isGrounded)
        // {
        //     SetState(STATE.FALLING);
        // } 

        // 5. Execute Logic
        HandleStates();
        UpdateAnimator();

        // 6. hangle gravity (test)
        if (isGrounded)
        {
            velocity.y = 0;
        }
        else
        {
            velocity.y -= GRAVITY;
        }

        myRigidBody.linearVelocity = velocity;

        // --- SUPER INPUT (I Key) ---
        // if (Input.GetKeyDown(KeyCode.I)) 
        // {
        //     if (myStats.TrySpendMeter(100f)) 
        //     {
        //         PerformSuperMove();
        //     }
        //     else
        //     {
        //         Debug.Log("Not enough Meter!");
        //     }
        // }
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
            myAnimator.SetBool("IsBlocking", currentState == GetState(STATE.BLOCKING));
            myAnimator.SetBool("isDash", currentState == GetState(STATE.FDASHING));
            myAnimator.SetBool("isBackDash", currentState == GetState(STATE.BDASHING));
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
        // (int)STATE.DEAD -> GetState(STATE.DEAD)
        if (currentState == GetState(STATE.DEAD)) return;
        
        // Check Blocking
        if (currentState == GetState(STATE.BLOCKING))
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
            case (int) STATE.FDASHING:  FDashingState(); break;
            case (int) STATE.BDASHING:  BDashingState(); break;
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
            // isAttacking was never set back to false
            isAttacking = false;
            SetState(STATE.IDLE);
            attackTimer = 0; 
        }
    }

    void HandleAirControl()
    {
        velocity.x += hDirection * (movementSpeed / 2) * acceleration * Time.deltaTime;
        float absVelocity = Math.Abs(velocity.x);
        velocity.x = Math.Clamp(absVelocity, 0, maxSpeed / 2) * hDirection;
    }

    bool applyGravity = false;
    void JumpingState()
    {
        isGrounded = false;

        HandleAirControl();

        // the actions in the jumping state take control of the properties of the character when called
        // which means we need to apply gravity manually when in this state 
        if (!applyGravity)
        {
            velocity.y = jumpHeight;
            applyGravity = true;
        }
        else
        {
            velocity.y -= GRAVITY;
        }

        if (velocity.y <= jumpHeight - 2) 
        {
            SetState(STATE.FALLING);
            applyGravity = false;
        }
    }

    void FallingState()
    {
        // player holds down, add more gravity
        if (isBlocking)
        {
            // Debug.Log("HELLOOO");
            velocity.y -= GRAVITY * 5;  // add 1 half more gravity
        } else
        {
            velocity.y -= GRAVITY;
        }

        HandleAirControl();

        // gravity should always be affecting the player, restricting it to a state causes issue
        if (isGrounded)
        {
            // velocity.y = 0;
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

    void FDashingState()
    {
        //myAnimator.SetBool("isDash", true);
        dashDuration = forwardDash.totalFrames / 60f;
        dashTimer += Time.deltaTime;
        velocity.x = maxSpeed + 2;

        if (dashTimer >= dashDuration)
        {
            //myAnimator.SetBool("isDash", false);
            SetState(STATE.IDLE);
            dashTimer = 0;
        }
    }

    void BDashingState()
    {
        //myAnimator.SetBool("isBackDash", true);
        dashDuration = backDash.totalFrames / 60f;
        dashTimer += Time.deltaTime;
        velocity.x = -maxSpeed - 2;

        if (dashTimer >= dashDuration)
        {
            //myAnimator.SetBool("isBackDash", false);
            SetState(STATE.IDLE);
            dashTimer = 0;
        }
    }

    // --- COLLISION ---
    void OnCollisionStay2D(Collision2D collision)
    {
        // let the player leave the ground if jumping
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