using UnityEngine;

public class GojoMovement : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;
    
    [Header("MUGEN Stats")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 8.5f;
    public float jumpForce = 8.0f;
    public float gravityScale = 1.5f;

    [Header("State Flags")]
    public bool isGrounded;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private float moveInput;
    private bool isCrouching;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        // 1. Ground Check
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        }

        // 2. Input Processing
        moveInput = Input.GetAxisRaw("Horizontal");
        bool jumpInput = Input.GetButtonDown("Jump");
        bool crouchInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // 3. Actions
        if (isGrounded)
        {
            if (jumpInput && !isCrouching)
            {
                PerformJump();
            }
            
            if (crouchInput)
            {
                isCrouching = true;
                moveInput = 0; 
            }
            else
            {
                isCrouching = false;
            }
        }

        // 4. Update Animator
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (!isCrouching)
        {
            // Walk vs Run
            float speed = (Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
        }
        
        // Flip Character
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        // Play Jump Start Animation
        PlayAnimSafe("Action_40"); 
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 1. AIR ANIMATION (Priority 1)
        if (!isGrounded)
        {
            // Play "Jump Air" loop if available, otherwise "Jump Start"
            if (HasState("Action_50"))
            {
                animator.Play("Action_50");
            }
            else
            {
                animator.Play("Action_40");
            }
        }
        // 2. CROUCH ANIMATION (Priority 2)
        else if (isCrouching)
        {
            PlayAnimSafe("Action_11"); 
        }
        // 3. RUN/WALK ANIMATION (Priority 3)
        else if (Mathf.Abs(moveInput) > 0.1f)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                PlayAnimSafe("Action_60"); // Run
            else
                PlayAnimSafe("Action_20"); // Walk
        }
        // 4. IDLE (Default)
        else
        {
            PlayAnimSafe("Action_0"); 
        }
    }

    // --- HELPER FUNCTIONS (These were missing!) ---

    void PlayAnimSafe(string animName)
    {
        if (HasState(animName))
        {
            animator.Play(animName);
        }
        // Uncomment this line if you want to see warnings for missing animations
        // else { Debug.LogWarning($"Animation State '{animName}' not found!"); }
    }

    bool HasState(string name)
    {
        return animator.HasState(0, Animator.StringToHash(name));
    }

    // Visual Debug for Ground Check
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(groundCheck.position, 0.5f);
        }
    }
}