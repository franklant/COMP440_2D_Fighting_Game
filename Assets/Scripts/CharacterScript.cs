using UnityEngine;
using UnityEngine.Events;
using System;


// TODO: FIX STATES, FIX USER INPUT, FIX JUMPING (THERE IS NO STATE)

public class CharacterScript : MonoBehaviour
{
    [Header("--- Control Settings ---")]
    public bool isPlayer = true; // CHECK THIS FOR GOJO, UNCHECK FOR DUMMY

    [Header("--- Character ---")]
    public string characterName = "Gojo";

    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public Animator myAnimator; 
    public FighterStatsManager myStats; 
    public GameObject myGroundCheck;

    [Header("--- VFX References ---")]
    public ScreenFlashEffect redScreenFlash; 
    public CameraShake camShake; 
    public ScreenDimmer screenDimmer;

    [Header("--- Combat References ---")]
    public GameObject midJabHitBox;      
    public GameObject kickHitBox;        
    public Transform enemyTarget;        
    public Transform firePoint;          

    [Header("--- Projectile Settings ---")]
    public GameObject blueProjectilePrefab;   // Meter 1
    public GameObject redProjectilePrefab;    // Meter 2
    public GameObject purpleProjectilePrefab; // Meter 3
    
    [Tooltip("How far in front of the enemy Red should spawn")]
    public float redSpawnOffset = 1.5f; 

    [Header("--- Combat Balance ---")]
    public float jabDamage = 50f;
    public float kickDamage = 80f;
    public float meter1Damage = 200f; 
    public float meter2Damage = 400f; 
    public float meter3Damage = 600f; 

    [Header("--- Physics Attributes ---")]
    public float GRAVITY = 0.098f;          
    public Vector3 velocity = Vector3.zero; 
    public float movementSpeed = 4f;
    public float maxSpeed = 8f;
    public float acceleration = 3f;
    public float jumpHeight = 22f; 
    
    [Header("--- State Tracking ---")]
    public int currentState;
    public float attackCoolDownDuration = 0.4f; 
    public float attackTimer = 0;
    public int attackLevel = 0; // 0=None, 1=Light, 2=Heavy, 3=Special
    public float dashDuration = 0f;
    public float dashTimer = 0f;

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
    private float hDirection = 0;

    // Status Flags
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    public bool isBlocking; 

    private enum STATE {
        IDLE = 0, WALKING = 1, JUMPING = 2, FALLING = 3,
        ATTACKING = 4, DIZZIED = 5, DEAD = 6, BLOCKING = 7,
        FDASHING = 8,
        BDASHING = 9
    };

    void SetState(STATE state) { currentState = (int) state; }

    int GetState(STATE state) { return (int) state; }

