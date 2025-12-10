using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterScript : MonoBehaviour
{
    [Header("--- Control Settings ---")]
    public bool isPlayer = true; 

    [Header("--- Character ---")]
    public string characterName;

    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public Animator myAnimator; 
    public FighterStatsManager myStats; 
    public GameObject myGroundCheck;

    [Header("--- Combat References ---")]
    public GameObject midJabHitBox;      
    public GameObject kickHitBox;        
    public Transform enemyTarget;        
    public Transform firePoint;          

    [Header("--- Projectile Settings ---")]
    public GameObject blueProjectilePrefab;   // Meter 1
    public GameObject redProjectilePrefab;    // Meter 2
    public GameObject purpleProjectilePrefab; // Meter 3
    public float redSpawnOffset = 1.5f; 

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
    public int attackLevel = 0; 
    public float dashDuration = 0f;
    public float dashTimer = 0f;

    [Header("--- AUDIO ---")]
    public GameObject audioManager;
    private AudioManager audioScript;

    [Header("--- Input & Data ---")]
    public NewInputReaderScript inputReaderScript;
    public GameObject moveDatabase;
    private MovementDataManager moveDatabaseManagerScript;

    // Moves
   // Moves
    public MoveDetails lightPunch, kick, superMove, forwardDash, backDash;
    public MoveDetails meter1, meter2, meter3;
    
    [HideInInspector] // Hides it from the Inspector but keeps it public for the AI script
    public float hDirection = 0; 

    // Flags
    public bool isGrounded, isAttacking; // These are read-only state flags
    
    [HideInInspector]
    public bool isJumping, isBlocking; // These are input flags set by Player or AI
    // FIX: Make the STATE enum public so AIController can use it.
    public enum STATE { 
        IDLE = 0, WALKING = 1, JUMPING = 2, FALLING = 3,
        ATTACKING = 4, DIZZIED = 5, DEAD = 6, BLOCKING = 7,
        FDASHING = 8, BDASHING = 9, KNOCKBACK = 10, AERIALKNOCKBACK = 11
    };

    // FIX: Make GetState public so AIController can check the current state.
    public int GetState(STATE state) { return (int) state; }

    // NOTE: SetState should remain private/internal as only the CharacterScript should change its own state directly.
    void SetState(STATE state) { currentState = (int) state; }
    Vector3 originalMidBoxPosition, reflectedMidBoxPosition;
    Vector3 originalKickBoxPosition, reflectedKickBoxPosition;

    // VFX
    public ScreenFlashEffect redScreenFlash; 
    public CameraShake camShake; 
    public ScreenDimmer screenDimmer;

    void Start()
    {
        if (isPlayer)   
            characterName = PlayerPrefs.GetString("selectedCharacter");
        if (name == "AIFighter")
            characterName = name;
        
        if (string.IsNullOrEmpty(characterName)) characterName = "Gojo"; 

        // Component Fetching
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>(); 
        myStats = GetComponent<FighterStatsManager>();
        moveDatabase = GameObject.FindGameObjectWithTag("MoveDB");
        if (myGroundCheck == null) myGroundCheck = GameObject.FindGameObjectWithTag("GroundCheck");
        if (midJabHitBox == null) midJabHitBox = GameObject.FindGameObjectWithTag("MidJabHitBox");
        inputReaderScript = GetComponent<NewInputReaderScript>();

        if (moveDatabase != null) moveDatabaseManagerScript = moveDatabase.GetComponent<MovementDataManager>();

        // VFX Fetching
        GameObject dimObj = GameObject.FindGameObjectWithTag("ScreenDimmer");
        if (dimObj) screenDimmer = dimObj.GetComponent<ScreenDimmer>();
        GameObject flashObj = GameObject.FindGameObjectWithTag("ScreenFlash");
        if (flashObj) redScreenFlash = flashObj.GetComponent<ScreenFlashEffect>();

        // Hitbox Setup
        if (midJabHitBox) {
            originalMidBoxPosition = midJabHitBox.transform.localPosition;
            reflectedMidBoxPosition = Vector3.Reflect(midJabHitBox.transform.localPosition, Vector3.right);
        }
        if (kickHitBox) {
            originalKickBoxPosition = kickHitBox.transform.localPosition;
            reflectedKickBoxPosition = Vector3.Reflect(kickHitBox.transform.localPosition, Vector3.right);
        }

        // character requires more offsets
        if (characterName == "Naruto" && CompareTag("Player1"))
        {
            reflectedMidBoxPosition -= (Vector3.left * 0.5f);
            reflectedKickBoxPosition -= (Vector3.left * 0.5f);
        }

        // Stats Listeners
        if (myStats != null) {
            myStats.OnDizzyStart.AddListener(() => SetState(STATE.DIZZIED));
            myStats.OnDeath.AddListener(() => SetState(STATE.DEAD));
            myStats.OnDizzyEnd.AddListener(() => SetState(STATE.IDLE));
        }

        // Target Search
        if (CompareTag("Player1")) StartCoroutine(SearchForEnemy());
        if (CompareTag("Player2")) StartCoroutine(SearchForPlayer());

        // Load Database
        LoadAllMoves();

        audioScript = AudioManager.Instance;

        SetState(STATE.IDLE);
        isGrounded = false;
    }

    void LoadAllMoves()
    {
        if (moveDatabaseManagerScript == null) return;
        lightPunch = LoadCharacterMove(characterName, "lightPunch");
        kick = LoadCharacterMove(characterName, "kick");
        superMove = LoadCharacterMove(characterName, "superMove");
        forwardDash = LoadCharacterMove(characterName, "forwardDash");
        backDash = LoadCharacterMove(characterName, "backDash");
        meter1 = LoadCharacterMove(characterName, "meter1");
        meter2 = LoadCharacterMove(characterName, "meter2");
        meter3 = LoadCharacterMove(characterName, "meter3");
    }

    public MoveDetails LoadCharacterMove(string charName, string moveName) {
        if (moveDatabaseManagerScript == null) return null;
        return moveDatabaseManagerScript.GetMove(charName, moveName);
    }

    IEnumerator SearchForPlayer() {
        yield return new WaitForSeconds(0.1f);
        GameObject target = GameObject.FindGameObjectWithTag("Player1");
        if(target != null) enemyTarget = target.transform;
    }

    IEnumerator SearchForEnemy() {
        yield return new WaitForSeconds(0.1f);
        GameObject target = GameObject.FindGameObjectWithTag("Player2");
        if(target != null) enemyTarget = target.transform;
    }

    // =========================================================
    //               HYBRID INPUT HANDLER
    // =========================================================
    void HandleInput()
    {
        if (inputReaderScript.controlKeyPressed)
        {
            // --- DASHING ---
            if (inputReaderScript.FindInput(backDash.input)) {
                dashTimer = 0; SetState(STATE.BDASHING);
                audioScript.PlayWhoosh();
            }
            else if (inputReaderScript.FindInput(forwardDash.input)) {
                dashTimer = 0; SetState(STATE.FDASHING);
                audioScript.PlayWhoosh();
            }

            // --- METER 1 ---
            else if (inputReaderScript.FindInput(meter1.input))
            {
                if (myStats.TrySpendMeter(100f)) 
                {
                    if (characterName == "Gojo") StartCoroutine(Gojo_Meter1());
                    else if (characterName == "Sukuna") StartCoroutine(Sukuna_Meter1());
                    else PerformSuperMove("Meter1", meter1); // Generic Fallback
                    audioScript.PlaySpecial();
                }
            }
            // --- METER 2 ---
            else if (inputReaderScript.FindInput(meter2.input))
            {
                if (myStats.TrySpendMeter(200f)) 
                {
                    if (characterName == "Gojo") StartCoroutine(Gojo_Meter2());
                    else if (characterName == "Sukuna") StartCoroutine(Sukuna_Meter2());
                    else PerformSuperMove("Meter2", meter2);
                    audioScript.PlaySpecial();
                }
            }
            // --- METER 3 ---
            else if (inputReaderScript.FindInput(meter3.input))
            {
                if (myStats.TrySpendMeter(300f)) 
                {
                    if (characterName == "Gojo") StartCoroutine(Gojo_Meter3());
                    else if (characterName == "Sukuna") StartCoroutine(Sukuna_Meter3());
                    else PerformSuperMove("Meter3", meter3);
                    audioScript.PlaySpecial();
                }
            }

            // --- NORMALS ---
            else if (inputReaderScript.FindInput(lightPunch.input)) {
                PerformAttack("Attack", lightPunch.damage, 1, lightPunch);
                audioScript.PlayPunch();
            }
            else if (inputReaderScript.FindInput(kick.input)) {
                PerformAttack("Kick", kick.damage, 2, kick);
                audioScript.PlayKick();
            }
        }
    }

    // =========================================================
    //               GENERIC SYSTEM (Naruto, etc.)
    // =========================================================
    
    // 1. The Trigger Setter
    public void PerformSuperMove(string triggerName, MoveDetails moveData)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = 3; 
        attackTimer = 0;

        if (moveData != null) attackCoolDownDuration = moveData.totalFrames / 60f;
        else attackCoolDownDuration = 1.0f;
        
        myAnimator.SetTrigger(triggerName); // <--- Triggers transition arrow
        SetState(STATE.ATTACKING);
    }

    // 2. The Animation Events (CALLED BY ANIMATION CLIPS)
    public void CastBlue() {
        if (screenDimmer != null) screenDimmer.TriggerDim(0.5f);
        SpawnProjectileAtLocation(blueProjectilePrefab, meter1 != null ? meter1.damage : 200, firePoint.position);
    }

    public void CastRedAtEnemy() {
        if (redScreenFlash != null) redScreenFlash.TriggerFlash();
        Vector3 spawnPosition;
        float facingDir = mySpriteRenderer.flipX ? -1f : 1f;

        if (enemyTarget != null) {
            spawnPosition = enemyTarget.position + new Vector3(redSpawnOffset * facingDir, 0, 0);
            spawnPosition.y = firePoint.position.y; 
        } else {
            spawnPosition = firePoint.position; 
        }
        SpawnProjectileAtLocation(redProjectilePrefab, meter2 != null ? meter2.damage : 400, spawnPosition);
    }

    public void CastPurple() {
        if (camShake != null) StartCoroutine(camShake.Shake(0.5f, 0.3f));
        if (screenDimmer != null) screenDimmer.TriggerDim(0.8f);
        SpawnProjectileAtLocation(purpleProjectilePrefab, meter3 != null ? meter3.damage : 600, firePoint.position);
    }


    // =========================================================
    //               GOJO SYSTEM (Coroutines)
    // =========================================================

    IEnumerator Gojo_Meter1() // BLUE
    {
        SetupSpecial("Action_1300"); // Play animation immediately
        if (screenDimmer) screenDimmer.TriggerDim(0.5f);

        yield return new WaitForSeconds(0.4f); // Timing for throw

        SpawnProjectileAtLocation(blueProjectilePrefab, meter1.damage, firePoint.position);
        
        FinishSpecial(meter1);
    }

    IEnumerator Gojo_Meter2() // RED
    {
        SetupSpecial("Action_1600");
        if (redScreenFlash) redScreenFlash.TriggerFlash();

        // Dodge logic
        yield return new WaitForSeconds(0.1f);
        transform.position += new Vector3(mySpriteRenderer.flipX ? 2f : -2f, 0, 0);

        yield return new WaitForSeconds(0.4f); // Flick time

        // Calculate Position
        Vector3 spawnPosition;
        float facingDir = mySpriteRenderer.flipX ? -1f : 1f;
        if (enemyTarget != null) {
            spawnPosition = enemyTarget.position + new Vector3(redSpawnOffset * facingDir, 0, 0);
            spawnPosition.y = firePoint.position.y; 
        } else {
            spawnPosition = firePoint.position + new Vector3(2.0f * facingDir, 0, 0); 
        }

        SpawnProjectileAtLocation(redProjectilePrefab, meter2.damage, spawnPosition);
        FinishSpecial(meter2);
    }

    IEnumerator Gojo_Meter3() // PURPLE
    {
        SetupSpecial("Action_1900");
        if (camShake) StartCoroutine(camShake.Shake(0.5f, 0.3f));
        if (screenDimmer) screenDimmer.TriggerDim(0.8f);

        yield return new WaitForSeconds(0.8f); // Charge time

        SpawnProjectileAtLocation(purpleProjectilePrefab, meter3.damage, firePoint.position);
        FinishSpecial(meter3);
    }

    // =========================================================
    //               SUKUNA SYSTEM (Placeholders)
    // =========================================================

    IEnumerator Sukuna_Meter1()
    {
        // Example: Dismantle
        SetupSpecial("Action_1300"); 
        yield return new WaitForSeconds(0.3f);
        SpawnProjectileAtLocation(blueProjectilePrefab, meter1.damage, firePoint.position); 
        FinishSpecial(meter1);
    }

    IEnumerator Sukuna_Meter2()
    {
        // Example: Cleave
        SetupSpecial("Action_1600"); 
        yield return new WaitForSeconds(0.4f);
        SpawnProjectileAtLocation(redProjectilePrefab, meter2.damage, firePoint.position);
        FinishSpecial(meter2);
    }

    IEnumerator Sukuna_Meter3()
    {
        // Example: Fire Arrow
        SetupSpecial("Action_1900"); 
        yield return new WaitForSeconds(1.0f);
        SpawnProjectileAtLocation(purpleProjectilePrefab, meter3.damage, firePoint.position);
        FinishSpecial(meter3);
    }

    // =========================================================
    //               SHARED HELPERS
    // =========================================================

    void SetupSpecial(string animName)
    {
        isAttacking = true;
        SetState(STATE.ATTACKING);
        velocity.x = 0;
        myAnimator.Play(animName); 
    }

    void FinishSpecial(MoveDetails moveData)
    {
        StartCoroutine(UnlockAfterDelay(0.5f));
    }

    IEnumerator UnlockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
        SetState(STATE.IDLE);
    }

    public void PerformAttack(string triggerName, float dmg, int level, MoveDetails moveData)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = level; 
        attackTimer = 0;     

        attackCoolDownDuration = (moveData != null) ? moveData.totalFrames / 60f : 0.5f;

        if (triggerName == "Kick")
        {
            AudioManager.Instance.PlayKick();
        }
        else // It's a punch or generic attack
        {
            AudioManager.Instance.PlayPunch();
        }

        if (triggerName == "Attack" && midJabHitBox) {
            var hb = midJabHitBox.GetComponent<Hitbox>();
            if(hb) { hb.damage = dmg; hb.isAerial = false; }
        }
        if (triggerName == "Kick" && kickHitBox) {
            var hb = kickHitBox.GetComponent<Hitbox>();
            if(hb) { hb.damage = dmg; hb.isAerial = true; }
        }

        // HYBRID ATTACKS:
        // If Gojo/Sukuna, Play() directly. Else Trigger().
        if (characterName == "Gojo" || characterName == "Sukuna")
        {
             if (triggerName == "Attack") myAnimator.Play("Action_200");
             else if (triggerName == "Kick") myAnimator.Play("Action_300");
             SetState(STATE.ATTACKING);
        }
        else
        {
            myAnimator.SetTrigger(triggerName);
            SetState(STATE.ATTACKING);
        }
    }

    void SpawnProjectileAtLocation(GameObject prefab, float damageValue, Vector3 location)
    {
        if (prefab != null)
        {
            GameObject proj = Instantiate(prefab, location, Quaternion.identity);
            ProjectileController pc = proj.GetComponent<ProjectileController>();
            if (pc != null) pc.damage = damageValue;
            
            if (mySpriteRenderer.flipX) 
            {
                proj.transform.Rotate(0, 180, 0);
                Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
                if(prb) prb.linearVelocity = new Vector2(-prb.linearVelocity.x, prb.linearVelocity.y);
            }
        }
    }

    void Update()
{
    // 1. Core State Checks (Must run first)
    if (myStats != null && myStats.CurrentHealth == 0) SetState(STATE.DEAD);

    if (currentState == (int)STATE.DIZZIED || currentState == (int)STATE.DEAD) {
        HandleStates(); 
        myRigidBody.linearVelocity = velocity;
        return; // Halt all normal movement/input processing
    }

    // 2. Input Acquisition
    if (isPlayer) 
    {
        // PLAYER: Read inputs directly from the keyboard/controller
        hDirection = Input.GetAxisRaw("Horizontal");
        isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        isBlocking = Input.GetKey(KeyCode.S); 
        
        // Only allow attack input if not already attacking
        if (!isAttacking) HandleInput(); 
    } 
    // AI: If isPlayer is FALSE, the AIController sets hDirection, isJumping, and isBlocking.
    // We remove the redundant 'else' block that previously zeroed out the AI's inputs:
    // REMOVED: else { hDirection = 0; isJumping = false; isBlocking = false; isAttacking = false; }


    // 3. Facing Logic (Always check facing regardless of control source)
    if (enemyTarget != null) 
    {
        // Face the opponent
        if (transform.position.x > enemyTarget.position.x) mySpriteRenderer.flipX = true;  
        else mySpriteRenderer.flipX = false; 
    }

    // 4. Hitbox Flipping Logic
    if (midJabHitBox && kickHitBox) 
    {
        if (mySpriteRenderer.flipX) 
        {
            midJabHitBox.transform.localPosition = reflectedMidBoxPosition;
            kickHitBox.transform.localPosition = reflectedKickBoxPosition;
        } 
        else 
        {
            midJabHitBox.transform.localPosition = originalMidBoxPosition;
            kickHitBox.transform.localPosition = originalKickBoxPosition;
        }
    }

    // 5. State Handling and Physics (Crucial: Must execute for BOTH Player and AI)
    
    // HandleStates contains the logic that uses hDirection to switch between IDLE/WALKING
    // and applies the jump velocity when isJumping is true.
    HandleStates(); 
    UpdateAnimator();

    if (isGrounded) velocity.y = 0;
    else velocity.y -= GRAVITY * Time.deltaTime;
    
    myRigidBody.linearVelocity = velocity;
}

    void UpdateAnimator()
    {
        if (myAnimator != null) {
            float facingMult = mySpriteRenderer.flipX ? -1 : 1;
            float directionalSpeed = velocity.x * facingMult;
            float normalizedSpeed = (Mathf.Abs(directionalSpeed) > 0.1f) ? directionalSpeed / Mathf.Abs(directionalSpeed) : 0;

            myAnimator.SetFloat("Speed", Mathf.Abs(velocity.x));
            myAnimator.SetFloat("WalkDirection", normalizedSpeed);
            myAnimator.SetBool("IsBlocking", currentState == (int)STATE.BLOCKING);
            myAnimator.SetFloat("VerticalSpeed", velocity.y);
            myAnimator.SetBool("IsGrounded", CheckGround());
            // Compatibility parameters
            myAnimator.SetBool("isDash", currentState == GetState(STATE.FDASHING));
            myAnimator.SetBool("isBackDash", currentState == GetState(STATE.BDASHING));
        }
    }

    // STATE HANDLERS
    void HandleStates() {
        switch (currentState) {   
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
            case (int) STATE.KNOCKBACK:  KnockBackState(); break;
            case (int) STATE.AERIALKNOCKBACK: AerialKnockBackState(); break;
        }
    }

    bool CheckGround()
    {
        List<Collider2D> results = new List<Collider2D>();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        int hitCount = GetComponent<CapsuleCollider2D>().Overlap(filter, results);

        if (hitCount > 0)
        {
            foreach (Collider2D c in results)
            {
                if (c.gameObject.CompareTag("Ground"))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    // --- STANDARD STATES ---
    
    void IdleState() {
        velocity.x = 0;
        if (hDirection != 0) SetState(STATE.WALKING);
        if (isJumping && CheckGround()) SetState(STATE.JUMPING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }

    void WalkingState() {
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime;
        float absVelocity = Mathf.Abs(velocity.x); // Fixed Math.Abs -> Mathf.Abs
        velocity.x = Mathf.Clamp(absVelocity, 0, maxSpeed) * hDirection; // Fixed Math.Clamp -> Mathf.Clamp
        if (hDirection == 0 && CheckGround()) SetState(STATE.IDLE);
        if (isJumping && CheckGround()) SetState(STATE.JUMPING);
        if (isBlocking) SetState(STATE.BLOCKING);
    }
    
    void BlockingState() { velocity.x = 0; if (!isBlocking) SetState(STATE.IDLE); }

    void AttackingState() {
        if(characterName == "Gojo" || characterName == "Sukuna") return; // Handled by coroutine
        
        velocity.x = 0; 
        attackTimer += Time.deltaTime;
        if (attackTimer > attackCoolDownDuration) {
            attackTimer = 0; attackLevel = 0; isAttacking = false; SetState(STATE.IDLE);
        }
    }

    void FDashingState() {
        float dur = (forwardDash != null) ? forwardDash.totalFrames / 60f : 0.3f;
        dashTimer += Time.deltaTime; velocity.x = maxSpeed + 2;
        if (dashTimer >= dur) { SetState(STATE.IDLE); dashTimer = 0; }
    }

    void BDashingState() {
        float dur = (backDash != null) ? backDash.totalFrames / 60f : 0.3f;
        dashTimer += Time.deltaTime; velocity.x = -maxSpeed - 2;
        if (dashTimer >= dur) { SetState(STATE.IDLE); dashTimer = 0; }
    }

    void JumpingState() {
        isGrounded = false; HandleAirControl();
        if (!applyGravity) { velocity.y = jumpHeight; applyGravity = true; }
        else velocity.y -= GRAVITY * Time.deltaTime;
        if (velocity.y <= jumpHeight - 2) { SetState(STATE.FALLING); applyGravity = false; }
    }

    void FallingState() {
        if (isBlocking) velocity.y -= GRAVITY * 5 * Time.deltaTime; 
        else velocity.y -= GRAVITY * Time.deltaTime;
        HandleAirControl();
        if (CheckGround()) { if (hDirection != 0) SetState(STATE.WALKING); else SetState(STATE.IDLE); }
    }

    void HandleAirControl() {
        velocity.x += hDirection * (movementSpeed / 2) * acceleration * Time.deltaTime;
        float absVelocity = Mathf.Abs(velocity.x); // Fixed Math.Abs
        velocity.x = Mathf.Clamp(absVelocity, 0, maxSpeed / 2) * hDirection; // Fixed Math.Clamp
    }

    void DizziedState() { velocity.x = 0; mySpriteRenderer.color = Color.gray; }
    void DeadState() { velocity.x = 0; myRigidBody.simulated = false; mySpriteRenderer.color = Color.red; }
    
    void KnockBackState() {
        float k = mySpriteRenderer.flipX ? 1 : -1;
        if (knockTimer < 0.2f) { velocity.x = k; knockTimer += Time.deltaTime; }
        else { velocity.x = 0; knockTimer = 0; SetState(STATE.IDLE); }
    }
    
    // --- UPDATED PHYSICS-BASED AERIAL KNOCKBACK ---
    void AerialKnockBackState()
    {
        isGrounded = false;
        
        // 1. Apply Gravity (So the launch eventually stops rising and falls)
        velocity.y -= GRAVITY * Time.deltaTime;

        // 2. Air Drag (Slows X movement)
        velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 10f);   

        // check if we're still not grounded
        if (velocity.y <= 0 && CheckGround())
        {
            velocity.x = 0;
            velocity.y = 0;
            aerialKnockTimer = 0;
            SetState(STATE.IDLE); 
        }
    }


    // =========================================================
    //               GET HIT FUNCTION
    // =========================================================

    public void GetHit(float damage, float stunDamage, bool isAerial)
    {
        if (currentState == (int)STATE.DEAD) return;
        
        if (currentState == (int)STATE.BLOCKING)
        {
            float chipDamage = damage * 0.1f; 
            if(myStats != null) myStats.BlockAttack(chipDamage, 5f);
            
            // --- ADD BLOCK SOUND ---
            AudioManager.Instance.PlayBlock();
        }
        else
        {
            if(myStats != null) myStats.TakeDamage(damage, 10f, stunDamage);
            
            // --- ADD HIT/VOICE SOUND ---
            AudioManager.Instance.PlayHit();
            // Optional: 50% chance to play a voice line when hit
            if (Random.value > 0.5f) AudioManager.Instance.VoiceLines(); 
            
            if (isAerial)
            {
                velocity.y = 15;
                SetState(STATE.AERIALKNOCKBACK);
            } else
            {
                SetState(STATE.KNOCKBACK);
            }
            StartCoroutine(FlashRed());
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        mySpriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (currentState != (int)STATE.DIZZIED) mySpriteRenderer.color = Color.white;
    }

    // --- COLLISION LOGIC ---

    bool applyGravity = false;
    float knockTimer = 0;
    float aerialKnockTimer = 0;

    void OnCollisionEnter2D(Collision2D collision) { 
        if (collision.collider.CompareTag("Ground")) isGrounded = true; 
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (enemyTarget != null) {
            if (collision.collider.CompareTag(enemyTarget.gameObject.tag) && !CheckGround() && velocity.y <= 0) {
                float myX = GetComponent<BoxCollider2D>().ClosestPoint(collision.collider.transform.position).x;
                float theirX = collision.collider.ClosestPoint(transform.position).x;
                float push = myX - theirX;
                if (push < 0) transform.position += new Vector3(Mathf.Abs(push + 0.1f), 0, 0); 
                else transform.position -= new Vector3(Mathf.Abs(push + 0.1f), 0, 0); 
            }
        }
    }
    void OnCollisionExit2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = false; }
}