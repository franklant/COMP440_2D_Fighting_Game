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
        // 1. Divine Dogs (State 1400)
        if (Input.GetKeyDown(KeyCode.U)) 
        {
            // Immediate spawn for now (or add casting anim if needed)
            Summon(divineDogsPrefab, new Vector2(1.5f, 0)); 
        }

        // 2. Nue (State 1200)
        if (Input.GetKeyDown(KeyCode.I)) 
        {
            Summon(nuePrefab, new Vector2(0.5f, 2.0f)); 
        }

        // 3. Max Elephant (State 1700)
        if (Input.GetKeyDown(KeyCode.O)) 
        {
            Summon(maxElephantPrefab, new Vector2(3.0f, 5.0f)); 
        }

        // 4. Piercing Ox (State 1000)
        if (Input.GetKeyDown(KeyCode.J)) 
        {
            Summon(bullPrefab, new Vector2(1.5f, 0)); 
        }

        // 5. Agito (State 1900) - CASTING ANIMATION
        if (Input.GetKeyDown(KeyCode.K)) 
        {
            StartCoroutine(PerformSummon("Action_1500", agitoPrefab, new Vector2(2.0f, 0)));
        }

        // 6. Mahoraga (State 1500) - CASTING ANIMATION
        if (Input.GetKeyDown(KeyCode.L)) 
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
            
            // Wait for the animation to reach the "Summoning Point"
            // MUGEN usually spawns helpers around frame 3 or 5 of the cast
            // Adjust this delay (0.5f) to match when Sukuna's hands clap/move
            yield return new WaitForSeconds(0.5f); 
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
            // Wait remaining time or fixed amount
            yield return new WaitForSeconds(0.5f); 
        }

        // 4. Return to Idle
        animator.Play("Idle"); // Make sure you have an "Idle" state!
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