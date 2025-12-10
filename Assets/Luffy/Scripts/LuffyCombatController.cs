using UnityEngine;

public class LuffyCombatController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public bool facingRight = true;

    [Header("Input Keys")]
    public KeyCode keyComboA = KeyCode.J; 
    public KeyCode keyComboB = KeyCode.K; 
    public KeyCode keySpecial1 = KeyCode.I; 
    public KeyCode keySpecial2 = KeyCode.O; 
    public KeyCode keyUltimate = KeyCode.P; // NEW: Ultimate Key
    public KeyCode keyTransform = KeyCode.Return; 
    
    public KeyCode keyJump = KeyCode.Space;
    public KeyCode keyCrouch = KeyCode.S; 

    // --- CURRENT ACTIVE STATE ---
    [Header("Active Action IDs (Do not edit)")]
    public int ID_IDLE;
    public int ID_RUN_FWD;
    public int ID_RUN_BACK;
    public int ID_JUMP_START; 
    public int ID_JUMP;       
    public int ID_LAND;
    public int ID_CROUCH_IN;
    public int ID_CROUCH_LOOP;
    public int ID_CROUCH_OUT; 
    public int ID_GUARD;
    
    public int[] currentComboA;
    public int[] currentComboB;

    // --- STATE DEFINITIONS ---
    // 1. BASE FORM
    private int BASE_IDLE = 0;
    private int BASE_RUN_FWD = 20;
    private int BASE_RUN_BACK = 21;
    private int BASE_JUMP = 41;
    private int BASE_LAND = 47;
    private int BASE_CROUCH_IN = 10;
    private int BASE_CROUCH_LOOP = 11;
    private int BASE_GUARD = 120;
    private int[] BASE_COMBO_A = { 200, 205, 210 };
    private int[] BASE_COMBO_B = { 305, 300, 320 };
    
    // Base Specials
    public int ID_SPECIAL_I = 1003; // Red Hawk
    public int ID_SPECIAL_O_1 = 1301; // Grab Run
    public int ID_SPECIAL_O_2 = 1302; // Grab Hit
    public int ID_ULTIMATE_1 = -1; // Base has no P ultimate defined yet
    public int ID_ULTIMATE_2 = -1;

    // 2. GEAR 5 FORM
    private int G5_TRANSFORM = 900;
    private int G5_IDLE = 2000;
    private int G5_CROUCH_IN = 2010;
    private int G5_CROUCH_LOOP = 2011;
    private int G5_CROUCH_OUT = 2012;
    private int G5_RUN_FWD = 2020;
    private int G5_RUN_BACK = 2021;
    private int G5_JUMP_START = 2040;
    private int G5_JUMP = 2041;
    private int G5_LAND = 2050;
    private int G5_GUARD = 2120;
    private int[] G5_COMBO_A = { 2200, 2205, 2220 };
    private int[] G5_COMBO_B = { 2300, 2310, 2320 };

    // Gear 5 Specials
    private int G5_SPECIAL_I = 2400;   // Python
    private int G5_SPECIAL_O_1 = 41000; // Kong Gun (Start)
    private int G5_SPECIAL_O_2 = 43001; // Leo Bazooka (End)
    private int G5_ULTIMATE_1 = 3300;   // King Kong Gun (Charge)
    private int G5_ULTIMATE_2 = 3350;   // King Kong Gun (Hit)

    // Internal State
    private Animator animator;
    private Rigidbody2D rb;
    private bool isGrounded = true;
    private bool isAttacking = false;
    private bool isLanding = false;
    private bool isTransforming = false;
    private bool isGear5 = false; 
    private bool isGrabbing = false;

    private int comboIndex = 0; 
    private int currentActionID = -1;
    private float lastAttackTime = 0f;
    private float comboTimeout = 1.0f; 

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        SetBaseStats();
        PlayAction(ID_IDLE);
    }

    void Update()
    {
        // 1. TRANSFORMATION
        if (isTransforming)
        {
            rb.linearVelocity = Vector2.zero;
            if (IsAnimationFinished())
            {
                isTransforming = false;
                SetGear5Stats(); 
                PlayAction(ID_IDLE); 
            }
            return;
        }

        // 2. LANDING
        if (isLanding)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetKeyDown(keyJump) || 
                Input.GetKeyDown(keyComboA) || Input.GetKeyDown(keyComboB))
            {
                isLanding = false;
            }
            else 
            {
                rb.linearVelocity = Vector2.zero;
                if (GetAnimationProgress() > 0.2f) 
                {
                    isLanding = false;
                    PlayAction(ID_IDLE);
                }
                return; 
            }
        }

        if (Time.time - lastAttackTime > comboTimeout && !isAttacking) comboIndex = 0;

        // 3. ATTACK / SPECIAL SEQUENCES
        if (isAttacking)
        {
            if (isGrounded) rb.linearVelocity = Vector2.zero; 

            // --- CHAIN LOGIC FOR SPECIALS ---
            
            // Base Form: Grab Logic (1301 -> 1302)
            if (!isGear5 && isGrabbing)
            {
                HandleBaseGrab();
                return;
            }

            // Gear 5: O Key Chain (41000 -> 43001)
            if (isGear5 && currentActionID == ID_SPECIAL_O_1)
            {
                if (IsAnimationFinished()) PlayAction(ID_SPECIAL_O_2); // Auto-chain
                return;
            }
            if (isGear5 && currentActionID == ID_SPECIAL_O_2)
            {
                if (IsAnimationFinished()) EndAttack();
                return;
            }

            // Gear 5: Ultimate Chain (3300 -> 3350)
            if (isGear5 && currentActionID == ID_ULTIMATE_1)
            {
                if (IsAnimationFinished()) PlayAction(ID_ULTIMATE_2); // Auto-chain
                return;
            }
            if (isGear5 && currentActionID == ID_ULTIMATE_2)
            {
                if (IsAnimationFinished()) EndAttack();
                return;
            }

            // Crouch Out Logic
            if (currentActionID == ID_CROUCH_OUT && IsAnimationFinished())
            {
                EndAttack();
                return;
            }

            // Jump Start Logic
            if (currentActionID == ID_JUMP_START && IsAnimationFinished())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                isAttacking = false;
                isGrounded = false;
                PlayAction(ID_JUMP);
                return;
            }

            // Standard Combos
            CheckComboInput();
            if (IsAnimationFinished()) EndAttack();
            return;
        }

        // 4. INPUTS
        if (Input.GetKeyDown(keyTransform) && !isGear5)
        {
            isTransforming = true;
            PlayAction(G5_TRANSFORM);
        }
        else if (Input.GetKeyDown(keyComboA)) ExecuteCombo(currentComboA);
        else if (Input.GetKeyDown(keyComboB)) ExecuteCombo(currentComboB);
        else if (Input.GetKeyDown(keySpecial1)) PerformSpecial(ID_SPECIAL_I);
        else if (Input.GetKeyDown(keySpecial2)) PerformSpecial(ID_SPECIAL_O_1); // Starts chain
        else if (Input.GetKeyDown(keyUltimate)) PerformSpecial(ID_ULTIMATE_1);  // Starts chain
        
        else HandleMovement();
    }

    // --- LOGIC HELPERS ---

    void HandleBaseGrab()
    {
        float dir = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        
        if (currentActionID == ID_SPECIAL_O_1 && IsAnimationFinished()) EndAttack(); // Missed
        else if (currentActionID == ID_SPECIAL_O_2 && IsAnimationFinished()) EndAttack(); // Hit finished
    }

    void EndAttack()
    {
        isAttacking = false;
        isGrabbing = false;
        if (Input.GetKey(keyCrouch)) PlayAction(ID_CROUCH_LOOP);
        else PlayAction(ID_IDLE);
    }

    void SetBaseStats()
    {
        isGear5 = false;
        ID_IDLE = BASE_IDLE;
        ID_RUN_FWD = BASE_RUN_FWD;
        ID_RUN_BACK = BASE_RUN_BACK;
        ID_JUMP_START = -1; 
        ID_JUMP = BASE_JUMP;
        ID_LAND = BASE_LAND;
        ID_CROUCH_IN = BASE_CROUCH_IN;
        ID_CROUCH_LOOP = BASE_CROUCH_LOOP;
        ID_CROUCH_OUT = -1; 
        ID_GUARD = BASE_GUARD;
        currentComboA = BASE_COMBO_A;
        currentComboB = BASE_COMBO_B;
        
        // Base Special Mapping
        ID_SPECIAL_I = 1003; 
        ID_SPECIAL_O_1 = 1301; 
        ID_SPECIAL_O_2 = 1302;
        ID_ULTIMATE_1 = -1; // No ultimate for base
    }

    void SetGear5Stats()
    {
        isGear5 = true;
        ID_IDLE = G5_IDLE;
        ID_RUN_FWD = G5_RUN_FWD;
        ID_RUN_BACK = G5_RUN_BACK;
        ID_JUMP_START = G5_JUMP_START;
        ID_JUMP = G5_JUMP;
        ID_LAND = G5_LAND;
        ID_CROUCH_IN = G5_CROUCH_IN;
        ID_CROUCH_LOOP = G5_CROUCH_LOOP;
        ID_CROUCH_OUT = G5_CROUCH_OUT;
        ID_GUARD = G5_GUARD;
        currentComboA = G5_COMBO_A;
        currentComboB = G5_COMBO_B;

        // Gear 5 Special Mapping
        ID_SPECIAL_I = G5_SPECIAL_I;     // 2400
        ID_SPECIAL_O_1 = G5_SPECIAL_O_1; // 41000 (Starts O chain)
        ID_SPECIAL_O_2 = G5_SPECIAL_O_2; // 43001 (Ends O chain)
        ID_ULTIMATE_1 = G5_ULTIMATE_1;   // 3300 (Starts P chain)
        ID_ULTIMATE_2 = G5_ULTIMATE_2;   // 3350 (Ends P chain)
    }

    void PerformSpecial(int id)
    {
        if (id == -1) return; // Ignore if no move assigned
        
        isAttacking = true;
        // Base Form Grab Flag Logic
        if (!isGear5 && id == ID_SPECIAL_O_1) isGrabbing = true;
        
        PlayAction(id);
    }

    void ExecuteCombo(int[] chain)
    {
        if (comboIndex >= chain.Length) comboIndex = 0;
        PlayAction(chain[comboIndex]);
        isAttacking = true;
        lastAttackTime = Time.time;
        comboIndex++;
    }

    void CheckComboInput()
    {
        if (isGrabbing) return; 
        if (Input.GetKeyDown(keyComboA))
        {
            if (comboIndex < currentComboA.Length && GetAnimationProgress() > 0.4f)
                ExecuteCombo(currentComboA);
        }
        else if (Input.GetKeyDown(keyComboB))
        {
            if (comboIndex < currentComboB.Length && GetAnimationProgress() > 0.4f)
                ExecuteCombo(currentComboB);
        }
    }

    void HandleMovement()
    {
        if (isGrounded && Input.GetKeyDown(keyJump))
        {
            if (ID_JUMP_START != -1)
            {
                isAttacking = true; 
                PlayAction(ID_JUMP_START);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                PlayAction(ID_JUMP);
                isGrounded = false;
            }
            return;
        }

        if (isGrounded && Input.GetKey(keyCrouch))
        {
            rb.linearVelocity = Vector2.zero;
            if (currentActionID != ID_CROUCH_IN && currentActionID != ID_CROUCH_LOOP) PlayAction(ID_CROUCH_IN);
            else if (currentActionID == ID_CROUCH_IN && IsAnimationFinished()) PlayAction(ID_CROUCH_LOOP);
            return;
        }
        else if (isGrounded && !Input.GetKey(keyCrouch) && currentActionID == ID_CROUCH_LOOP)
        {
            if (ID_CROUCH_OUT != -1)
            {
                isAttacking = true;
                PlayAction(ID_CROUCH_OUT);
                return;
            }
        }

        float x = Input.GetAxisRaw("Horizontal");
        if (isGrounded)
        {
            if (x != 0)
            {
                rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);
                bool movingFwd = (x > 0 && facingRight) || (x < 0 && !facingRight);
                if (movingFwd) PlayAction(ID_RUN_FWD);
                else PlayAction(ID_RUN_BACK);
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                PlayAction(ID_IDLE);
            }
        }
    }

    void PlayAction(int id)
    {
        if (currentActionID == id && !isAttacking) return;
        currentActionID = id;
        animator.Play($"Action_{id}");
    }

    bool IsAnimationFinished()
    {
        return GetAnimationProgress() >= 1.0f;
    }

    float GetAnimationProgress()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName($"Action_{currentActionID}")) return info.normalizedTime;
        return 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            isAttacking = false; 
            isGrabbing = false;
            comboIndex = 0; 
            isLanding = true;
            rb.linearVelocity = Vector2.zero; 
            PlayAction(ID_LAND);
        }
        // Base form grab collision
        if (!isGear5 && isGrabbing && currentActionID == ID_SPECIAL_O_1 && collision.gameObject.CompareTag("Enemy"))
        {
            rb.linearVelocity = Vector2.zero;
            PlayAction(ID_SPECIAL_O_2);
        }
    }
}