using UnityEngine;
using System.Collections;

public class SukunaCombatController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 8.0f;
    public float jumpForce = 14.0f;
    public float gravityScale = 2.0f;
    public bool facingRight = true;

    [Header("Summon Prefabs")]
    public GameObject mahoragaPrefab; 
    public GameObject agitoPrefab;    
    public GameObject bullPrefab;     
    public GameObject nuePrefab;      
    public GameObject divineDogsPrefab; 
    public GameObject maxElephantPrefab;

    [Header("Input Keys - Combat")]
    public KeyCode keyLightAttack = KeyCode.H;   // Action 200
    public KeyCode keyMediumAttack = KeyCode.N;  // Action 260
    public KeyCode keyHeavyAttack = KeyCode.M;   // Action 11400

    [Header("Input Keys - Summons")]
    public KeyCode keySummonDogs = KeyCode.U;
    public KeyCode keySummonNue = KeyCode.I;     // Remap here if needed
    public KeyCode keySummonElephant = KeyCode.O;
    public KeyCode keySummonBull = KeyCode.J;
    public KeyCode keySummonAgito = KeyCode.K;
    public KeyCode keySummonMahoraga = KeyCode.L;

    [Header("Components")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Transform spawnPoint; // Optional spawn point for summons

    // --- MUGEN ACTION IDS ---
    private const int ID_IDLE = 0;
    private const int ID_CROUCH = 11;
    private const int ID_WALK = 20;
    private const int ID_JUMP_START = 40;
    private const int ID_JUMP_LOOP = 50;
    private const int ID_RUN = 60;
    
    // Attacks
    private const int ID_ATTACK_L = 200;
    private const int ID_ATTACK_M = 260;
    private const int ID_ATTACK_H = 11400;

    // Summon Cast Anims
    private const int ID_CAST_AGITO = 1500;
    private const int ID_CAST_MAHORAGA = 2000;

    // Internal State
    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private bool isAttacking; // Locks movement during attacks/summons
    private bool isCrouching;
    private float moveInput;
    
    // Debug Info
    private int currentActionID = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        // 1. Physics Checks
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        }

        // 2. Stop input processing if locked (attacking/casting)
        if (isAttacking) 
        {
            // Ensure physics doesn't slide us while attacking
            if(isGrounded) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 3. Process Inputs
        moveInput = Input.GetAxisRaw("Horizontal");
        HandleCombatInputs();
        HandleSummonInputs();
        HandleMovementInputs();

        // 4. Update Animation State
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isAttacking) return;

        // Apply Movement Velocity
        if (!isCrouching)
        {
            float speed = (Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Handle Facing Direction
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
    }

    // --- INPUT HANDLERS ---

    void HandleCombatInputs()
    {
        if (!isGrounded) return;

        if (Input.GetKeyDown(keyLightAttack)) 
            StartCoroutine(PerformAttack(ID_ATTACK_L, 0.4f));
        
        else if (Input.GetKeyDown(keyMediumAttack)) 
            StartCoroutine(PerformAttack(ID_ATTACK_M, 0.5f));
        
        else if (Input.GetKeyDown(keyHeavyAttack)) 
            StartCoroutine(PerformAttack(ID_ATTACK_H, 0.8f));
    }

    void HandleSummonInputs()
    {
        // Instant Summons
        if (Input.GetKeyDown(keySummonDogs)) SpawnSummon(divineDogsPrefab, new Vector2(1.5f, 0));
        if (Input.GetKeyDown(keySummonNue)) SpawnSummon(nuePrefab, new Vector2(0.5f, 2.0f));
        if (Input.GetKeyDown(keySummonElephant)) SpawnSummon(maxElephantPrefab, new Vector2(3.0f, 5.0f));
        if (Input.GetKeyDown(keySummonBull)) SpawnSummon(bullPrefab, Vector2.zero);

        // Casting Summons (Requires Animation)
        if (Input.GetKeyDown(keySummonAgito)) 
            StartCoroutine(PerformSummonCast(ID_CAST_AGITO, agitoPrefab, new Vector2(2.0f, 0)));
        
        if (Input.GetKeyDown(keySummonMahoraga)) 
            StartCoroutine(PerformSummonCast(ID_CAST_MAHORAGA, mahoragaPrefab, new Vector2(2.5f, 0)));
    }

    void HandleMovementInputs()
    {
        // Jumping
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            PlayAction(ID_JUMP_START);
        }

        // Crouching
        if (isGrounded && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)))
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
    }

    // --- COROUTINES (ACTIONS) ---

    IEnumerator PerformAttack(int actionID, float duration)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        
        PlayAction(actionID);
        
        yield return new WaitForSeconds(duration);
        
        isAttacking = false;
        PlayAction(ID_IDLE); // Force return to idle
    }

    IEnumerator PerformSummonCast(int actionID, GameObject prefab, Vector2 offset)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // 1. Play Cast Anim
        PlayAction(actionID);
        
        // 2. Wait for "Summon Point" (Hand clap/sign)
        yield return new WaitForSeconds(0.5f); 

        // 3. Spawn
        SpawnSummon(prefab, offset);

        // 4. Wait for rest of anim
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
        PlayAction(ID_IDLE);
    }

    // --- HELPERS ---

    void SpawnSummon(GameObject prefab, Vector2 offset)
    {
        if (prefab == null) return;

        Vector3 spawnOrigin = spawnPoint != null ? spawnPoint.position : transform.position;
        float dir = facingRight ? 1f : -1f;
        Vector3 finalPos = spawnOrigin + new Vector3(offset.x * dir, offset.y, 0);

        GameObject summon = Instantiate(prefab, finalPos, Quaternion.identity);
        
        // Flip summon to match Sukuna
        Vector3 scale = summon.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        summon.transform.localScale = scale;
    }

    void UpdateAnimations()
    {
        if (isAttacking) return; // Don't override attacks

        if (!isGrounded)
        {
            // Check for Jump Loop, fallback to Jump Start
            if (HasState($"Action_{ID_JUMP_LOOP}")) PlayAction(ID_JUMP_LOOP);
            else PlayAction(ID_JUMP_START);
        }
        else if (isCrouching)
        {
            PlayAction(ID_CROUCH);
        }
        else if (Mathf.Abs(moveInput) > 0.1f)
        {
            // Run if shift held, else Walk
            if (Input.GetKey(KeyCode.LeftShift)) PlayAction(ID_RUN);
            else PlayAction(ID_WALK);
        }
        else
        {
            PlayAction(ID_IDLE);
        }
    }

    void PlayAction(int id)
    {
        if (currentActionID == id && !isAttacking) return; // Don't spam same state unless attacking

        currentActionID = id;
        string animName = $"Action_{id}";
        
        // Safety check to prevent crashing if animation is missing
        if (HasState(animName))
        {
            animator.Play(animName);
        }
        else
        {
            // If main state missing, fallback to Idle to prevent T-Pose
            if (id != ID_IDLE && HasState($"Action_{ID_IDLE}")) 
                animator.Play($"Action_{ID_IDLE}");
        }
    }

    bool HasState(string stateName)
    {
        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(groundCheck.position, 0.2f);
        }
    }

    // --- VISUAL DEBUGGER ---
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        
        // Background box
        GUI.Box(new Rect(10, 10, 300, 120), "Sukuna Debug");

        GUI.Label(new Rect(20, 40, 280, 30), $"Grounded: {isGrounded}", style);
        GUI.Label(new Rect(20, 65, 280, 30), $"Target Action: Action_{currentActionID}", style);
        GUI.Label(new Rect(20, 90, 280, 30), $"Moving Input: {moveInput}", style);
    }
}