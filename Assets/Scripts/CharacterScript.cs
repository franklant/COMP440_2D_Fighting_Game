using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // Required for Dictionary

public class CharacterScript : MonoBehaviour
{
    [Header("--- Control Settings ---")]
    public bool isPlayer = true; 
    public string characterName = "Gojo";

    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public Animator myAnimator; 
    public FighterStatsManager myStats; 
    public GameObject myGroundCheck;

    [Header("--- VFX System ---")]
    private Dictionary<string, AnimationClip> vfxDatabase = new Dictionary<string, AnimationClip>();

    [Header("--- VFX References (Screen Effects) ---")]
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
    public float jabDamage = 30f;
    public float kickDamage = 40f;
    public float mediumDamage = 60f;
    public float heavyDamage = 80f;
    public float crouchDamage = 90f;
    
    public float meter1Damage = 200f; 
    public float meter2Damage = 400f; 
    public float meter3Damage = 600f; 

    [Header("--- Input Configuration ---")]
    public KeyCode comboKeyA = KeyCode.Z;  
    public KeyCode comboKeyB = KeyCode.X; 
    
    [Header("--- Specials Inputs ---")]
    public KeyCode keyBlueSpin = KeyCode.U;
    public KeyCode keyBlueCast = KeyCode.I;
    public KeyCode keyRedCast = KeyCode.O;
    public KeyCode keyRedBack = KeyCode.L;
    public KeyCode keySimpleDomain = KeyCode.J;
    public KeyCode keyKnockback = KeyCode.K;

    [Header("--- Supers Inputs ---")]
    public KeyCode keyPurpleUnlimited = KeyCode.P;
    public KeyCode keyPurpleHollow = KeyCode.M;
    public KeyCode keyDomainExpansion = KeyCode.Return;

    [Header("--- Combo System ---")]
    public float comboWindow = 1.0f;     
    private float lastComboInputTime = 0f;
    private int currentComboIndex = 0;
    private string activeChain = ""; 

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
    
    private float hDirection = 0;
    private float vDirection = 0;

    // Status Flags
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    public bool isBlocking; 
    public bool isCrouching;

    private enum STATE {
        IDLE = 0, WALKING = 1, JUMPING = 2, FALLING = 3,
        ATTACKING = 4, DEAD = 5, BLOCKING = 6, CROUCHING = 7, 
        FDASHING = 8, BDASHING = 9
    };

    void SetState(STATE state) { currentState = (int) state; }

    void Start()
    {
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>(); 
        myStats = GetComponent<FighterStatsManager>();
        
        if (myGroundCheck == null) myGroundCheck = GameObject.FindGameObjectWithTag("GroundCheck");
        
        // Removed Hitbox logic for now as requested
        
        if (myStats != null) myStats.OnDeath.AddListener(EnableDeadState);
        
        // --- VFX LOADING ---
        // Loads all clips from "Assets/Resources/Chars/Gojo/Animations/_VFX" into memory
        AnimationClip[] vfxClips = Resources.LoadAll<AnimationClip>("Chars/Gojo/Animations/_VFX");
        foreach (AnimationClip clip in vfxClips)
        {
            string[] parts = clip.name.Split('_'); // Splits "6040_Dust" into "6040" and "Dust"
            if (parts.Length > 0)
            {
                if (!vfxDatabase.ContainsKey(parts[0]))
                {
                    vfxDatabase.Add(parts[0], clip);
                }
            }
        }
        // Debug.Log($"Loaded {vfxDatabase.Count} VFX Clips.");

        SetState(STATE.IDLE);
        isGrounded = false;
    }

    // --- NEW: VFX SPAWNER (Called by Animation Events) ---
    public void SpawnVFX(string fxID)
    {
        if (vfxDatabase.ContainsKey(fxID))
        {
            GameObject vfxObj = new GameObject("VFX_" + fxID);
            vfxObj.transform.position = transform.position;
            MugenVfxObject handler = vfxObj.AddComponent<MugenVfxObject>();
            handler.Setup(vfxDatabase[fxID], mySpriteRenderer.flipX, 5); // Order 5 = In front
        }
    }

