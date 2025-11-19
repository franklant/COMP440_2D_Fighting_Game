using System;
using Mono.Cecil.Cil;
using NUnit.Framework;
using TMPro;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;

public class CharacterScript : MonoBehaviour
{
    [Header("Access Components")]
    public SpriteRenderer mySpriteRenderer;
    public Rigidbody2D myRigidBody;
    public GameObject myGroundCheck;
    //public CharacterController myController;      // Character Controller does not work well for unity.

    [Header("Access Hitbox Components")]
    public GameObject midJabHitBox;

    [Header("Player Attributes")]
    public float GRAVITY = 0.098f;
    public Vector3 velocity = Vector3.zero;
    public float movementSpeed = 4f;
    public float maxSpeed = 8f;
    public float acceleration = 3f;
    public float jumpHeight = 7f;
    public int currentState;
    public float attackCoolDownDuration = 0.5f;
    public float attackLevel = 0;
    public float attackTimer = 0;
    private float hDirection = 0;
    private bool isAttackChain = false;

    [Header("Player Status")]
    public bool isGrounded;
    public bool isJumping;
    public bool isAttacking;
    
    /// <summary>
    ///     List of all the possible states of the character.
    /// </summary>
    private enum STATE {
        IDLE = 0,
        WALKING = 1,
        JUMPING = 2,
        FALLING = 3,
        ATTACKING = 4
    };

    /// <summary>
    ///     HELPER to remove repetitive type casting of enum.
    ///     Sets the current state with the enum state casted properly to integer.
    /// </summary>
    /// <param name="state">
    ///     The STATE attribute given as the argument.
    /// </param>
    void SetState(STATE state)
    {
        currentState = (int) state;
        /// Debug.Log("Setting State: " + state);
    }

    /// <summary>
    ///     Casts the enum value type of the state to proper integer type for comparison.
    /// </summary>
    /// <param name="state">
    ///     The STATE attribute given as the argument.
    /// </param>
    /// <returns>
    ///     The STATE value in the argument casted to an integer type.
    /// </returns>  
    int GetState(STATE state)
    {
        return (int) state;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myGroundCheck = GameObject.FindGameObjectWithTag("GroundCheck");        // not accessing a specific component

        // NOTE: Potentially use FindGameObjectsWithTag to get an array of all the hitbox objects.
        midJabHitBox = GameObject.FindGameObjectWithTag("MidJabHitBox");

        if (mySpriteRenderer == null)
            Debug.LogError("Could not access 'Sprite Renderer'!");
        if (myRigidBody == null)
            Debug.LogError("Could not access 'Character Controller'!");
        if (myGroundCheck == null)
            Debug.LogError("Could not access 'Ground Check'!");

        if (midJabHitBox == null)
            Debug.LogError("Could not access 'MidJabHitBox'!");


        SetState(STATE.IDLE);
        isGrounded = false;
    }

    // Update is called once per frame
    void Update()
    {
        hDirection = Input.GetAxisRaw("Horizontal");
        isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        isAttacking = Input.GetKeyDown(KeyCode.P);
        /// Debug.Log("Current Horizontal Direction: " + hDirection);

        if (!isGrounded)
        {
            SetState(STATE.FALLING);
            //velocity.y -= GRAVITY;
        } 

        // Keeps the hitbox in the correct position relative to the way in which the player is facing
        if (hDirection != 0)
        {
            // player +- 0.75 
            Vector3 hitBoxPosition = midJabHitBox.transform.position;
            hitBoxPosition.x = transform.position.x + (float) (hDirection * 0.75);
            midJabHitBox.transform.position = hitBoxPosition;
        }

        // Start the attack chain
        if (isAttacking)
        {
            isAttackChain = true;
        }

        // start attack chain
        if (isAttackChain)
        {
            if (attackTimer < attackCoolDownDuration)
            {
                attackTimer += Time.deltaTime;
            } else
            {
                attackTimer = 0;
                attackLevel = 0;
                isAttackChain = false;
            }
        }

        //HandleMovement();
        HandleStates();
        myRigidBody.linearVelocity = velocity;
        mySpriteRenderer.color = Color.aliceBlue;
    }

