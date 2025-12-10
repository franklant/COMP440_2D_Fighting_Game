using UnityEngine;

public class FighterMovement : MonoBehaviour
{
    [Header("Settings")]
    public bool isPlayer1 = true;   // ✔ Set this in the Inspector for each fighter

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float jumpForce = 8f;
    public float gravity = -25f;

    private Vector2 velocity;

    [Header("State")]
    public bool isGrounded = true;
    public bool isCrouching = false;
    public bool isBlocking = false;
    public bool facingRight = true;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    private Transform opponent;

    void Start()
    {
        opponent = GameObject.FindGameObjectWithTag(isPlayer1 ? "Player2" : "Player1").transform;
    }

    void Update()
    {
        HandleFacing();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleBlock();
    }

    void FixedUpdate()
    {
        // Apply gravity when in air
        if (!isGrounded)
            velocity.y += gravity * Time.fixedDeltaTime;

        rb.linearVelocity = velocity;
    }

    // ---------------------------
    // FACING
    // ---------------------------
    void HandleFacing()
    {
        if (opponent == null) return;

        bool shouldFaceRight = opponent.position.x > transform.position.x;

        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    // ---------------------------
    // MOVEMENT
    // ---------------------------
    void HandleMovement()
    {
        if (isBlocking || isCrouching)
        {
            velocity.x = 0;
            return;
        }

        float move = 0f;

        if (isPlayer1)
        {
            // Player 1 movement
            if (Input.GetKey(KeyCode.A)) move = -1f;
            if (Input.GetKey(KeyCode.D)) move = 1f;
        }
        else
        {
            // Player 2 movement
            if (Input.GetKey(KeyCode.LeftArrow)) move = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) move = 1f;
        }

        velocity.x = move * walkSpeed;
    }

    // ---------------------------
    // JUMP
    // ---------------------------
    void HandleJump()
    {
        if (isGrounded)
        {
            if (isPlayer1 && Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
            else if (!isPlayer1 && Input.GetKeyDown(KeyCode.UpArrow))
            {
                Jump();
            }
        }
    }

    void Jump()
    {
        velocity.y = jumpForce;
        isGrounded = false;
    }

    // ---------------------------
    // CROUCH
    // ---------------------------
    void HandleCrouch()
    {
        if (isPlayer1)
            isCrouching = Input.GetKey(KeyCode.S);
        else
            isCrouching = Input.GetKey(KeyCode.DownArrow);
    }

    // ---------------------------
    // BLOCK
    // ---------------------------
    void HandleBlock()
    {
        bool holdingBack =
            (facingRight && (isPlayer1 ? Input.GetKey(KeyCode.A) : Input.GetKey(KeyCode.LeftArrow))) ||
            (!facingRight && (isPlayer1 ? Input.GetKey(KeyCode.D) : Input.GetKey(KeyCode.RightArrow)));

        bool crouching = isPlayer1 ? Input.GetKey(KeyCode.S) : Input.GetKey(KeyCode.DownArrow);
        bool blockButton = isPlayer1 ? Input.GetKey(KeyCode.LeftShift) : Input.GetKey(KeyCode.RightShift);

        isBlocking = holdingBack && blockButton;
    }

    // ---------------------------
    // GROUND CHECK
    // ---------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            isGrounded = true;
            velocity.y = 0;
        }
    }


    void UpdateAnimations()
    {
        if (!animator) return;

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsBlocking", isCrouching);
        animator.SetFloat("WalkDirection", walkSpeed);
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }
}

