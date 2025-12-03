using UnityEngine;
using System.Collections;

public class GojoMoveset : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;
    public Rigidbody2D rb;
    public AudioSource audioSource;

    [Header("Move Prefabs")]
    public GameObject redProjectilePrefab;   // Assign your Red Prefab here
    public GameObject blueOrbPrefab;         // Assign your Blue Prefab here
    public GameObject purpleBlastPrefab;     // Assign your Purple Prefab here

    [Header("Settings")]
    public float facingDirection = 1f; // 1 for right, -1 for left

    private bool isBusy = false;

    void Update()
    {
        if (isBusy) return;

        // Input Mapping (Change these keys to whatever you prefer)
        if (Input.GetKeyDown(KeyCode.R)) StartCoroutine(PerformRed_Back()); // "R" for Red
        if (Input.GetKeyDown(KeyCode.B)) StartCoroutine(PerformBlue_Spinning()); // "B" for Blue
        if (Input.GetKeyDown(KeyCode.P)) StartCoroutine(PerformHollowPurple()); // "P" for Purple
        if (Input.GetKeyDown(KeyCode.D)) StartCoroutine(PerformDomainExpansion()); // "D" for Domain
    }

    // --- 1. REVERSAL RED (Back) ---
    // Logic based on MUGEN State 1600 -> 1610 -> 1620
    IEnumerator PerformRed_Back()
    {
        isBusy = true;
        
        // Play Animation
        if (animator) animator.Play("Action_1600");
        
        // Teleport Backwards (The "Back" in Back Red)
        yield return new WaitForSeconds(0.06f); 
        transform.position += new Vector3(-2.0f * facingDirection, 0, 0); 

        // Spawn The Red Orb
        yield return new WaitForSeconds(0.1f); 
        if (redProjectilePrefab != null)
        {
            // Spawn it slightly in front
            Vector3 spawnPos = transform.position + new Vector3(1f * facingDirection, 0, 0);
            GameObject red = Instantiate(redProjectilePrefab, spawnPos, Quaternion.identity);
            
            // Start the launch logic for the projectile
            StartCoroutine(HandleRedProjectileLogic(red));
        }

        yield return new WaitForSeconds(0.5f); // Recovery time
        isBusy = false;
        if (animator) animator.Play("Action_0"); // Return to Idle
    }

    // Logic to hold the red ball in place, then launch it
    IEnumerator HandleRedProjectileLogic(GameObject red)
    {
        Rigidbody2D redRb = red.GetComponent<Rigidbody2D>();
        if (redRb == null) redRb = red.AddComponent<Rigidbody2D>();
        
        redRb.gravityScale = 0;
        redRb.linearVelocity = Vector2.zero;
        
        // Wait/Charge for approx 0.5s (35 ticks)
        yield return new WaitForSeconds(0.58f); 

        // Launch!
        redRb.linearVelocity = new Vector2(20f * facingDirection, 0); 
        
        // Destroy after 2 seconds to clean up
        Destroy(red, 2.0f);
    }

    // --- 2. LAPSE BLUE (Spinning) ---
    // Logic based on MUGEN State 1300 -> 1360
    IEnumerator PerformBlue_Spinning()
    {
        isBusy = true;
        if (animator) animator.Play("Action_1300");

        // Wait a split second then spawn
        yield return new WaitForSeconds(0.1f);
        
        if (blueOrbPrefab != null)
        {
            GameObject blue = Instantiate(blueOrbPrefab, transform.position, Quaternion.identity);
            
            // Add the orbiting behavior script dynamically
            BlueOrbBehavior behavior = blue.AddComponent<BlueOrbBehavior>();
            behavior.owner = this.transform;
        }

        yield return new WaitForSeconds(1.0f);
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }

    // --- 3. HOLLOW PURPLE ---
    // Logic based on MUGEN State 1900
    IEnumerator PerformHollowPurple()
    {
        isBusy = true;
        if (animator) animator.Play("Action_1900");

        // Wait for charge up animation
        yield return new WaitForSeconds(0.6f);

        // Spawn The Blast
        if (purpleBlastPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(2f * facingDirection, 0, 0);
            GameObject purple = Instantiate(purpleBlastPrefab, spawnPos, Quaternion.identity);
            
            Rigidbody2D pRb = purple.GetComponent<Rigidbody2D>();
            if (pRb == null) pRb = purple.AddComponent<Rigidbody2D>();
            
            pRb.gravityScale = 0;
            pRb.linearVelocity = new Vector2(10f * facingDirection, 0); 
            
            // Clean up purple after 5 seconds
            Destroy(purple, 5.0f);
        }

        yield return new WaitForSeconds(1.0f);
        
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }

    // --- 4. DOMAIN EXPANSION: UNLIMITED VOID ---
    // Logic based on MUGEN State 3200
    IEnumerator PerformDomainExpansion()
    {
        isBusy = true;
        if (animator) animator.Play("Action_3200");

        Debug.Log("Domain Expansion: Infinite Void Activated");

        // Cinematic wait time
        yield return new WaitForSeconds(1.5f); 

        // Here you would instantiate your domain background or apply global stun
        // For now, we just wait for the animation to finish
        
        yield return new WaitForSeconds(1.0f);
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }
}

// --- HELPER CLASS FOR BLUE ORB ---
// This handles the orbiting movement of Blue
public class BlueOrbBehavior : MonoBehaviour
{
    public Transform owner;
    private float startTime;

    void Start()
    {
        startTime = Time.time;
        // Automatically destroy the orb after 3 seconds
        Destroy(gameObject, 3.0f); 
    }

    void Update()
    {
        if (owner == null) 
        {
            Destroy(gameObject);
            return;
        }

        // Logic adapted from MUGEN [Statedef 1360]
        // Calculates an orbital position using Sine waves
        
        float time = (Time.time - startTime) * 10; // Speed multiplier
        float xOffset = 3.0f * Mathf.Sin(time * Mathf.PI / 10f); // X Orbit
        float yOffset = 1.0f * Mathf.Sin((time - 5f) * Mathf.PI / 10f); // Y Orbit

        // Update position relative to Gojo (owner)
        transform.position = owner.position + new Vector3(xOffset, yOffset + 1.0f, 0);
        
        // Pull Logic (Optional Placeholder)
        // You can add code here to find nearby objects and suck them in
    }
}