    void Update()
    {
        if (currentState == (int)STATE.DEAD) { myRigidBody.linearVelocity = velocity; return; }

        if (isPlayer)
        {
            hDirection = Input.GetAxisRaw("Horizontal");
            vDirection = Input.GetAxisRaw("Vertical");
            
            // Jump Trigger
            if (Input.GetKeyDown(KeyCode.W) && isGrounded) isJumping = true;
            else isJumping = false;

            isBlocking = Input.GetKey(KeyCode.S) && hDirection == 0; 

            // --- COMBOS ---
            if (Input.GetKeyDown(comboKeyA)) HandleComboInput("A");
            if (Input.GetKeyDown(comboKeyB)) HandleComboInput("B");

            // --- SPECIALS (100 Meter Cost = 1 Bar) ---
            // U: Blue Orb Spin (1300) - Cost 100
            if (Input.GetKeyDown(keyBlueSpin) && myStats.TrySpendMeter(100f)) 
                PerformSpecial("1300_Blue_Orb_Spin", 1.2f);

            // J: Simple Domain (1400) - Cost 10 (Cheap)
            if (Input.GetKeyDown(keySimpleDomain) && myStats.TrySpendMeter(10f)) 
                PerformSpecial("1400_Simple_Domain", 1.0f);

            // K: Knockback (1500) - Cost 50
            if (Input.GetKeyDown(keyKnockback) && myStats.TrySpendMeter(50f)) 
                PerformSpecial("1500_Knockback_Burst", 1.2f);

            // L: Red Backwards (1600) - Cost 150
            if (Input.GetKeyDown(keyRedBack) && myStats.TrySpendMeter(150f)) 
            {
                PerformSpecial("1600_Red_Backwards", 1.0f);
                float backDir = mySpriteRenderer.flipX ? 1f : -1f; 
                velocity.x = backDir * 10f; // Add burst of speed backwards
            }

            // I: Blue Cast (1700) - Cost 100
            if (Input.GetKeyDown(keyBlueCast) && myStats.TrySpendMeter(100f)) 
                PerformSpecial("1700_Blue_Cast", 1.5f);

            // O: Red Cast (1800) - Cost 100
            if (Input.GetKeyDown(keyRedCast) && myStats.TrySpendMeter(100f)) 
                PerformSpecial("1800_Red_Cast", 1.5f);

            // --- SUPERS ---
            // M: Hollow Purple Nuke (1900) - Cost 50
            if (Input.GetKeyDown(keyPurpleHollow) && myStats.TrySpendMeter(50f)) 
                PerformSpecial("1900_Purple_Hollow", 2.5f);

            // P: Unlimited Purple (3000) - Cost 300
            if (Input.GetKeyDown(keyPurpleUnlimited) && myStats.TrySpendMeter(300f)) 
                PerformSpecial("3000_Unlimited_Purple", 3.0f);

            // ENTER: Domain Expansion (3100) - Cost 300
            if (Input.GetKeyDown(keyDomainExpansion) && myStats.TrySpendMeter(300f)) 
            {
                PerformSpecial("3100_Domain_Expansion_Void", 4.0f);
                if(screenDimmer != null) screenDimmer.TriggerDim(3.5f);
            }

            // Reset combo
            if (Time.time > lastComboInputTime + comboWindow && currentComboIndex > 0 && !isAttacking) {
                currentComboIndex = 0; activeChain = "";
            }
        }
        else
        {
            hDirection = 0; vDirection = 0; isJumping = false; isBlocking = false; isAttacking = false;
        }

        // Facing Logic
        if (enemyTarget != null && !isAttacking)
        {
            if (transform.position.x > enemyTarget.position.x) mySpriteRenderer.flipX = true;  
            else mySpriteRenderer.flipX = false; 
        }

        HandleStates();
        UpdateAnimator();

        // --- PHYSICS ---
        if (isGrounded) 
        { 
            if (currentState != (int)STATE.JUMPING) velocity.y = 0; 
        }
        else 
        { 
            // Apply gravity if airborn
            velocity.y -= GRAVITY; 
        }
        
        myRigidBody.linearVelocity = new Vector2(velocity.x, myRigidBody.linearVelocity.y);
    }