    void Start()
    {
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>(); 
        myStats = GetComponent<FighterStatsManager>();
        inputBuffer = GameObject.FindGameObjectWithTag("InputReader");
        moveDatabase = GameObject.FindGameObjectWithTag("MoveDB");
        
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

        if (myStats != null)
        {
            myStats.OnDizzyStart.AddListener(() => SetState(STATE.DIZZIED));
            myStats.OnDeath.AddListener(() => SetState(STATE.DEAD));
            myStats.OnDizzyEnd.AddListener(() => SetState(STATE.IDLE));
        }

        // Movement Database Details
        lightPunch = LoadCharacterMove(characterName, "lightPunch");
        kick = LoadCharacterMove(characterName, "kick");
        superMove = LoadCharacterMove(characterName, "superMove");
        forwardDash = LoadCharacterMove(characterName, "forwardDash");
        backDash = LoadCharacterMove(characterName, "backDash");
        meter1 = LoadCharacterMove(characterName, "meter1");
        meter2 = LoadCharacterMove(characterName, "meter2");
        meter3 = LoadCharacterMove(characterName, "meter3");

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

    /// <summary>
    /// Handles the input given a certain input sequence from the player.
    /// -TODO- Edit to use the perform move function
    /// </summary>
    void HandleInput()
    {
        //string inputToRemove = "";
        if (Input.anyKey)
        {
            // if (inputReaderScript.FindInput("jjj"))
            // {
            //     Debug.Log("Perform <color=yellow>placeholder CHAIN ATTACK FINAL</color>.");


            //     Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>"); 
                
            //     //inputToRemove = "jjj";
            //     //Debug.Log("Index of Input Sequence: " + inputReaderScript.inputBuffer.IndexOf("jjj"));
            // } else if (inputReaderScript.FindInput("jj"))
            // {
            //     Debug.Log("Perform <color=yellow>placeholder CHAIN ATTACK SECOND</color>.");


            //     Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>"); 
            // }
            // else 
            if (inputReaderScript.FindInput(backDash.input))
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
            else if (inputReaderScript.FindInput(meter1.input))
            {
                Debug.Log("Perform <color=yellow>METER 1</color>.");

                if (myStats.TrySpendMeter(100f)) 
                {
                    PerformSuperMove("Meter1");
                }
                else
                {
                    Debug.Log("Not enough Meter!");
                }

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                // inputReaderScript.inputBuffer = "";
            }

            else if (inputReaderScript.FindInput(meter2.input))
            {
                Debug.Log("Perform <color=yellow>METER 2</color>.");

                if (myStats.TrySpendMeter(200f)) 
                {
                    PerformSuperMove("Meter2");
                }
                else
                {
                    Debug.Log("Not enough Meter!");
                }

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                // inputReaderScript.inputBuffer = "";
            }

            else if (inputReaderScript.FindInput(meter3.input))
            {
                Debug.Log("Perform <color=yellow>METER 3</color>.");

                if (myStats.TrySpendMeter(300f)) 
                {
                    PerformSuperMove("Meter3");
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

                PerformAttack("Attack", jabDamage, 1);

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                
                // C. Lock the state
                //SetState(STATE.ATTACKING);
            }
            else if (inputReaderScript.FindInput(kick.input))
            {
                Debug.Log("Perform <color=purple>KICK</color>.");

                PerformAttack("Kick", kickDamage, 2);

                Debug.Log("Successful Input: <color=green>" + inputReaderScript.inputBuffer + "</color>");
                // inputReaderScript.inputBuffer = "";
                
                // 3. Lock State
                //SetState(STATE.ATTACKING);
            }
        }

        //inputReaderScript.RemoveInputSequence(inputToRemove);
    }

    // TODO: Edit this to read from database
    void Update()
    {
        // 1. Disable inputs if Dead/Dizzied
        if (currentState == (int)STATE.DIZZIED || currentState == (int)STATE.DEAD)
        {
            HandleStates(); 
            myRigidBody.linearVelocity = velocity;
            return; 
        }

        // --- INPUT LOGIC (ONLY IF PLAYER) ---
        if (isPlayer)
        {
            // Basic Movement
            hDirection = Input.GetAxisRaw("Horizontal");
            isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
            isBlocking = Input.GetKey(KeyCode.S); 

            // Variable Jump Gravity (Short Hop)
            // if (myRigidBody.linearVelocity.y > 0 && !Input.GetKey(KeyCode.W))
            // {
            //     myRigidBody.linearVelocity += Vector2.up * Physics2D.gravity.y * 2.5f * Time.deltaTime;
            // }

            // update input logic, previous code within the method
            HandleInput();
        }
        else
        {
            // --- DUMMY LOGIC (Force Stop) ---
            hDirection = 0;
            isJumping = false;
            isBlocking = false;
            isAttacking = false;
        }

        // --- SHARED LOGIC (Gravity, Physics, Facing) ---

        // Facing Logic
        if (enemyTarget != null)
        {
            if (transform.position.x > enemyTarget.position.x) mySpriteRenderer.flipX = true;  
            else mySpriteRenderer.flipX = false; 
        }

        // Gravity State Check (FLOATING)
        // if (!isGrounded)
        // {
        //     if (velocity.y > 0) SetState(STATE.JUMPING);
        //     else SetState(STATE.FALLING);
        // } 

        HandleStates();
        UpdateAnimator();

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

    // --- ATTACK HELPERS ---

    void PerformAttack(string triggerName, float dmg, int level)
    {
        Debug.Log($"ACTION: {triggerName} | Level {level} | Previous Level: {attackLevel}");
        velocity.x = 0;
        isAttacking = true;
        attackLevel = level; 
        attackTimer = 0;     
        //attackCoolDownDuration = duration;

        myAnimator.SetTrigger(triggerName);
        SetState(STATE.ATTACKING);

        // EDIT to use the total frames from the move database
        if (triggerName == "Attack" && midJabHitBox != null) 
            attackCoolDownDuration = lightPunch.totalFrames;
            midJabHitBox.GetComponent<Hitbox>().damage = dmg;
        
        if (triggerName == "Kick" && kickHitBox != null) 
            attackCoolDownDuration = kick.totalFrames;
            kickHitBox.GetComponent<Hitbox>().damage = dmg;

        // myAnimator.SetTrigger(triggerName);
        // SetState(STATE.ATTACKING);
    }

    void PerformSuperMove(string triggerName)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = 3; // Max Level
        attackTimer = 0;
        // attackCoolDownDuration = duration;

        switch (triggerName)
        {
            case "Meter1":
                attackCoolDownDuration = meter1.totalFrames;
                break;
            case "Meter2":
                attackCoolDownDuration = meter2.totalFrames;
                break;
            case "Meter3":
                attackCoolDownDuration = meter3.totalFrames;
                break;
        }
        
        myAnimator.SetTrigger(triggerName);
        SetState(STATE.ATTACKING);
    }

    // --- ANIMATION EVENTS ---

    public void CastBlue() 
    {
        SpawnProjectileAtLocation(blueProjectilePrefab, meter1Damage, firePoint.position);
    }

    public void CastPurple() 
    {
        if (camShake != null) StartCoroutine(camShake.Shake(0.5f, 0.3f));
        if (screenDimmer != null) screenDimmer.TriggerDim(0.5f);
        SpawnProjectileAtLocation(purpleProjectilePrefab, meter3Damage, firePoint.position);
    }

    public void CastRedAtEnemy() 
    {
        if (redScreenFlash != null) redScreenFlash.TriggerFlash();

        Vector3 spawnPosition;
        float facingDir = mySpriteRenderer.flipX ? -1f : 1f;

        if (enemyTarget != null)
        {
            spawnPosition = enemyTarget.position + new Vector3(redSpawnOffset * facingDir, 0, 0);
            spawnPosition.y = firePoint.position.y; 
        }
        else
        {
            spawnPosition = firePoint.position; 
        }

        SpawnProjectileAtLocation(redProjectilePrefab, meter2Damage, spawnPosition);
    }

    public void TeleportToEnemy()
    {
        if (enemyTarget != null)
        {
            float directionToEnemy = Mathf.Sign(enemyTarget.position.x - transform.position.x);
            Vector3 targetPos = enemyTarget.position;
            targetPos.x -= (directionToEnemy * redSpawnOffset); 
            transform.position = targetPos;
            mySpriteRenderer.flipX = (directionToEnemy < 0);
        }
    }

    void SpawnProjectileAtLocation(GameObject prefab, float damageValue, Vector3 location)
    {
        if (prefab != null)
        {
            GameObject proj = Instantiate(prefab, location, Quaternion.identity);
            ProjectileController pc = proj.GetComponent<ProjectileController>();
            if (pc != null) pc.damage = damageValue;
            
            if (mySpriteRenderer.flipX) proj.transform.Rotate(0, 180, 0);
        }
    }

    // --- DAMAGE & STATES ---

    public void GetHit(float damage, float stunDamage)
    {
        if (currentState == (int)STATE.DEAD) return;
        
        if (currentState == (int)STATE.BLOCKING)
        {
            float chipDamage = damage * 0.1f; 
            if(myStats != null) myStats.BlockAttack(chipDamage, 5f);
        }
        else
        {
            if(myStats != null) myStats.TakeDamage(damage, 10f, stunDamage);
            StartCoroutine(FlashRed());
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        mySpriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (currentState != (int)STATE.DIZZIED) mySpriteRenderer.color = Color.white;
    }

    void UpdateAnimator()
    {
        if (myAnimator != null)
        {
            float facingMult = mySpriteRenderer.flipX ? -1 : 1;
            float directionalSpeed = velocity.x * facingMult;
            float normalizedSpeed = 0;
            if(Mathf.Abs(directionalSpeed) > 0.1f) normalizedSpeed = directionalSpeed / Mathf.Abs(directionalSpeed);

            myAnimator.SetFloat("Speed", Mathf.Abs(velocity.x));
            myAnimator.SetFloat("WalkDirection", normalizedSpeed);
            myAnimator.SetBool("IsBlocking", currentState == (int)STATE.BLOCKING);
            myAnimator.SetFloat("VerticalSpeed", velocity.y);
            myAnimator.SetBool("IsGrounded", isGrounded);
            myAnimator.SetBool("isDash", currentState == GetState(STATE.FDASHING));
            myAnimator.SetBool("isBackDash", currentState == GetState(STATE.BDASHING));
        }
    }

    // --- STATE MACHINE ---

    void HandleStates()
    {
        switch (currentState)
        {   
            case (int) STATE.IDLE:      IdleState(); break;
            case (int) STATE.WALKING:   WalkingState(); break;
            case (int) STATE.JUMPING:   JumpingState(); break;
            case (int) STATE.FALLING:   FallingState(); break;
            case (int) STATE.ATTACKING: AttackingState(); break;
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
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void WalkingState()
    {
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime;
        float absVelocity = Math.Abs(velocity.x);
        velocity.x = Math.Clamp(absVelocity, 0, maxSpeed) * hDirection;
        if (hDirection == 0 && isGrounded) SetState(STATE.IDLE);
        if (isJumping && isGrounded) SetState(STATE.JUMPING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void BlockingState() { velocity.x = 0; if (!isBlocking) SetState(STATE.IDLE); }

    void AttackingState()
    {
        velocity.x = 0; 
        attackTimer += Time.deltaTime;
        if (attackTimer > attackCoolDownDuration) 
        {
            attackTimer = 0; 
            attackLevel = 0; // Reset Combo Level
            isAttacking = false;
            SetState(STATE.IDLE);
        }
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

    // void JumpingState() { velocity.y = jumpHeight; isGrounded = false; }
    // void FallingState() { velocity.y -= GRAVITY; if (isGrounded) SetState(STATE.IDLE); } 
    // UPDATED
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


    void DizziedState() { velocity.x = 0; mySpriteRenderer.color = Color.gray; }
    void DeadState() { velocity.x = 0; myRigidBody.simulated = false; mySpriteRenderer.color = Color.red; }

    // --- VISUAL DEBUGGER ---
    void OnGUI()
    {
        // Only show debug text for the Player, not the Dummy
        if (!isPlayer) return; 

        GUIStyle style = new GUIStyle();
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow; 

        // 1. Show State & Physics
        GUI.Label(new Rect(20, 20, 500, 40), "STATE: " + (STATE)currentState, style);
        GUI.Label(new Rect(20, 60, 500, 40), "Grounded: " + isGrounded, style);

        // 2. Show Combo Logic
        string comboStatus = "Level " + attackLevel;
        if (attackLevel == 0) comboStatus += " (Ready)";
        if (attackLevel == 1) comboStatus += " (Light -> Cancel into Kick/Special)";
        if (attackLevel == 2) comboStatus += " (Heavy -> Cancel into Special)";
        if (attackLevel == 3) comboStatus += " (MAX - Finish Move)";
        
        GUI.Label(new Rect(20, 120, 800, 40), "COMBO CHAIN: " + comboStatus, style);

        // 3. Show Timer (How much time left to cancel?)
        if (isAttacking)
        {
            float timeLeft = attackCoolDownDuration - attackTimer;
            string timerText = "Window Left: " + timeLeft.ToString("F2") + "s";
            GUI.Label(new Rect(20, 160, 500, 40), timerText, style);
        }
    }

    void OnCollisionStay2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = true; }
    void OnCollisionExit2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = false; }
}