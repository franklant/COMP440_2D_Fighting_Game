using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MadaraClone : MonoBehaviour
{
    // ==========================================
    //               DATA STRUCTURES
    // ==========================================
    [System.Serializable]
    public struct CloneMove
    {
        public string animationName;
        public float damage;
        public Vector2 knockback;
    }

    // ==========================================
    //               ATTRIBUTES
    // ==========================================
    [Header("Components")]
    public Animator animator;
    public Rigidbody2D rb;
    public MeleeDamage hitbox; 

    [Header("MUGEN Actions")]
    public string introClip = "Action_4000"; // Formation
    public string runClip = "Action_100";    // Run
    public string deathClip = "Action_4001"; // Vanish
    
    [Header("Attack Set")]
    public List<CloneMove> moveList;

    [Header("Settings")]
    public float runSpeed = 8f;
    // SET THIS TO 15 or 20 in Inspector for "Permanent" feel
    public float lifeTime = 15.0f; 
    public float spawnDelay = 0.5f; 
    public GameObject smokeVfxPrefab; 
    
    [Header("Behavior")]
    // Check this if you want them to die immediately after landing 1 punch
    public bool dieAfterAttack = true; 

    [Header("Tags")]
    public string wallTag = "Wall"; 
    public string enemyTag = "Enemy";

    private bool isActing = false; 
    private bool isAttacking = false;
    private bool isDead = false;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Default Moves if empty
        if (moveList == null || moveList.Count == 0)
        {
            moveList = new List<CloneMove>
            {
                new CloneMove { animationName = "Action_200", damage = 10, knockback = new Vector2(2, 0) },
                new CloneMove { animationName = "Action_210", damage = 15, knockback = new Vector2(3, 0) },
                new CloneMove { animationName = "Action_220", damage = 20, knockback = new Vector2(4, 2) },
                new CloneMove { animationName = "Action_300", damage = 15, knockback = new Vector2(2, 0) }
            };
        }

        // 1. Formation Phase
        rb.linearVelocity = Vector2.zero; 
        if (hitbox) hitbox.enabled = false;
        PlayAnimSafe(introClip);

        float formationTime = GetClipDuration(introClip);
        if (formationTime < 0.1f) formationTime = spawnDelay; 

        StartCoroutine(StartRunning(formationTime));
    }

    IEnumerator StartRunning(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isDead)
        {
            isActing = true;
            PlayAnimSafe(runClip);
            StartCoroutine(LifetimeRoutine());
        }
    }

    // This is just a safety cleanup so they don't exist for 3 hours
    IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!isDead) StartCoroutine(DieSequence());
    }

    void FixedUpdate()
    {
        if (isActing && !isAttacking && !isDead)
        {
            float direction = transform.localScale.x;
            rb.linearVelocity = new Vector2(direction * runSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    // ---------------- COLLISIONS ----------------

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead || !isActing || isAttacking) return;

        if (collision.CompareTag(enemyTag))
        {
            StartCoroutine(PerformRandomAttack());
        }
        else if (collision.CompareTag(wallTag))
        {
            StartCoroutine(DieSequence());
        }
    }

    // ---------------- COMBAT ----------------

    IEnumerator PerformRandomAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // Pick Random Move
        CloneMove selectedMove = moveList[Random.Range(0, moveList.Count)];
        
        if (hitbox != null)
        {
            hitbox.damage = selectedMove.damage;
            hitbox.knockback = selectedMove.knockback;
            hitbox.facingDirection = transform.localScale.x;
        }

        PlayAnimSafe(selectedMove.animationName);
        
        yield return new WaitForSeconds(0.1f);
        if (hitbox) hitbox.enabled = true;
        yield return new WaitForSeconds(0.2f);
        if (hitbox) hitbox.enabled = false;

        float attackDuration = GetClipDuration(selectedMove.animationName);
        yield return new WaitForSeconds(attackDuration);

        // MUGEN BEHAVIOR DECISION:
        // Wood Clones usually vanish after one solid hit.
        if (dieAfterAttack)
        {
            StartCoroutine(DieSequence());
        }
        else
        {
            // If unchecked, they will run again and try to hit another enemy
            isAttacking = false;
            PlayAnimSafe(runClip);
        }
    }

    IEnumerator DieSequence()
    {
        if (isDead) yield break;

        isDead = true;
        isActing = false;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        if (hitbox) hitbox.enabled = false;

        PlayAnimSafe(deathClip);

        if (smokeVfxPrefab != null)
            Instantiate(smokeVfxPrefab, transform.position, Quaternion.identity);

        float deathDuration = GetClipDuration(deathClip);
        yield return new WaitForSeconds(deathDuration);

        Destroy(gameObject);
    }

    void PlayAnimSafe(string name)
    {
        if (animator) animator.Play(name);
    }

    float GetClipDuration(string name)
    {
        if (!animator || animator.runtimeAnimatorController == null) return 0.5f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name) return clip.length;
        }
        return 0.5f; 
    }
}