    // --- STATE MACHINE ---
    void HandleStates()
    {
        switch (currentState)
        {   
            case (int) STATE.IDLE:      IdleState(); break;
            case (int) STATE.WALKING:   WalkingState(); break;
            case (int) STATE.CROUCHING: CrouchingState(); break;
            case (int) STATE.JUMPING:   JumpingState(); break;
            case (int) STATE.FALLING:   FallingState(); break;
            case (int) STATE.ATTACKING: AttackingState(); break;
            case (int) STATE.DEAD:      DeadState(); break;
            case (int) STATE.BLOCKING:  BlockingState(); break;
        }
    }

    void IdleState() 
    { 
        velocity.x = 0; 
        if (hDirection != 0) SetState(STATE.WALKING); 
        if (vDirection < 0) SetState(STATE.CROUCHING);
        if (isJumping && isGrounded) 
        { 
            velocity.y = jumpHeight; 
            isGrounded = false; 
            SetState(STATE.JUMPING); 
        } 
    }

    void WalkingState() 
    { 
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime; 
        float absVelocity = Mathf.Abs(velocity.x); 
        velocity.x = Mathf.Clamp(absVelocity, 0, maxSpeed) * hDirection; 
        
        if (hDirection == 0 && isGrounded) SetState(STATE.IDLE); 
        if (vDirection < 0) SetState(STATE.CROUCHING);
        
        if (isJumping && isGrounded) 
        { 
            velocity.y = jumpHeight; 
            isGrounded = false; 
            SetState(STATE.JUMPING); 
        } 
    }

    void CrouchingState()
    {
        velocity.x = 0; 
        isCrouching = true;
        if (vDirection >= 0) // Released Down
        {
            isCrouching = false;
            SetState(STATE.IDLE);
        }
    }

    void BlockingState() { velocity.x = 0; if (!isBlocking) SetState(STATE.IDLE); }
    
    void AttackingState() 
    { 
        // Slow down slide during attack
        velocity.x = Mathf.MoveTowards(velocity.x, 0, Time.deltaTime * 5f); 
        attackTimer += Time.deltaTime; 
        if (attackTimer > attackCoolDownDuration) 
        { 
            attackTimer = 0; 
            attackLevel = 0; 
            isAttacking = false; 
            SetState(STATE.IDLE); 
        } 
    }
    
    void JumpingState() 
    { 
        velocity.x = hDirection * movementSpeed; 
        velocity.y -= GRAVITY; 
        if (velocity.y < 0) SetState(STATE.FALLING); 
    } 
    
    void FallingState() 
    { 
        velocity.x = hDirection * movementSpeed; 
        velocity.y -= GRAVITY; 
        if (isGrounded) SetState(STATE.IDLE); 
    } 
    
    void DeadState() { velocity.x = 0; myRigidBody.simulated = false; mySpriteRenderer.color = Color.red; }

