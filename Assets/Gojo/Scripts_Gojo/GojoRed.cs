using UnityEngine;
using System.Collections;

public class GojoRed : MonoBehaviour
{
    [Header("Setup")]
    public Animator animator;      
    public GameObject redOrbPrefab; 

    [Header("Settings")]
    public float facingDirection = 1f; // 1 = Right, -1 = Left

    private bool isBusy = false;

    void Update()
    {
        // Press R to trigger Reversal Red
        if (!isBusy && Input.GetKeyDown(KeyCode.R)) 
        {
            StartCoroutine(Sequence_ReversalRed());
        }
    }

    // Logic mirroring MUGEN State 1600 -> 1610 -> 1620
    IEnumerator Sequence_ReversalRed()
    {
        isBusy = true;

        // 1. THE FLIP
        if (animator) animator.Play("Action_1600");

        // 2. THE TELEPORT 
        // Move Gojo BACKWARDS behind where he was
        yield return new WaitForSeconds(0.06f); 
        transform.position += new Vector3(-3.0f * facingDirection, 0, 0);

        // 3. GENERATE ORB 
        // Spawns the orb shortly after teleport
        yield return new WaitForSeconds(0.1f); 

        if (redOrbPrefab != null)
        {
            // Spawn slightly in front of Gojo's new position
            Vector3 spawnPos = transform.position + new Vector3(1.5f * facingDirection, 0.5f, 0);
            GameObject orb = Instantiate(redOrbPrefab, spawnPos, Quaternion.identity);

            // Pass control to the orb logic
            StartCoroutine(OrbBehavior(orb));
        }

        // 4. FINISH ANIMATION
        // Wait for Gojo to land/finish posing
        yield return new WaitForSeconds(1.0f);
        
        if (animator) animator.Play("Action_0"); // Return to Idle
        isBusy = false;
    }

    // Controls the Orb
    IEnumerator OrbBehavior(GameObject orb)
    {
        Rigidbody2D rb = orb.GetComponent<Rigidbody2D>();
        
        // PHASE A: CHARGE 
        // The orb stays still for approx 0.6s
        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        // Ensure it is small (Mimics MUGEN scale)
        orb.transform.localScale = new Vector3(0.2f, 0.2f, 1f); 

        yield return new WaitForSeconds(0.6f);

        // PHASE B: FIRE 
        // Velocity set to high speed
        if (orb != null && rb != null)
        {
            rb.linearVelocity = new Vector2(25f * facingDirection, 0);
        }

        // Cleanup after 2 seconds
        if (orb != null) Destroy(orb, 2.0f);
    }
}