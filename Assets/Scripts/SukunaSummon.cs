using UnityEngine;
using System.Collections;

public class SukunaSummon : MonoBehaviour
{
    [Header("MUGEN Settings")]
    [Tooltip("The State ID from Specials.txt (e.g., 1501 for Mahoraga, 1901 for Agito, 1010 for Bull, 1200 for Nue, 1700 for Elephant)")]
    public int startStateID;
    public float patrolSpeed = 6f;
    public float attackRange = 2.5f;

    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform target; // The enemy (P2)

    [Header("Effects")]
    [Tooltip("Assign the Nue_Lightning Prefab here")]
    public GameObject lightningPrefab; 

    private bool isFacingRight = true;
    private Rigidbody2D rb;
    private bool canFlip = true; // New flag to control turning

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p2 = GameObject.FindWithTag("Player2");
            if (p2) target = p2.transform;
        }

        // Initial Facing Check
        if (target != null && Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            float dist = target.position.x - transform.position.x;
            if (dist < 0)
            {
                isFacingRight = false;
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        StartCoroutine(MugenStateLogic(startStateID));
    }

    void Update()
    {
        // Only flip if allowed (not attacking) and not moving too fast (like bull charge)
        if (canFlip && target != null && Mathf.Abs(rb.linearVelocity.x) < 1f && rb.gravityScale == 0)
        {
            float dist = target.position.x - transform.position.x;
            // Threshold of 0.5f prevents jittering when standing right on top of target
            if (dist > 0.5f && !isFacingRight) Flip();
            else if (dist < -0.5f && isFacingRight) Flip();
        }
    }

    IEnumerator MugenStateLogic(int stateID)
    {
        switch (stateID)
        {
            // =========================================================
            // PIERCING OX (BULL)
            // =========================================================
            case 1010:
                canFlip = false; // Bull charges straight, no turning mid-charge
                PlayAnimation("Action_1010");
                yield return new WaitForSeconds(GetAnimLength("Action_1010"));

                PlayAnimation("Action_1060");

                // Calculate direction once before charge
                float dirBull = 1f;
                if (target != null) dirBull = (target.position.x > transform.position.x) ? 1f : -1f;
                else dirBull = isFacingRight ? 1f : -1f;

                // Ensure sprite faces charge direction
                if ((dirBull > 0 && !isFacingRight) || (dirBull < 0 && isFacingRight)) Flip();

                rb.linearVelocity = new Vector2(dirBull * 15f, 0);
                yield return new WaitForSeconds(2.0f);
                Destroy(gameObject);
                break;

            // =========================================================
            // NUE (1200)
            // =========================================================
            case 1200:
                rb.gravityScale = 0f;
                canFlip = false; // Nue flies straight in this version

                PlayAnimation("Action_1200");
                yield return new WaitForSeconds(GetAnimLength("Action_1200"));

                PlayAnimation("Action_1230");
                
                float nueTimer = 0f;
                float flyDirection = isFacingRight ? 1f : -1f;
                
                while(nueTimer < 2.0f)
                {
                    rb.linearVelocity = new Vector2(flyDirection * 5f, 0); 
                    if (nueTimer % 0.2f < Time.deltaTime) SpawnLightning();
                    nueTimer += Time.deltaTime;
                    yield return null;
                }
                Destroy(gameObject);
                break;

            // =========================================================
            // MAX ELEPHANT (1700)
            // =========================================================
            case 1700: 
                canFlip = false; // Falling objects don't turn
                PlayAnimation("Action_1701"); 
                rb.gravityScale = 4.0f;
                
                while (transform.position.y > 0.5f) yield return null;
                
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                transform.position = new Vector3(transform.position.x, 0, 0);
                
                if(HasState("Action_1702")) PlayAnimation("Action_1702");
                else PlayAnimation("Action_1700");
                
                Debug.Log("Elephant Crash!");
                yield return new WaitForSeconds(1.0f);
                Destroy(gameObject);
                break;

            // =========================================================
            // MAHORAGA (1500+) - Smart Targeting
            // =========================================================
            case 1501: // Intro
                canFlip = true; // Can turn during intro if needed
                PlayAnimation("Action_2001");
                yield return new WaitForSeconds(GetAnimLength("Action_2001"));
                StartCoroutine(MugenStateLogic(1510));
                break;

            case 1510: // Idle
                canFlip = true; // ALLOW turning while idle to face enemy
                PlayAnimation("Action_2002");
                yield return new WaitForSeconds(0.5f);

                if (target == null) yield break;
                float distM = Mathf.Abs(target.position.x - transform.position.x);

                if (distM > attackRange) StartCoroutine(MugenStateLogic(1520));
                else
                {
                    int rng = Random.Range(0, 4);
                    if (rng == 0) StartCoroutine(MugenStateLogic(1540));
                    else if (rng == 1) StartCoroutine(MugenStateLogic(1541));
                    else if (rng == 2) StartCoroutine(MugenStateLogic(1542));
                    else StartCoroutine(MugenStateLogic(1543));
                }
                break;

            case 1520: // Move
                canFlip = true; // ALLOW turning while moving
                PlayAnimation("Action_2003");
                float walkTimer = 0f;
                while (walkTimer < 1.5f && Mathf.Abs(target.position.x - transform.position.x) > attackRange)
                {
                    float dir = (target.position.x > transform.position.x) ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);
                    walkTimer += Time.deltaTime;
                    yield return null;
                }
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(MugenStateLogic(1510));
                break;

            // Attacks (Disable flipping so they commit to the attack direction)
            case 1540: yield return PerformAttack("Action_2005"); StartCoroutine(MugenStateLogic(1510)); break;
            case 1541: yield return PerformAttack("Action_2105"); StartCoroutine(MugenStateLogic(1510)); break;
            case 1542: yield return PerformAttack("Action_2205"); StartCoroutine(MugenStateLogic(1510)); break;
            case 1543: yield return PerformAttack("Action_2305"); StartCoroutine(MugenStateLogic(1510)); break;


            // =========================================================
            // AGITO (1900+) - Smart Targeting
            // =========================================================
            case 1901: // Intro
                canFlip = true;
                PlayAnimation("Action_1501");
                yield return new WaitForSeconds(GetAnimLength("Action_1501"));
                StartCoroutine(MugenStateLogic(1910));
                break;

            case 1910: // Idle
                canFlip = true; // Turn to face enemy
                PlayAnimation("Action_1510");
                yield return new WaitForSeconds(0.3f);

                if (target == null) yield break;
                float distA = Mathf.Abs(target.position.x - transform.position.x);

                if (distA > attackRange) StartCoroutine(MugenStateLogic(1920));
                else
                {
                    int rng = Random.Range(0, 4);
                    if (rng == 0) StartCoroutine(MugenStateLogic(1940));
                    else if (rng == 1) StartCoroutine(MugenStateLogic(1941));
                    else if (rng == 2) StartCoroutine(MugenStateLogic(1942));
                    else StartCoroutine(MugenStateLogic(1943));
                }
                break;

            case 1920: // Move
                canFlip = true;
                PlayAnimation("Action_1520");
                float agitoTimer = 0f;
                while (agitoTimer < 1.0f && Mathf.Abs(target.position.x - transform.position.x) > attackRange)
                {
                    float dir = (target.position.x > transform.position.x) ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);
                    agitoTimer += Time.deltaTime;
                    yield return null;
                }
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(MugenStateLogic(1910));
                break;

            // Attacks (Commit to direction)
            case 1940: yield return PerformAttack("Action_1540"); StartCoroutine(MugenStateLogic(1910)); break;
            case 1941: yield return PerformAttack("Action_1541"); StartCoroutine(MugenStateLogic(1910)); break;
            case 1942: yield return PerformAttack("Action_1542"); StartCoroutine(MugenStateLogic(1910)); break;
            case 1943: yield return PerformAttack("Action_1543"); StartCoroutine(MugenStateLogic(1910)); break;

            default:
                Debug.LogWarning($"State {stateID} not implemented yet!");
                break;
        }
    }

    IEnumerator PerformAttack(string animName)
    {
        canFlip = false; // LOCK rotation during attack
        PlayAnimation(animName);
        rb.linearVelocity = Vector2.zero;
        
        yield return new WaitForSeconds(0.2f);
        Debug.Log($"{animName} Hit Frame!"); 
        
        float duration = GetAnimLength(animName);
        float waitTime = duration > 0.2f ? duration - 0.2f : 0.1f;
        yield return new WaitForSeconds(waitTime);
        
        canFlip = true; // Unlock rotation after attack
    }

    void PlayAnimation(string animName)
    {
        if (animator.runtimeAnimatorController == null) return;

        if (HasState(animName)) animator.Play(animName);
        else if (HasState(animName.Replace("Action_", ""))) animator.Play(animName.Replace("Action_", ""));
        else Debug.LogWarning($"Animation '{animName}' missing on {gameObject.name}.");
    }

    bool HasState(string stateName)
    {
        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    float GetAnimLength(string clipName)
    {
        if (animator.runtimeAnimatorController == null) return 1f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.Contains(clipName)) return clip.length;
        }
        return 1f;
    }

    void SpawnLightning(float scaleMultiplier = 1.0f)
    {
        if (lightningPrefab == null) return;
        Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
        Vector3 spawnPos = transform.position + offset;
        GameObject bolt = Instantiate(lightningPrefab, spawnPos, Quaternion.identity);
        Vector3 scale = bolt.transform.localScale;
        scale.x = (isFacingRight ? 1 : -1) * Mathf.Abs(scale.x) * scaleMultiplier;
        scale.y *= scaleMultiplier;
        bolt.transform.localScale = scale;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}