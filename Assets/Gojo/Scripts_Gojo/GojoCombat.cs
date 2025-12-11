using UnityEngine;
using System.Collections;

public class GojoCombat : MonoBehaviour
{
    public Animator animator;
    public GojoMovement movementScript;

    // UPDATED: Now references the new Hitbox script
    public Hitbox hitboxScript;

    [Header("Combat Settings")]
    public float comboWindow = 1.0f;
    public KeyCode buttonA = KeyCode.J;
    public KeyCode buttonB = KeyCode.K;

    private int comboStepA = 0;
    private int comboStepB = 0;
    private float lastAttackTime = 0;
    private bool moveContact = false; 
    private Coroutine attackRoutine;

    void Start()
    {
        // Auto-find the Hitbox if you forgot to drag it in
        if (hitboxScript == null) hitboxScript = GetComponentInChildren<Hitbox>();
    }

    public void RegisterHit()
    {
        moveContact = true;
    }

    void Update()
    {
        if (Time.time - lastAttackTime > comboWindow)
        {
            comboStepA = 0;
            comboStepB = 0;
            moveContact = false;
        }

        // --- COMBO A (J Series) ---
        if (Input.GetKeyDown(buttonA))
        {
            if (comboStepA == 0 || moveContact) PerformComboA();
            else Debug.Log("Combo dropped: You missed!");
        }

        // --- COMBO B (K Series) ---
        if (Input.GetKeyDown(buttonB))
        {
            if (comboStepB == 0 || moveContact) PerformComboB();
        }
    }

    void PerformComboA()
    {
        comboStepB = 0; lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepA)
        {
            case 0: 
                clipName = "Action_200"; 
                SetHitStats(10, 15f, false); // Damage, Stun, Aerial
                ApplyForwardStep(3f); 
                comboStepA = 1; break;
            case 1: 
                clipName = "Action_210"; 
                SetHitStats(10, 15f, false); 
                ApplyForwardStep(4f); 
                comboStepA = 2; break;
            case 2: 
                clipName = "Action_220"; 
                SetHitStats(15, 20f, false); 
                ApplyForwardStep(5f); 
                comboStepA = 3; break;
            case 3: 
                clipName = "Action_230"; 
                SetHitStats(20, 30f, true); // Launcher (Aerial = true)
                ApplyForwardStep(5f); 
                comboStepA = 4; break; 
            case 4:
                StartCoroutine(PerformTeleportSpike());
                comboStepA = 0; break;
        }
        
        if (comboStepA != 0) PlayAttack(clipName);
    }

    IEnumerator PerformTeleportSpike()
    {
        yield return new WaitForSeconds(0.25f);
        
        // Simple Teleport Logic (Simplified for stability)
        GameObject target = GameObject.FindGameObjectWithTag("Player2"); // Assuming Enemy is P2
        if (target != null)
        {
            float side = (transform.position.x < target.transform.position.x) ? -1f : 1f;
            transform.position = target.transform.position + new Vector3(0.5f * side, 1.0f, 0);
        }

        SetHitStats(35, 40f, true); // Heavy Spike
        PlayAttack("Action_240");
    }

    void PerformComboB()
    {
        comboStepA = 0; lastAttackTime = Time.time;
        string clipName = "";

        switch (comboStepB)
        {
            case 0: clipName = "Action_300"; SetHitStats(15, 15f, false); ApplyForwardStep(3f); comboStepB = 1; break;
            case 1: clipName = "Action_310"; SetHitStats(15, 15f, false); ApplyForwardStep(4f); comboStepB = 2; break;
            case 2: clipName = "Action_320"; SetHitStats(20, 20f, false); ApplyForwardStep(4f); comboStepB = 3; break;
            case 3: clipName = "Action_330"; SetHitStats(25, 30f, true); ApplyForwardStep(5f); comboStepB = 0; break; 
        }
        PlayAttack(clipName);
    }

    void SetHitStats(float dmg, float stun, bool aerial)
    {
        // Update the Hitbox script directly
        if (hitboxScript != null)
        {
            hitboxScript.damage = dmg;
            hitboxScript.stun = stun;
            hitboxScript.isAerial = aerial;
            
            // Activate it for the attack
            hitboxScript.gameObject.SetActive(true);
            StartCoroutine(TurnOffHitbox(0.2f));
        }
    }

    IEnumerator TurnOffHitbox(float delay)
    {
        yield return new WaitForSeconds(delay);
        if(hitboxScript) hitboxScript.gameObject.SetActive(false);
    }

    void ApplyForwardStep(float speed)
    {
        if (movementScript && movementScript.rb)
        {
            float facing = transform.localScale.x;
            movementScript.rb.linearVelocity = new Vector2(speed * facing, 0); 
        }
    }

    void PlayAttack(string clipName)
    {
        moveContact = false;
        animator.Play(clipName, 0, 0f); 
        if (movementScript) movementScript.enabled = false;

        float duration = 0.5f; // Hardcoded safety, usually calculate from clip
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(RecoverMovement(duration));
    }

    IEnumerator RecoverMovement(float duration)
    {
        yield return new WaitForSeconds(0.1f); 
        if(movementScript && movementScript.rb) movementScript.rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(duration);
        if (movementScript) 
        {
            movementScript.enabled = true;
            animator.Play("Action_0"); 
        }
    }
}