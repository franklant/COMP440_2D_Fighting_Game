using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; 
using System.Text.RegularExpressions; 

public class CharacterScript : MonoBehaviour
{
    [Header("--- Debug Settings ---")]
    public bool enableVFX = true; 

    [Header("--- Control Settings ---")]
    public bool isPlayer = true; 
    public string characterName = "Sukuna"; 

    [Header("--- Components ---")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public Animator myAnimator; 
    public FighterStatsManager myStats; 
    public GameObject myGroundCheck;

    [Header("--- VFX System ---")]
    public MugenVfxDatabase vfxDataAsset; 
    private Dictionary<string, AnimationClip> vfxDatabase = new Dictionary<string, AnimationClip>();
    private HashSet<string> spawnedVFXTracker = new HashSet<string>();

    // Manual Scale Overrides
    private Dictionary<string, Vector3> vfxScales = new Dictionary<string, Vector3>()
    {
        { "1360", new Vector3(2.5f, 2.5f, 1f) }, 
        { "1361", new Vector3(1.5f, 1.5f, 1f) }, 
        { "1370", new Vector3(1.0f, 1.0f, 1f) }, 
        { "1450", new Vector3(0.3f, 0.3f, 1f) }, 
        { "1903", new Vector3(0.4f, 0.4f, 1f) },
        { "6040", new Vector3(0.5f, 0.5f, 1f) }, 
        { "6045", new Vector3(0.5f, 0.5f, 1f) } 
    };

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
    public GameObject blueProjectilePrefab;   
    public GameObject redProjectilePrefab;    
    public GameObject purpleProjectilePrefab; 
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
    
    [Header("--- Move Configuration (IDs) ---")]
    public string groundHit1 = "200_State";
    public string groundHit2 = "210_State";
    public string groundHit3 = "220_State";
    public string groundHit4 = "230_State";
    public string groundFinisher = "240_State";
    
    public string airHit1 = "600_State";
    public string airHit2 = "610_State";
    public string airHit3 = "620_State";
    
    public string special1 = "1000_State"; 
    public string special2 = "1100_State"; 
    public string special3 = "1200_State"; 
    public string special4 = "1300_State"; 
    public string special5 = "1400_State"; 
    public string special6 = "1500_State"; 
    
    public string super1 = "3000_State";   
    public string super2 = "3100_State";   

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
    // FIXED: Standardized variable names below
    private int comboIndex = 0; 
    private string activeChain = ""; 

    [Header("--- Physics Attributes ---")]
    public float GRAVITY = 0.098f;          
    public Vector3 velocity = Vector3.zero; 
    public float movementSpeed = 4f;
    public float maxSpeed = 8f;
    public float acceleration = 3f;
    public float jumpHeight = 22f; 
    public float dashSpeed = 12f;

    [Header("--- State Tracking ---")]
    public int currentState;
    // FIXED: Renamed to match usage in PerformAttack
    public float attackDuration = 0.4f; 
    public float attackTimer = 0;
    public int attackLevel = 0; 
    
    private float hDirection = 0;
    private float vDirection = 0;

    // Dash Input Vars
    private float lastTapTime = 0;
    private float tapSpeed = 0.3f;

    // Status Flags
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    public bool isBlocking; 
    public bool isCrouching;

    private enum STATE {
        IDLE = 0, WALKING = 1, JUMPING = 2, FALLING = 3,
        ATTACKING = 4, DEAD = 5, BLOCKING = 6, CROUCHING = 7, 
        DASHING = 8, AIR_ATTACK = 9
    };

    void SetState(STATE state) { currentState = (int) state; }

    void Start()
    {
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>(); 
        myStats = GetComponent<FighterStatsManager>();
        
        if (myGroundCheck == null) myGroundCheck = GameObject.FindGameObjectWithTag("GroundCheck");
        if (myStats != null) myStats.OnDeath.AddListener(EnableDeadState);
        
        // --- FUZZY VFX LOADING ---
        string path = $"Chars/{characterName}/Animations";
        AnimationClip[] allClips = Resources.LoadAll<AnimationClip>(path);
        
        foreach (AnimationClip clip in allClips)
        {
            string id = Regex.Match(clip.name, @"\d+").Value;
            
            if (!string.IsNullOrEmpty(id) && !vfxDatabase.ContainsKey(id))
            {
                vfxDatabase.Add(id, clip);
            }
        }
        
        SetState(STATE.IDLE);
        isGrounded = false;
    }

    public void SpawnVFX(string requestID)
    {
        if (!enableVFX) return;

        string finalAnimID = requestID;
        if (vfxDataAsset != null) finalAnimID = vfxDataAsset.GetAnimForState(requestID);

        // Prevent duplicates unless it's a dust effect
        if (spawnedVFXTracker.Contains(requestID) && requestID != "6040" && requestID != "6045") return; 

        if (vfxDatabase.ContainsKey(finalAnimID))
        {
            GameObject vfxObj = new GameObject($"VFX_{requestID}");
            vfxObj.transform.position = transform.position;
            
            MugenVfxDatabase.VfxProfile profile = null; 
            if(vfxDataAsset != null) {
                 profile = vfxDataAsset.GetProfile(requestID); // Try State ID
                 if(profile == null) profile = vfxDataAsset.GetProfile(finalAnimID); // Try Anim ID
            }
            
            Vector3 scale = Vector3.one;
            Vector3 vel = Vector3.zero;
            int priority = 5;
            bool isAdd = false;

            if(profile != null) 
            {
                scale = profile.scale;
                vel = profile.velocity;
                priority = profile.sprPriority + 5; // +5 to ensure it's above player default
                isAdd = profile.isAdditive;

                // --- POSTYPE LOGIC ---
                // If Back/Front/Left/Right -> Attach to Camera
                if (profile.posType != MugenVfxDatabase.PosType.P1 && profile.posType != MugenVfxDatabase.PosType.P2)
                {
                    if (Camera.main != null)
                    {
                        vfxObj.transform.SetParent(Camera.main.transform);
                        vfxObj.transform.localPosition = new Vector3(profile.offset.x, profile.offset.y, 10f); 
                        vfxObj.transform.localRotation = Quaternion.identity;
                    }
                }
                else
                {
                    // Player Relative
                    float flipMult = mySpriteRenderer.flipX ? -1f : 1f;
                    vfxObj.transform.position += new Vector3(profile.offset.x * flipMult, profile.offset.y, 0);
                    
                    // Handle Binding (Parenting)
                    if (profile.bindTime == -1 || profile.bindTime > 1)
                    {
                        vfxObj.transform.SetParent(transform);
                    }
                }
            }

            vfxObj.transform.localScale = scale;
            MugenVfxObject handler = vfxObj.AddComponent<MugenVfxObject>();
            handler.Setup(vfxDatabase[finalAnimID], mySpriteRenderer.flipX, priority, vel, isAdd); 

            spawnedVFXTracker.Add(requestID);
        }
    }
    
    void Update()
    {
        if (currentState == (int)STATE.DEAD) { myRigidBody.linearVelocity = velocity; return; }

        if (isPlayer)
        {
            hDirection = Input.GetAxisRaw("Horizontal");
            vDirection = Input.GetAxisRaw("Vertical");
            
            if (Input.GetKeyDown(KeyCode.W) && isGrounded) isJumping = true;
            else isJumping = false;

            isBlocking = Input.GetKey(KeyCode.S) && hDirection == 0; 

            HandleDashInput();

            if (Input.GetKeyDown(comboKeyA)) HandleComboInput("A");
            if (Input.GetKeyDown(comboKeyB)) HandleComboInput("B");

            // Specials
            if (Input.GetKeyDown(keyBlueSpin) && myStats.TrySpendMeter(100f)) 
                PerformSpecial("1300_State", 1.2f);

            if (Input.GetKeyDown(keySimpleDomain) && myStats.TrySpendMeter(10f)) 
                PerformSpecial("1400_State", 1.0f);

            if (Input.GetKeyDown(keyKnockback) && myStats.TrySpendMeter(50f)) 
                PerformSpecial("1500_State", 1.2f);

            if (Input.GetKeyDown(keyRedBack) && myStats.TrySpendMeter(150f)) 
            {
                PerformSpecial("1600_State", 1.0f);
                float backDir = mySpriteRenderer.flipX ? 1f : -1f; 
                velocity.x = backDir * 10f; 
            }

            if (Input.GetKeyDown(keyBlueCast) && myStats.TrySpendMeter(100f)) 
                PerformSpecial("1700_State", 1.5f);

            if (Input.GetKeyDown(keyRedCast) && myStats.TrySpendMeter(100f)) 
                PerformSpecial("1800_State", 1.5f);

            // Supers
            if (Input.GetKeyDown(keyPurpleHollow) && myStats.TrySpendMeter(50f)) 
                PerformSpecial("1900_State", 2.5f);

            if (Input.GetKeyDown(keyPurpleUnlimited) && myStats.TrySpendMeter(300f)) 
                PerformSpecial("3000_State", 3.0f);

            if (Input.GetKeyDown(keyDomainExpansion) && myStats.TrySpendMeter(300f)) 
            {
                PerformSpecial("3100_State", 4.0f);
                if(screenDimmer != null) screenDimmer.TriggerDim(3.5f);
            }

            // Combo Reset Logic using 'comboIndex'
            if (Time.time > lastComboInputTime + comboWindow && comboIndex > 0 && !isAttacking) {
                comboIndex = 0; activeChain = "";
            }
        }
        else
        {
            hDirection = 0; vDirection = 0; isJumping = false; isBlocking = false; isAttacking = false;
        }

        if (enemyTarget != null && !isAttacking)
        {
            if (transform.position.x > enemyTarget.position.x) mySpriteRenderer.flipX = true;  
            else mySpriteRenderer.flipX = false; 
        }

        HandleStates();
        UpdateAnimator();

        if (isGrounded) 
        { 
            if (currentState != (int)STATE.JUMPING) velocity.y = 0; 
        }
        else 
        { 
            velocity.y -= GRAVITY; 
        }
        
        myRigidBody.linearVelocity = new Vector2(velocity.x, myRigidBody.linearVelocity.y);
    }

    void HandleStates()
    {
        switch (currentState)
        {   
            case (int) STATE.IDLE:      IdleState(); break;
            case (int) STATE.WALKING:   WalkingState(); break;
            case (int) STATE.CROUCHING: CrouchingState(); break;
            case (int) STATE.DASHING:   velocity.x = (mySpriteRenderer.flipX ? -1 : 1) * dashSpeed; if(Mathf.Abs(hDirection) < 0.1f) SetState(STATE.IDLE); break;
            case (int) STATE.JUMPING:   JumpingState(); break;
            case (int) STATE.FALLING:   FallingState(); break;
            case (int) STATE.ATTACKING: AttackingState(); break;
            case (int) STATE.AIR_ATTACK: 
                velocity.x = 0; velocity.y -= (GRAVITY * 0.2f); 
                AttackingState(); 
                if(isGrounded) SetState(STATE.IDLE); 
                break;
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
        if (vDirection >= 0) 
        {
            isCrouching = false;
            SetState(STATE.IDLE);
        }
    }

    void BlockingState() { velocity.x = 0; if (!isBlocking) SetState(STATE.IDLE); }
    
    void AttackingState() 
    { 
        velocity.x = Mathf.MoveTowards(velocity.x, 0, Time.deltaTime * 5f); 
        attackTimer += Time.deltaTime; 
        // FIXED: uses attackDuration
        if (attackTimer > attackDuration) 
        { 
            attackTimer = 0; 
            attackLevel = 0; 
            isAttacking = false; 
            spawnedVFXTracker.Clear();
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

    void UpdateAnimator()
    {
        if (myAnimator == null || !myAnimator.gameObject.activeInHierarchy) return;
        if (isAttacking) return; 

        string animToPlay = "";

        switch (currentState)
        {
            case (int)STATE.IDLE:      animToPlay = "0_Basic"; break;
            case (int)STATE.CROUCHING: animToPlay = "11_Basic"; break;
            case (int)STATE.FALLING:   animToPlay = "41_Basic"; break;
            case (int)STATE.BLOCKING:  animToPlay = "0_Basic"; break;
            case (int)STATE.DEAD:      animToPlay = "5110_State"; break;
            case (int)STATE.DASHING:   animToPlay = "100_Basic"; break;
            
            case (int)STATE.WALKING:
                float facingDir = mySpriteRenderer.flipX ? -1f : 1f;
                bool movingForward = (hDirection * facingDir) > 0;
                animToPlay = movingForward ? "20_Basic" : "21_Basic"; 
                break;

            case (int)STATE.JUMPING:
                if (hDirection == 0) animToPlay = "41_Basic"; 
                else if (mySpriteRenderer.flipX) animToPlay = (hDirection < 0) ? "42_Basic" : "43_Basic";
                else animToPlay = (hDirection > 0) ? "42_Basic" : "43_Basic";
                break;
        }

        if (!string.IsNullOrEmpty(animToPlay))
        {
            int stateHash = Animator.StringToHash(animToPlay);
            if (myAnimator.HasState(0, stateHash))
            {
                if (myAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != stateHash)
                {
                    myAnimator.Play(stateHash, 0, 0f);
                }
            }
        }
    }

    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastTapTime < tapSpeed)
            {
                if(isGrounded && !isAttacking) SetState(STATE.DASHING);
            }
            lastTapTime = Time.time;
        }
    }

    void HandleComboInput(string inputType)
    {
        bool canStart = (currentState == (int)STATE.IDLE || currentState == (int)STATE.WALKING || currentState == (int)STATE.CROUCHING);
        bool canContinue = (currentState == (int)STATE.ATTACKING && isAttacking); 

        if (canStart || canContinue)
        {
            if (canStart) { activeChain = inputType; comboIndex = 0; }
            
            lastComboInputTime = Time.time;
            comboIndex++; // FIXED: uses comboIndex

            string animToPlay = "";
            float duration = 0.4f; 

            if (isCrouching) 
            { 
                animToPlay = "240_State"; 
                PerformAttack(animToPlay, crouchDamage, 0.5f, 1); 
                return; 
            }

            if (activeChain == "A")
            {
                switch (comboIndex)
                {
                    case 1: animToPlay = groundHit1; duration = 0.4f; break;
                    case 2: animToPlay = groundHit2; duration = 0.5f; break;
                    case 3: animToPlay = groundHit3; duration = 0.6f; break;
                    case 4: animToPlay = groundHit4; duration = 0.8f; break;
                    case 5: animToPlay = "231_State"; duration = 0.3f; PerformTeleport(); break;
                    case 6: animToPlay = groundFinisher; duration = 0.8f; break;
                    default: comboIndex = 1; animToPlay = groundHit1; duration = 0.4f; break;
                }
            }
            else if (activeChain == "B")
            {
                switch (comboIndex)
                {
                    case 1: animToPlay = "300_State"; duration = 0.4f; break;
                    case 2: animToPlay = "310_State"; duration = 0.5f; break;
                    case 3: animToPlay = "320_State"; duration = 0.6f; break;
                    case 4: animToPlay = "330_State"; duration = 0.6f; break;
                    case 5: animToPlay = "340_State"; duration = 0.6f; break;
                    case 6: animToPlay = "231_State"; duration = 0.3f; PerformTeleport(); break;
                    case 7: animToPlay = groundFinisher; duration = 0.8f; break;
                    default: comboIndex = 1; animToPlay = "300_State"; duration = 0.4f; break;
                }
            }
            PerformAttack(animToPlay, 30f, duration, comboIndex); 
        }
    }

    void PerformAttack(string stateName, float dmg, float duration, int level)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = level; 
        attackTimer = 0;     
        attackDuration = duration; // FIXED: uses attackDuration
        
        spawnedVFXTracker.Clear(); 
        PlayAnimSafe(stateName);
        
        SetState(STATE.ATTACKING);
    }
    
