using UnityEngine;
using System.Collections;

public class SukunaCombatController : MonoBehaviour
{
    [Header("Hitbox Setup")]
    public GameObject meleeHitbox; // DRAG SUKUNA'S HITBOX HERE
    
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 8.0f;
    public float jumpForce = 14.0f;
    public bool facingRight = true;

    [Header("Summon Prefabs")]
    public GameObject mahoragaPrefab; 
    public GameObject agitoPrefab;    
    public GameObject bullPrefab;     
    public GameObject nuePrefab;      
    public GameObject divineDogsPrefab; 
    public GameObject maxElephantPrefab;
    public Transform spawnPoint; 

    [Header("Input Keys")]
    public KeyCode keyLightAttack = KeyCode.H;   
    public KeyCode keyMediumAttack = KeyCode.N;  
    public KeyCode keyHeavyAttack = KeyCode.M;   

    // IDs
    private const int ID_IDLE = 0;
    private const int ID_ATTACK_L = 200;
    private const int ID_ATTACK_M = 260;
    private const int ID_ATTACK_H = 11400;

    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded = true;
    private bool isAttacking; 
    private int currentActionID = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isAttacking) return;

        // --- ATTACKS ---
        if (Input.GetKeyDown(keyLightAttack)) 
            StartCoroutine(PerformAttack(ID_ATTACK_L, 0.4f, 20f));
        
        else if (Input.GetKeyDown(keyMediumAttack)) 
            StartCoroutine(PerformAttack(ID_ATTACK_M, 0.5f, 30f));
        
        else if (Input.GetKeyDown(keyHeavyAttack)) 
            StartCoroutine(PerformAttack(ID_ATTACK_H, 0.8f, 50f));
    }

    IEnumerator PerformAttack(int actionID, float duration, float damage)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        
        PlayAction(actionID);
        
        // NEW: Activate Hitbox during the attack
        ActivateHitbox(damage);

        yield return new WaitForSeconds(duration);
        
        isAttacking = false;
        PlayAction(ID_IDLE); 
    }

    // --- HITBOX HELPER ---
    void ActivateHitbox(float dmg)
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.SetActive(true);
            Hitbox hb = meleeHitbox.GetComponent<Hitbox>();
            if (hb != null)
            {
                hb.damage = dmg;
                hb.stun = 15f; // Default stun
                hb.isAerial = false;
            }
            StartCoroutine(DisableHitboxDelay(0.2f));
        }
    }

    IEnumerator DisableHitboxDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (meleeHitbox) meleeHitbox.SetActive(false);
    }

    void PlayAction(int id)
    {
        currentActionID = id;
        animator.Play($"Action_{id}");
    }
}