    // --- ANIMATOR MAPPING ---
    void UpdateAnimator()
    {
        if (myAnimator == null) return;
        if (isAttacking) return; 

        string animToPlay = "";

        switch (currentState)
        {
            case (int)STATE.IDLE:
                animToPlay = "0_Idle";
                break;

            case (int)STATE.WALKING:
                float facingDir = mySpriteRenderer.flipX ? -1f : 1f;
                bool movingForward = (hDirection * facingDir) > 0;
                animToPlay = movingForward ? "20_Walk_Fwd" : "21_Walk_Back";
                break;

            case (int)STATE.CROUCHING:
                animToPlay = "11_Crouch_GetUp"; 
                break;

            case (int)STATE.JUMPING:
                if (hDirection == 0) animToPlay = "41_Jump_Up";       
                else if (mySpriteRenderer.flipX) animToPlay = (hDirection < 0) ? "42_Jump_Fwd" : "43_Jump_Back";
                else animToPlay = (hDirection > 0) ? "42_Jump_Fwd" : "43_Jump_Back";
                break;

            case (int)STATE.FALLING:
                animToPlay = "41_Jump_Up"; // Reuse Jump Up for fall unless you have a fall clip
                break;
                
            case (int)STATE.BLOCKING:
                animToPlay = "0_Idle"; 
                break;
                
            case (int)STATE.DEAD:
                animToPlay = "5110_Hit_LieDown";
                break;
        }

        // Only switch if not already playing it
        if (!string.IsNullOrEmpty(animToPlay))
        {
            if (!myAnimator.GetCurrentAnimatorStateInfo(0).IsName(animToPlay))
            {
                myAnimator.Play(animToPlay);
            }
        }
    }

    // --- COMBOS ---
    void HandleComboInput(string inputType)
    {
        bool canStart = (currentState == (int)STATE.IDLE || currentState == (int)STATE.WALKING || currentState == (int)STATE.CROUCHING);
        bool canContinue = (currentState == (int)STATE.ATTACKING && isAttacking); 

        if (canStart || canContinue)
        {
            if (canStart) { activeChain = inputType; currentComboIndex = 0; }
            
            lastComboInputTime = Time.time;
            currentComboIndex++;

            string animToPlay = "";
            float duration = 0.4f; 

            // Crouch Attack Override
            if (isCrouching) 
            { 
                animToPlay = "240_Launcher_Air"; 
                PerformAttack(animToPlay, crouchDamage, 0.5f, 1); 
                return; 
            }

            // Chain A (Z Key)
            if (activeChain == "A")
            {
                switch (currentComboIndex)
                {
                    case 1: animToPlay = "200_Jab_Light"; duration = 0.4f; break;
                    case 2: animToPlay = "210_Kick_Light"; duration = 0.5f; break;
                    case 3: animToPlay = "220_Attack_Medium"; duration = 0.6f; break;
                    case 4: animToPlay = "230_Attack_Heavy"; duration = 0.8f; break;
                    case 5: animToPlay = "231_Teleport_CrossUp"; duration = 0.3f; PerformTeleport(); break;
                    case 6: animToPlay = "240_Launcher_Air"; duration = 0.8f; break;
                    default: currentComboIndex = 1; animToPlay = "200_Jab_Light"; duration = 0.4f; break;
                }
            }
            // Chain B (X Key)
            else if (activeChain == "B")
            {
                switch (currentComboIndex)
                {
                    case 1: animToPlay = "300_Forward_Strike_1"; duration = 0.4f; break;
                    case 2: animToPlay = "310_Forward_Strike_2"; duration = 0.5f; break;
                    case 3: animToPlay = "320_Forward_Strike_3"; duration = 0.6f; break;
                    case 4: animToPlay = "330_Forward_Strike_4"; duration = 0.6f; break;
                    case 5: animToPlay = "340_Forward_Strike_5"; duration = 0.6f; break;
                    case 6: animToPlay = "231_Teleport_CrossUp"; duration = 0.3f; PerformTeleport(); break;
                    case 7: animToPlay = "240_Launcher_Air"; duration = 0.8f; break;
                    default: currentComboIndex = 1; animToPlay = "300_Forward_Strike_1"; duration = 0.4f; break;
                }
            }
            PerformAttack(animToPlay, 30f, duration, currentComboIndex); 
        }
    }

