using UnityEngine;
using System.Collections;

[RequireComponent(typeof(FighterStatsManager))]
public class GojoMoveset : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;
    public Rigidbody2D rb;
    public AudioSource audioSource;
    private FighterStatsManager stats; // Reference to stats

    [Header("Move Prefabs")]
    public GameObject redProjectilePrefab;
    public GameObject blueOrbPrefab;
    public GameObject purpleBlastPrefab;

    [Header("Meter Costs (Max = 300)")]
    public float blueCost = 100f;   // 1 Bar (33%)
    public float redCost = 200f;    // 2 Bars (66%)
    public float purpleCost = 300f; // Full Meter (100%)

    [Header("Settings")]
    public float facingDirection = 1f;
    private bool isBusy = false;

    void Awake()
    {
        stats = GetComponent<FighterStatsManager>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isBusy) return;

        // --- INPUTS WITH METER CHECKS ---

        // 1. Reversal Red (Cost: 200)
        if (Input.GetKeyDown(KeyCode.R))
        {
            // CHANGED: "TryConsumeHyper" -> "TrySpendMeter"
            if (stats.TrySpendMeter(redCost)) 
                StartCoroutine(PerformRed_Back());
            else
                Debug.Log("Not enough meter for Red! Need " + redCost);
        }

        // 2. Lapse Blue (Cost: 100)
        if (Input.GetKeyDown(KeyCode.B))
        {
            // CHANGED: "TryConsumeHyper" -> "TrySpendMeter"
            if (stats.TrySpendMeter(blueCost)) 
                StartCoroutine(PerformBlue_Spinning());
            else
                 Debug.Log("Not enough meter for Blue! Need " + blueCost);
        }

        // 3. Hollow Purple (Cost: 300 / Full)
        if (Input.GetKeyDown(KeyCode.P))
        {
            // CHANGED: "TryConsumeHyper" -> "TrySpendMeter"
            if (stats.TrySpendMeter(purpleCost)) 
                StartCoroutine(PerformHollowPurple());
            else
                 Debug.Log("Not enough meter for Purple! Need " + purpleCost);
        }

        // 4. Domain Expansion (Optional: Make this free or cost 3 bars too?)
        if (Input.GetKeyDown(KeyCode.D)) 
        {
            StartCoroutine(PerformDomainExpansion());
        }
    }

    // --- MOVES (Logic remains the same) ---

    IEnumerator PerformRed_Back()
    {
        isBusy = true;
        if (animator) animator.Play("Action_1600");
        
        yield return new WaitForSeconds(0.06f); 
        transform.position += new Vector3(-2.0f * facingDirection, 0, 0); 

        yield return new WaitForSeconds(0.1f); 
        if (redProjectilePrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(1f * facingDirection, 0, 0);
            GameObject red = Instantiate(redProjectilePrefab, spawnPos, Quaternion.identity);
            StartCoroutine(HandleRedProjectileLogic(red));
        }

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }

    IEnumerator HandleRedProjectileLogic(GameObject red)
    {
        Rigidbody2D redRb = red.GetComponent<Rigidbody2D>();
        if (redRb == null) redRb = red.AddComponent<Rigidbody2D>();
        
        redRb.gravityScale = 0;
        redRb.linearVelocity = Vector2.zero;
        
        yield return new WaitForSeconds(0.58f); 

        redRb.linearVelocity = new Vector2(20f * facingDirection, 0); 
        Destroy(red, 2.0f);
    }

    IEnumerator PerformBlue_Spinning()
    {
        isBusy = true;
        if (animator) animator.Play("Action_1300");

        yield return new WaitForSeconds(0.1f);
        
        if (blueOrbPrefab != null)
        {
            GameObject blue = Instantiate(blueOrbPrefab, transform.position, Quaternion.identity);
            BlueOrbBehavior behavior = blue.AddComponent<BlueOrbBehavior>();
            behavior.owner = this.transform;
        }

        yield return new WaitForSeconds(1.0f);
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }

    IEnumerator PerformHollowPurple()
    {
        isBusy = true;
        if (animator) animator.Play("Action_1900");

        yield return new WaitForSeconds(0.6f);

        if (purpleBlastPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(2f * facingDirection, 0, 0);
            GameObject purple = Instantiate(purpleBlastPrefab, spawnPos, Quaternion.identity);
            
            Rigidbody2D pRb = purple.GetComponent<Rigidbody2D>();
            if (pRb == null) pRb = purple.AddComponent<Rigidbody2D>();
            
            pRb.gravityScale = 0;
            pRb.linearVelocity = new Vector2(10f * facingDirection, 0); 
            Destroy(purple, 5.0f);
        }

        yield return new WaitForSeconds(1.0f);
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }

    IEnumerator PerformDomainExpansion()
    {
        isBusy = true;
        if (animator) animator.Play("Action_3200");
        yield return new WaitForSeconds(1.5f); 
        yield return new WaitForSeconds(1.0f);
        isBusy = false;
        if (animator) animator.Play("Action_0");
    }
}

// Helper for Blue Orb
public class BlueOrbBehavior : MonoBehaviour
{
    public Transform owner;
    private float startTime;

    void Start()
    {
        startTime = Time.time;
        Destroy(gameObject, 3.0f); 
    }

    void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }
        
        float time = (Time.time - startTime) * 10; 
        float xOffset = 3.0f * Mathf.Sin(time * Mathf.PI / 10f); 
        float yOffset = 1.0f * Mathf.Sin((time - 5f) * Mathf.PI / 10f); 

        transform.position = owner.position + new Vector3(xOffset, yOffset + 1.0f, 0);
    }
}