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

    private Animator animator;
    private SukunaMovement movementScript; // Reference to the movement script
    private bool isBusy = false; 

    void Awake()
    {
        animator = GetComponent<Animator>();
        movementScript = GetComponent<SukunaMovement>();
    }

    void Update()
    {
        // If we are casting, don't allow new inputs
        if (isBusy) return;

        HandleSummonInputs();
    }

    void HandleSummonInputs()
    {
        // 1. Divine Dogs (State 1400) - U
        if (Input.GetKeyDown(KeyCode.U)) 
        {
            Summon(divineDogsPrefab, new Vector2(1.5f, 0)); 
        }

        // 2. Nue (State 1200) - I
        if (Input.GetKeyDown(KeyCode.I)) 
        {
            Summon(nuePrefab, new Vector2(0.5f, 2.0f)); 
        }

        // 3. Max Elephant (State 1700) - O
        if (Input.GetKeyDown(KeyCode.O)) 
        {
            Summon(maxElephantPrefab, new Vector2(3.0f, 5.0f)); 
        }

        // 4. Piercing Ox (State 1000) - J
        if (Input.GetKeyDown(KeyCode.J)) 
        {
            Summon(bullPrefab, new Vector2(1.5f, 0)); 
        }

        // 5. Agito (State 1900) - K (Has Casting Animation)
        if (Input.GetKeyDown(KeyCode.K)) 
        {
            StartCoroutine(PerformSummon("Action_1500", agitoPrefab, new Vector2(2.0f, 0)));
        }

        // 6. Mahoraga (State 1500) - L (Has Casting Animation)
        if (Input.GetKeyDown(KeyCode.L)) 
        {
            StartCoroutine(PerformSummon("Action_2000", mahoragaPrefab, new Vector2(2.5f, 0)));
        }
    }

    IEnumerator PerformSummon(string animName, GameObject prefab, Vector2 offset)
    {
        isBusy = true; 
        
        // Tell the movement script to stop updating animations/physics
        if(movementScript != null) movementScript.isAttacking = true;
        
        // Stop any current movement physics immediately
        if(GetComponent<Rigidbody2D>()) GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

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

        // 3. Wait for rest of animation
        if(HasState(animName))
        {
            yield return new WaitForSeconds(0.5f); 
        }

        // 4. Return control to Movement Script
        if(movementScript != null) 
        {
            movementScript.isAttacking = false;
            // Let SukunaMovement handle the transition back to Idle (Action_0)
        }
        else
        {
            // Fallback if movement script is missing
            if(HasState("Action_0")) animator.Play("Action_0"); 
        }

        isBusy = false;
    }

    void Summon(GameObject prefab, Vector2 offset)
    {
        if (prefab == null) return;

        Vector3 spawnOrigin = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // Determine facing from Transform (since we removed isFacingRight variable)
        float facingDir = transform.localScale.x > 0 ? 1 : -1;
        
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
}