using UnityEngine;
using System.Collections;

public class SukunaMovement : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;
    
    [Header("MUGEN Stats")]
    public float walkSpeed = 3.0f; 
    public float runSpeed = 8.0f;
    public float jumpForce = 14.0f;
    public float gravityScale = 2.0f;

    [Header("State Flags")]
    public bool isGrounded;
    public bool isAttacking; // Prevents movement while attacking
    public Transform groundCheck;
    public LayerMask groundLayer;

    private float moveInput;
    private bool isCrouching;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        // 1. Ground Check
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        }

        // Stop input processing if we are locked in an attack animation
        if (isAttacking) return;

        // 2. Input Processing
        moveInput = Input.GetAxisRaw("Horizontal");
        bool jumpInput = Input.GetButtonDown("Jump");
        bool crouchInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // 3. Attack Inputs (J, N, M)
        if (isGrounded)
        {
            // Light Attack (Action 200)
            if (Input.GetKeyDown(KeyCode.J)) 
            {
                StartCoroutine(PerformAttack("Action_200", 0.4f)); 
            }
            // Medium Attack (Action 260)
            else if (Input.GetKeyDown(KeyCode.K)) 
            {
                StartCoroutine(PerformAttack("Action_260", 0.5f)); 
            }
            // Heavy Attack (Action 11400)
            else if (Input.GetKeyDown(KeyCode.M)) 
            {
                StartCoroutine(PerformAttack("Action_11400", 0.8f)); 
            }
        }

        // 4. Movement Logic
        if (isGrounded && !isAttacking)
        {
            if (jumpInput && !isCrouching)
            {
                PerformJump();
            }
            
            if (crouchInput)
            {
                isCrouching = true;
                moveInput = 0; // Stop moving while crouching
            }
            else
            {
                isCrouching = false;
            }
        }

        // 5. Update Animator (Only if not attacking to avoid overriding the attack anim)
        if (!isAttacking) UpdateAnimations();
    }

    void FixedUpdate()
    {
        // Don't move physics if attacking or crouching
        if (!isCrouching && !isAttacking)
        {
            // Walk vs Run (Shift to Run logic)
            float speed = (Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
        }
        else if (isAttacking || isCrouching)
        {
            // Stop sliding
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        // Flip Character (Only if not attacking to prevent spinning mid-punch)
        if (!isAttacking)
        {
            if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
            else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        // Sukuna Jump Start is Action 40
        PlayAnimSafe("Action_40"); 
    }

    IEnumerator PerformAttack(string animName, float duration)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero; // Halt movement
        
        PlayAnimSafe(animName);
        
        // Wait for animation to finish (approximate time or fetch real length if needed)
        yield return new WaitForSeconds(duration);
        
        isAttacking = false;
        // The UpdateAnimations() in Update() will take over from here and play Idle/Walk
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 1. AIR ANIMATION
        if (!isGrounded)
        {
            // Try Jump Loop (50) -> Jump Start (40)
            if (HasState("Action_50")) animator.Play("Action_50");
            else animator.Play("Action_40");
        }
        // 2. CROUCH ANIMATION
        else if (isCrouching)
        {
            PlayAnimSafe("Action_11"); // Crouch Loop
        }
        // 3. RUN/WALK ANIMATION
        else if (Mathf.Abs(moveInput) > 0.1f)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // Sukuna Run Forward is 60
                PlayAnimSafe("Action_60"); 
            }
            else
            {
                PlayAnimSafe("Action_20"); // Walk
            }
        }
        // 4. IDLE
        else
        {
            PlayAnimSafe("Action_0"); 
        }
    }

    // --- HELPER FUNCTIONS ---

    void PlayAnimSafe(string animName)
    {
        if (HasState(animName))
        {
            animator.Play(animName);
        }
        // Optional: Log warning if animation is missing
        // else Debug.LogWarning("Missing animation: " + animName);
    }

    bool HasState(string name)
    {
        return animator.HasState(0, Animator.StringToHash(name));
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(groundCheck.position, 0.2f);
        }
    }
}