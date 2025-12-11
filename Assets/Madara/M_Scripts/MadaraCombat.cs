using UnityEngine;
using System.Collections;

public class MadaraCombat : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;
    public MadaraMovement movementScript;
    public BoxCollider2D bodyCollider; 
    public Hitbox hitboxScript;

    [Header("Combat Inputs")]
    public KeyCode buttonA = KeyCode.J; 
    public KeyCode buttonB = KeyCode.K; 
    public float comboWindow = 1.0f;

    [Header("Jutsu Inputs")]
    public KeyCode bunshinKey = KeyCode.I;   
    public KeyCode chargeKey = KeyCode.C;    
    public KeyCode transformKey = KeyCode.Return; 

    [Header("Clone Settings")]
    public GameObject shadowClonePrefab; 
    public GameObject smokeVFX;          

    [Header("Perfect Susanoo")]
    public GameObject susanooAuraObject; 
    public float susanooDuration = 15.0f;    
    public Vector2 normalSize = new Vector2(1f, 2f);      
    public Vector2 susanooSize = new Vector2(4f, 6f);     

    // Internal
    public bool isSusanooActive = false;
    private bool isAttacking = false;
    private int comboStepA = 0;
    private int comboStepB = 0;
    private float lastAttackTime = 0;

    void Start()
    {
        if (hitboxScript == null) hitboxScript = GetComponentInChildren<Hitbox>();
        if (hitboxScript == null) hitboxScript = GetComponentInChildren<Hitbox>(true);

        if (movementScript == null) movementScript = GetComponent<MadaraMovement>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider2D>();
        
        if (bodyCollider) normalSize = bodyCollider.size;
        if (susanooAuraObject) susanooAuraObject.SetActive(false);
    }

    void Update()
    {
        // 1. Susanoo Walk Animation Logic
        if (isSusanooActive && !isAttacking && movementScript != null)
        {
            if (Mathf.Abs(movementScript.rb.linearVelocity.x) > 0.1f)
            {
                PlayAnimationSafe("Action_39020"); // Walk
            }
            else
            {
                PlayAnimationSafe("Action_39000"); // Idle
            }
        }

        // 2. Block Inputs if Attacking
        if (isAttacking) return;

        // Combo Reset
        if (Time.time - lastAttackTime > comboWindow)
        {
            comboStepA = 0;
            comboStepB = 0;
        }

        // 3. Inputs
        if (Input.GetKeyDown(transformKey) && !isSusanooActive) 
        {
            StartCoroutine(ActivateSusanoo());
        }
        
        if (Input.GetKeyDown(bunshinKey)) StartCoroutine(SummonClone());

        if (Input.GetKeyDown(buttonA) || Input.GetKeyDown(buttonB))
        {
            if (isSusanooActive) PerformSusanooSlash();
            else 
            {
                if (Input.GetKeyDown(buttonA)) PerformComboA();
                else PerformComboB();
            }
        }
    }

    // --- HITBOX LOGIC ---
    void SetHitStats(float damage, float stun, bool aerial)
    {
        if (hitboxScript != null)
        {
            hitboxScript.damage = damage;
            hitboxScript.stun = stun;
            hitboxScript.isAerial = aerial;
            hitboxScript.gameObject.SetActive(true);
            StartCoroutine(TurnOffHitbox(0.2f));
        }
    }
    
    IEnumerator TurnOffHitbox(float delay)
    {
        yield return new WaitForSeconds(delay);
        if(hitboxScript) hitboxScript.gameObject.SetActive(false);
    }

    // --- SUSANOO LOGIC (UPDATED) ---

    IEnumerator ActivateSusanoo()
    {
        isSusanooActive = true;
        isAttacking = true;
        
        // Stop Movement
        if (movementScript) movementScript.enabled = false;
        if (movementScript) movementScript.rb.linearVelocity = Vector2.zero;
        
        // Play Transform Animation
        string transformAnim = "Action_39001";
        PlayAnimationSafe(transformAnim);
        
        if (bodyCollider) bodyCollider.size = susanooSize;

        // WAIT FOR ANIMATION (Luffy Style Logic)
        // We wait for the animator to pick up the state, then wait for completion.
        yield return new WaitForSeconds(0.1f); 
        yield return new WaitUntil(() => IsAnimationComplete(transformAnim));
        
        isAttacking = false;
        if (movementScript) movementScript.enabled = true; 
        
        // Duration Timer
        yield return new WaitForSeconds(susanooDuration);
        
        // Deactivate
        isSusanooActive = false;
        if (bodyCollider) bodyCollider.size = normalSize;
        PlayAnimationSafe("Action_0"); 
    }

    void PerformSusanooSlash()
    {
        if(isAttacking) return;
        SetHitStats(80, 40f, true);
        StartCoroutine(PerformAttackRoutine("Action_39300", 0.8f));
    }

    // --- NORMAL COMBOS ---
    void PerformComboA()
    {
        comboStepB = 0; lastAttackTime = Time.time;
        string clipName = "Action_200";
        if(comboStepA == 1) clipName = "Action_210";
        if(comboStepA == 2) clipName = "Action_220";
        
        SetHitStats(15, 10f, false);
        comboStepA++;
        if(comboStepA > 2) comboStepA = 0;
        
        StartCoroutine(PerformAttackRoutine(clipName, 0.4f));
    }

    void PerformComboB()
    {
        comboStepA = 0; lastAttackTime = Time.time;
        StartCoroutine(PerformAttackRoutine("Action_300", 0.4f));
    }

    // --- HELPERS ---

    IEnumerator PerformAttackRoutine(string animName, float duration)
    {
        isAttacking = true;
        PlayAnimationSafe(animName);

        if (movementScript) movementScript.enabled = false;
        if (movementScript) movementScript.rb.linearVelocity = Vector2.zero;

        // Use strict timing or animation check? Keeping duration for now for normal attacks
        // as they are fast, but you can swap to IsAnimationComplete logic if preferred.
        yield return new WaitForSeconds(duration);
        
        isAttacking = false;
        if (movementScript) movementScript.enabled = true;
        
        if (!isSusanooActive) PlayAnimationSafe("Action_0");
    }

    IEnumerator SummonClone()
    {
        if (isSusanooActive) yield break;
        isAttacking = true;
        PlayAnimationSafe("Action_650");
        yield return new WaitForSeconds(0.5f);
        
        if (shadowClonePrefab) 
        {
            float facingDir = transform.localScale.x; 
            Vector3 spawnOffset = new Vector3(2.5f * facingDir, 0, 0); 
            GameObject clone = Instantiate(shadowClonePrefab, transform.position + spawnOffset, Quaternion.identity);
            Vector3 cloneScale = clone.transform.localScale;
            cloneScale.x = facingDir;
            clone.transform.localScale = cloneScale;
        }
        
        isAttacking = false;
        PlayAnimationSafe("Action_0");
    }

    void PlayAnimationSafe(string animName)
    {
        if (animator) animator.Play(animName);
    }

    // NEW HELPER: Checks if a specific animation has finished playing
    bool IsAnimationComplete(string animName)
    {
        if (animator == null) return true;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        
        // Ensure we are checking the correct animation state
        if (info.IsName(animName))
        {
            // normalizedTime >= 1.0f means the animation has played at least once fully
            return info.normalizedTime >= 1.0f;
        }
        return false;
    }
}