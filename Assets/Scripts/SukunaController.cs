using UnityEngine;
using System.Collections;

public class SukunaController : MonoBehaviour
{
    [Header("Summon Prefabs")]
    public GameObject mahoragaPrefab; 
    public GameObject agitoPrefab;    
    public GameObject bullPrefab;     
    public GameObject nuePrefab;      
    public GameObject divineDogsPrefab; 
    public GameObject maxElephantPrefab;

    [Header("Settings")]
    public Transform spawnPoint; 
    public bool isFacingRight = true;

    private Animator animator;
    private bool isBusy = false; // Prevents spamming summons

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Prevent moving/attacking while casting
        if (isBusy) return;

        // --- MOVEMENT & FACING ---
        float horizontal = Input.GetAxis("Horizontal");
        if (horizontal > 0 && !isFacingRight) Flip();
        else if (horizontal < 0 && isFacingRight) Flip();

        // --- SUMMON INPUTS ---
        HandleSummonInputs();
    }

    void HandleSummonInputs()
    {
        // 1. Divine Dogs (State 1400) - Key: U (Unchanged)
        if (Input.GetKeyDown(KeyCode.U)) 
        {
            Summon(divineDogsPrefab, new Vector2(1.5f, 0)); 
        }

        // 2. Nue (State 1200) - Key: Y (Remapped from I to make room for Elephant)
        // Feel free to change this if you prefer a different key for Nue
        if (Input.GetKeyDown(KeyCode.Y)) 
        {
            Summon(nuePrefab, new Vector2(0.5f, 2.0f)); 
        }

        // 3. Max Elephant (State 1700) - Key: I (Remapped)
        if (Input.GetKeyDown(KeyCode.I)) 
        {
            Summon(maxElephantPrefab, new Vector2(3.0f, 5.0f)); 
        }

        // 4. Piercing Ox (State 1000) - Key: Q (Unchanged)
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            Summon(bullPrefab, new Vector2(1.5f, 0)); 
        }

        // 5. Agito (State 1900) - Key: O (Remapped)
        if (Input.GetKeyDown(KeyCode.O)) 
        {
            StartCoroutine(PerformSummon("Action_1500", agitoPrefab, new Vector2(2.0f, 0)));
        }

        // 6. Mahoraga (State 1500) - Key: P (Remapped)
        if (Input.GetKeyDown(KeyCode.P)) 
        {
            StartCoroutine(PerformSummon("Action_2000", mahoragaPrefab, new Vector2(2.5f, 0)));
        }
    }

    IEnumerator PerformSummon(string animName, GameObject prefab, Vector2 offset)
    {
        isBusy = true; // Lock input

        // 1. Play Casting Animation
        if(HasState(animName))
        {
            animator.Play(animName);
            yield return new WaitForSeconds(0.5f); // Wait for hand sign
        }
        else
        {
            Debug.LogWarning($"Sukuna missing animation: {animName}. Spawning immediately.");
        }

        // 2. Spawn the Summon
        Summon(prefab, offset);

        // 3. Wait for rest of animation to finish
        if(HasState(animName))
        {
            yield return new WaitForSeconds(0.5f); 
        }

        // 4. Return to Idle
        // Using "Action_0" which is the standard Idle name in your other scripts
        // If your Idle state is named "Idle", change this string back to "Idle"
        if(HasState("Action_0")) animator.Play("Action_0"); 
        else animator.Play("Idle");

        isBusy = false;
    }

    void Summon(GameObject prefab, Vector2 offset)
    {
        if (prefab == null) return;

        Vector3 spawnOrigin = spawnPoint != null ? spawnPoint.position : transform.position;
        float facingDir = isFacingRight ? 1 : -1;
        Vector3 finalPosition = spawnOrigin + new Vector3(offset.x * facingDir, offset.y, 0);

        GameObject summon = Instantiate(prefab, finalPosition, Quaternion.identity);

        Vector3 scale = summon.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDir;
        summon.transform.localScale = scale;
    }

    bool HasState(string stateName)
    {
        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}