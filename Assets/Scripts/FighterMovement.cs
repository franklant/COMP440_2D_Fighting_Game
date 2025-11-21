using UnityEngine;

public class FighterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float jumpForce = 8f;

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
        opponent = GameObject.FindGameObjectWithTag("opponent").GetComponent<Transform>();
    }
    private void Update()
    {
        HandleFacing();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleBlock();
        UpdateAnimations();
    }

    void HandleFacing()
    {
        if (opponent == null) return;

        bool shouldFaceRight = opponent.position.x > transform.position.x;

        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    void HandleMovement()
    {
        if (isBlocking || isCrouching) return; // No walking while blocking/crouching

        float move = 0f;

        if (Input.GetKey(KeyCode.A)) move = -1f;
        if (Input.GetKey(KeyCode.D)) move = 1f;

        rb.linearVelocity = new Vector2(move * walkSpeed, rb.linearVelocity.y);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKey(KeyCode.S))
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
    }

    void HandleBlock()
    {
        // Example: Hold BACK to block
        // If facing right, back = A
        // If facing left, back = D

        bool holdingBack =
            (facingRight && Input.GetKey(KeyCode.A)) ||
            (!facingRight && Input.GetKey(KeyCode.D));

        if (holdingBack && !Input.GetKey(KeyCode.S)) // standing block
        {
            isBlocking = true;
        }
        else if (holdingBack && Input.GetKey(KeyCode.S)) // crouch block
        {
            isBlocking = true;
        }
        else
        {
            isBlocking = false;
        }
    }

    void UpdateAnimations()
    {
        if (!animator) return;

        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Crouch", isCrouching);
        animator.SetBool("Block", isBlocking);
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }
}