    // --- ACTION HELPERS ---
    void PerformAttack(string stateName, float dmg, float duration, int level)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = level; 
        attackTimer = 0;     
        attackCoolDownDuration = duration;
        myAnimator.Play(stateName, 0, 0f);
        SetState(STATE.ATTACKING);
    }
    
    void PerformSpecial(string stateName, float duration)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = 5; 
        attackTimer = 0;
        attackCoolDownDuration = duration;
        myAnimator.Play(stateName, 0, 0f);
        SetState(STATE.ATTACKING);
    }

    void PerformTeleport()
    {
        if (enemyTarget != null)
        {
            Vector3 newPos = enemyTarget.position;
            float offset = (transform.position.x < enemyTarget.position.x) ? 1.5f : -1.5f;
            newPos.x += offset;
            transform.position = newPos;
            mySpriteRenderer.flipX = !mySpriteRenderer.flipX;
        }
    }

    public void GetHit(float damage)
    {
        if (currentState == (int)STATE.DEAD) return;
        if (myStats != null) myStats.TakeDamage(damage, 10f);
        string hurtAnim = isGrounded ? "5000_Hit_Stand" : "5030_Hit_Air";
        myAnimator.Play(hurtAnim, 0, 0f);
        StopAttack();
        StartCoroutine(FlashRed());
    }

    void StopAttack() { isAttacking = false; attackTimer = 0; }
    void EnableDeadState() { SetState(STATE.DEAD); }
    
    // Animation Event Hookups
    public void CastBlue() { SpawnProjectileAtLocation(blueProjectilePrefab, meter1Damage, firePoint.position); }
    public void CastPurple() { if (camShake != null) StartCoroutine(camShake.Shake(0.5f, 0.3f)); if (screenDimmer != null) screenDimmer.TriggerDim(0.5f); SpawnProjectileAtLocation(purpleProjectilePrefab, meter3Damage, firePoint.position); }
    public void CastRedAtEnemy() { if (redScreenFlash != null) redScreenFlash.TriggerFlash(); Vector3 spawnPosition; float facingDir = mySpriteRenderer.flipX ? -1f : 1f; if (enemyTarget != null) { spawnPosition = enemyTarget.position + new Vector3(redSpawnOffset * facingDir, 0, 0); spawnPosition.y = firePoint.position.y; } else { spawnPosition = firePoint.position; } SpawnProjectileAtLocation(redProjectilePrefab, meter2Damage, spawnPosition); }
    public void TeleportToEnemy() { if (enemyTarget != null) { float directionToEnemy = Mathf.Sign(enemyTarget.position.x - transform.position.x); Vector3 targetPos = enemyTarget.position; targetPos.x -= (directionToEnemy * redSpawnOffset); transform.position = targetPos; mySpriteRenderer.flipX = (directionToEnemy < 0); } }
    
    void SpawnProjectileAtLocation(GameObject prefab, float damageValue, Vector3 location) { if (prefab != null) { GameObject proj = Instantiate(prefab, location, Quaternion.identity); ProjectileController pc = proj.GetComponent<ProjectileController>(); if (pc != null) { pc.damage = damageValue; pc.ownerTag = gameObject.tag; } if (mySpriteRenderer.flipX) proj.transform.Rotate(0, 180, 0); } }
    System.Collections.IEnumerator FlashRed() { mySpriteRenderer.color = Color.red; yield return new WaitForSeconds(0.1f); mySpriteRenderer.color = Color.white; }
    void OnCollisionStay2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = true; }
    void OnCollisionExit2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = false; }

    void OnGUI()
    {
        if (!isPlayer) return; 
        GUIStyle style = new GUIStyle(); style.fontSize = 30; style.fontStyle = FontStyle.Bold; style.normal.textColor = Color.yellow; 
        GUI.Label(new Rect(20, 20, 500, 40), "STATE: " + (STATE)currentState, style);
        string comboStatus = "Hit " + currentComboIndex + " (" + activeChain + ")";
        GUI.Label(new Rect(20, 120, 800, 40), "COMBO CHAIN: " + comboStatus, style);
        if(myStats!=null) GUI.Label(new Rect(20, 160, 500, 40), "METER: " + myStats.CurrentHyper, style);
    }
}