    /// <summary>
    ///     -NOT BEING USED-
    ///     A back-up method that acts as a prototype for how the character will move in the game.
    ///     This is before implemeneting them into state machines.
    /// </summary>
    void HandleMovement()
    {   
        // MOVEMENT
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime;

        // Clamp the value to prevent infinitely increasing vel
        float absVelocity = Math.Abs(velocity.x);
        velocity.x = Math.Clamp(absVelocity, 0, maxSpeed) * hDirection;

        // JUMPING
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // SetState(STATE.JUMPING);
            velocity.y = jumpHeight;
            isGrounded = false;
        }

        // FALLING OR GROUNDED
        // if (!isGrounded)
        // {
        //     SetState(STATE.FALLING);
        //     //velocity.y -= GRAVITY;
        // } else
        // {
        //     velocity.y = 0;
        // }
    }

    /// <summary>
    ///     Acts as the state manager of the character.
    ///     Given the current state of the character, handle it's respective character action.
    ///     Each state needs to be responsible for it's own respective action, as well as the transition from
    ///     it's state to another one. 
    /// </summary>
    void HandleStates()
    {
        switch (currentState)
        {   
            case (int) STATE.IDLE:
                IdleState();
                break;
            case (int) STATE.WALKING:
                WalkingState();
                break;
            case (int) STATE.JUMPING:
                JumpingState();
                break;
            case (int) STATE.FALLING:
                FallingState();
                break;
            case (int) STATE.ATTACKING:
                AttackingState();
                break;
            default:
                Debug.LogError("Current State '" + currentState + "' not recognized or implemented!");
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void IdleState()
    {
        // isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        // Whatever happens with idle state
        velocity.x = 0;

        if (hDirection != 0)
        {
            SetState(STATE.WALKING);
        }

        if (isJumping && isGrounded)
        {
            SetState(STATE.JUMPING);
            //isGrounded = false;
        }

        if (isAttacking)
        {
            // NOTE: We can be more specific when determining an aerial or grounded attack later on
            SetState(STATE.ATTACKING);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void WalkingState()
    {
        // isJumping = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        // myController.Move(new Vector3(hDirection, 0, 0) * movementSpeed * Time.deltaTime);
        velocity.x += hDirection * movementSpeed * acceleration * Time.deltaTime;

        // Clamp the value to prevent infinitely increasing vel
        float absVelocity = Math.Abs(velocity.x);
        velocity.x = Math.Clamp(absVelocity, 0, maxSpeed) * hDirection;

        if (hDirection == 0 && isGrounded)
        {
            SetState(STATE.IDLE);
        }

        if (isJumping && isGrounded)
        {
            SetState(STATE.JUMPING);
            //isGrounded = false;
        }

        if (isAttacking)
        {
            SetState(STATE.ATTACKING);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void JumpingState()
    {
        velocity.y = jumpHeight;

        SetState(STATE.FALLING);
        isGrounded = false;
    }

    /// <summary>
    /// 
    /// </summary>
    void FallingState()
    {
        velocity.y -= GRAVITY;

        if (isGrounded)
        {
            velocity.y = 0;
            if (hDirection != 0)
            {
                SetState(STATE.WALKING);
            } else
            {
                SetState(STATE.IDLE);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void AttackingState()
    {
        // logic
        Debug.Log("Just Attacked!");
        attackLevel += 1;
        velocity.x = 0;
        
        // transitions
        // if (!isAttackChain)
        // {   
        //     if (hDirection == 0)
        //     {
        //         if (isGrounded && !isAttacking)
        //             SetState(STATE.IDLE);
        //     } else
        //     {
        //         if (isGrounded && !isAttacking)
        //             SetState(STATE.WALKING);
        //     }
        // }
        if (hDirection == 0)
        {
            if (isGrounded && !isAttacking)
                SetState(STATE.IDLE);
        } else
        {
            if (isGrounded && !isAttacking)
                SetState(STATE.WALKING);
        }

        if (isJumping && isGrounded)
        {
            SetState(STATE.JUMPING);
            //isGrounded = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        /// TODO: Check the bottom of the player collider only
        Debug.Log("Colliding with " + collision.collider.name);

        if (collision.collider.CompareTag("Ground"))
        {
            if (collision.collider.OverlapPoint(myGroundCheck.transform.position))
            {
                Debug.Log("Standing on " + collision.collider.name);
                isGrounded = true;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("No longer colliding with " + collision.collider.name);

        if (collision.collider.CompareTag("Ground"))
        {
            if (!collision.collider.OverlapPoint(myGroundCheck.transform.position))
            {
                Debug.Log("No longer standing on " + collision.collider.name);
                isGrounded = false;
            }
        }
    }
}