    void PerformSpecial(string stateName, float duration)
    {
        velocity.x = 0;
        isAttacking = true;
        attackLevel = 5; 
        attackTimer = 0;
        attackDuration = duration;
        
        spawnedVFXTracker.Clear();
        PlayAnimSafe(stateName);
        
        SetState(STATE.ATTACKING);
    }

    public void PerformTeleport() 
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
        string hurtAnim = isGrounded ? "5000_State" : "5030_State";
        
        PlayAnimSafe(hurtAnim);
        StopAttack();
        StartCoroutine(FlashRed());
    }

    void StopAttack() { isAttacking = false; attackTimer = 0; spawnedVFXTracker.Clear(); }
    void EnableDeadState() { SetState(STATE.DEAD); }
    
    void PlayAnimSafe(string anim) {
        if (string.IsNullOrEmpty(anim)) return;
        int hash = Animator.StringToHash(anim);
        if (myAnimator.HasState(0, hash) && myAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != hash)
            myAnimator.Play(hash, 0, 0f);
    }

    public void CastBlue() { SpawnProjectileAtLocation(blueProjectilePrefab, meter1Damage, firePoint.position); }
    public void CastPurple() { if (camShake != null) StartCoroutine(camShake.Shake(0.5f, 0.3f)); if (screenDimmer != null) screenDimmer.TriggerDim(0.5f); SpawnProjectileAtLocation(purpleProjectilePrefab, meter3Damage, firePoint.position); }
    public void CastRedAtEnemy() { if (redScreenFlash != null) redScreenFlash.TriggerFlash(); Vector3 spawnPosition; float facingDir = mySpriteRenderer.flipX ? -1f : 1f; if (enemyTarget != null) { spawnPosition = enemyTarget.position + new Vector3(redSpawnOffset * facingDir, 0, 0); spawnPosition.y = firePoint.position.y; } else { spawnPosition = firePoint.position; } SpawnProjectileAtLocation(redProjectilePrefab, meter2Damage, spawnPosition); }
    public void TeleportToEnemy() { if (enemyTarget != null) { float directionToEnemy = Mathf.Sign(enemyTarget.position.x - transform.position.x); Vector3 targetPos = enemyTarget.position; targetPos.x -= (directionToEnemy * redSpawnOffset); transform.position = targetPos; mySpriteRenderer.flipX = (directionToEnemy < 0); } }
    
    void SpawnProjectileAtLocation(GameObject prefab, float damageValue, Vector3 location) { if (prefab != null) { GameObject proj = Instantiate(prefab, location, Quaternion.identity); ProjectileController pc = proj.GetComponent<ProjectileController>(); if (pc != null) { pc.damage = damageValue; pc.ownerTag = gameObject.tag; } if (mySpriteRenderer.flipX) proj.transform.Rotate(0, 180, 0); } }
    System.Collections.IEnumerator FlashRed() { mySpriteRenderer.color = Color.red; yield return new WaitForSeconds(0.1f); mySpriteRenderer.color = Color.white; }
    void OnCollisionStay2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = true; }
    void OnCollisionExit2D(Collision2D collision) { if (collision.collider.CompareTag("Ground")) isGrounded = false; }
    void ResetInputs() { hDirection = 0; vDirection = 0; isJumping = false; isBlocking = false; }
    
    void OnGUI() { if (!isPlayer) return; GUIStyle style = new GUIStyle(); style.fontSize = 30; style.fontStyle = FontStyle.Bold; style.normal.textColor = Color.yellow; GUI.Label(new Rect(20, 20, 500, 40), "STATE: " + (STATE)currentState, style); string comboStatus = "Hit " + comboIndex + " (" + activeChain + ")"; GUI.Label(new Rect(20, 120, 800, 40), "COMBO CHAIN: " + comboStatus, style); if(myStats!=null) GUI.Label(new Rect(20, 160, 500, 40), "METER: " + myStats.CurrentHyper, style); }
}