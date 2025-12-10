using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class AIController : MonoBehaviour
{
    // --- External Reference ---
    private CharacterScript characterScript;
    private FighterStatsManager opponentStats;

    [Header("--- AI Settings ---")]
    public DifficultySetting difficulty = DifficultySetting.Medium;

    [Header("--- Cooldowns (in seconds) ---")]
    [Tooltip("Minimum time between any two attack/movement actions.")]
    public float actionCooldown = 0.5f; 
    public float jumpCooldown = 5.0f;
    public float specialMoveCooldown = 3.0f;

    [Header("--- Decision Distances (in units) ---")]
    [Tooltip("If distance > walkForwardRange, AI walks toward enemy.")]
    public float walkForwardRange = 4.0f; 
    [Tooltip("If distance <= retreatRange, AI walks away.")]
    public float retreatRange = 1.5f;
    [Tooltip("If distance between SpecialMin/Max, AI favors special moves.")]
    public float specialMoveMinRange = 2.0f;
    public float specialMoveMaxRange = 4.0f;
    [Tooltip("If distance <= attackRange, AI uses close attacks (jab/heavy).")]
    public float closeAttackRange = 2.0f;

    // --- Internal Timers ---
    private float lastActionTime;
    private float lastJumpTime;
    private float lastSpecialMoveTime;

    // --- Difficulty Modifiers ---
    private float reactionSpeed = 0.5f; 
    private float blockChance = 0.3f;   
    private float aggressionLevel = 1.0f; 

    // --- State Tracking ---
    private bool isBlockingAttempting = false; 

    public enum DifficultySetting
    {
        Easy,
        Medium,
        Hard
    }

    void Start()
    {
        characterScript = GetComponent<CharacterScript>();
        if (characterScript == null)
        {
            Debug.LogError("AIController requires a CharacterScript on the same GameObject!");
            enabled = false;
            return;
        }

        characterScript.isPlayer = false;
        
        SetAIDifficulty();

        if (characterScript.enemyTarget != null)
        {
            opponentStats = characterScript.enemyTarget.GetComponent<FighterStatsManager>();
        } else {
            StartCoroutine(WaitForTargetAndGetStats());
        }

        StartCoroutine(AILoop());
    }

    IEnumerator WaitForTargetAndGetStats()
    {
        while (characterScript.enemyTarget == null)
        {
            yield return null; 
        }
        opponentStats = characterScript.enemyTarget.GetComponent<FighterStatsManager>();
    }

    private void SetAIDifficulty()
    {
        switch (difficulty)
        {
            case DifficultySetting.Easy:
                reactionSpeed = 0.8f; 
                blockChance = 0.1f;   
                aggressionLevel = 0.8f;
                break;
            case DifficultySetting.Medium:
                reactionSpeed = 0.5f; 
                blockChance = 0.3f;   
                aggressionLevel = 1.0f;
                break;
            case DifficultySetting.Hard:
                reactionSpeed = 0.2f; 
                blockChance = 0.6f;   
                aggressionLevel = 1.2f;
                break;
        }
    }

    IEnumerator AILoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(reactionSpeed); 

            if (characterScript.isAttacking || characterScript.myStats.CurrentHealth <= 0 || 
                characterScript.currentState == characterScript.GetState(CharacterScript.STATE.DIZZIED) ||
                characterScript.currentState == characterScript.GetState(CharacterScript.STATE.KNOCKBACK) ||
                characterScript.currentState == characterScript.GetState(CharacterScript.STATE.AERIALKNOCKBACK))
            {
                AI_StopAction();
                continue;
            }

            AI_MakeDecision();
        }
    }

    void Update()
    {
        if (characterScript.enemyTarget == null || opponentStats == null) return;
        
        if (isBlockingAttempting)
        {
            if (!IsOpponentAttacking() || Time.time > lastActionTime + (reactionSpeed * 2f) )
            {
                AI_StopAction(); 
            }
        }
        else
        {
            AI_HandleBlocking();
        }
    }

    // =========================================================
    //               AI INPUTS (Calling CharacterScript)
    // =========================================================

    private void AI_Move(float input) 
    {
        characterScript.hDirection = input; 
        lastActionTime = Time.time;
    }

    private void AI_Jump() 
    {
        if (Time.time < lastJumpTime + jumpCooldown) return;
        if (!characterScript.isGrounded) return;
        
        characterScript.isJumping = true; 
        StartCoroutine(ResetJumpInput()); 
        lastActionTime = Time.time;
        lastJumpTime = Time.time;
    }

    IEnumerator ResetJumpInput()
    {
        yield return null; 
        characterScript.isJumping = false;
    }

    private void AI_LightAttack() 
    {
        if (Time.time < lastActionTime + actionCooldown) return;
        
        characterScript.PerformAttack("Attack", characterScript.lightPunch.damage, 1, characterScript.lightPunch);
        lastActionTime = Time.time;
    }
    
    private void AI_HeavyAttack() 
    {
        if (Time.time < lastActionTime + actionCooldown) return;
        
        characterScript.PerformAttack("Kick", characterScript.kick.damage, 2, characterScript.kick);
        lastActionTime = Time.time;
    }

    private void AI_SpecialMove()
    {
        if (Time.time < lastSpecialMoveTime + specialMoveCooldown || Time.time < lastActionTime + actionCooldown) return;
        
        // FIX: Using the now public 'currentHyper' field
        if (characterScript.myStats.currentHyper >= 100f) 
        {
            if (characterScript.characterName == "Gojo") 
                characterScript.StartCoroutine("Gojo_Meter1");
            else if (characterScript.characterName == "Sukuna")
                characterScript.StartCoroutine("Sukuna_Meter1");
            else 
                characterScript.PerformSuperMove("Meter1", characterScript.meter1); 

            lastActionTime = Time.time;
            lastSpecialMoveTime = Time.time;
        }
    }
    
    private void AI_Block()
    {
        characterScript.isBlocking = true;
        isBlockingAttempting = true;
        lastActionTime = Time.time; 
    }

    private void AI_StopAction()
    {
        characterScript.hDirection = 0;
        characterScript.isBlocking = false;
        isBlockingAttempting = false;
    }

    // =========================================================
    //               AI BEHAVIOR LOGIC
    // =========================================================

    private bool IsOpponentAttacking()
    {
        if (opponentStats != null)
        {
            CharacterScript opponentScript = opponentStats.GetComponent<CharacterScript>();
            if (opponentScript != null)
            {
                return opponentScript.isAttacking;
            }
        }
        return false; 
    }

    private void AI_HandleBlocking()
    {
        if (Time.time < lastActionTime + (reactionSpeed * 0.5f) ) return;
        
        float distanceToOpponent = Vector3.Distance(transform.position, characterScript.enemyTarget.position);
        
        if (IsOpponentAttacking() && distanceToOpponent <= closeAttackRange)
        {
            if (Random.value < blockChance)
            {
                AI_Block();
            }
        }
    }

    private void AI_MakeDecision()
    {
        float distance = Vector3.Distance(transform.position, characterScript.enemyTarget.position);
        float decisionRoll = Random.value * aggressionLevel;

        if (Time.time > lastJumpTime + jumpCooldown && Random.value < 0.15f * aggressionLevel)
        {
            AI_Jump();
            return;
        }

        if (distance <= retreatRange)
        {
            if (decisionRoll < 0.4f) 
            {
                AI_StopAction();
                AI_LightAttack(); 
            }
            else
            {
                AI_MoveAwayFromEnemy();
            }
            return;
        }

        if (distance > retreatRange && distance <= closeAttackRange)
        {
            if (decisionRoll < 0.6f)
            {
                AI_LightAttack(); 
            }
            else
            {
                AI_HeavyAttack();
            }
            return;
        }
        
        if (distance > specialMoveMinRange && distance <= specialMoveMaxRange)
        {
            // FIX: Using the now public 'currentHyper' field
            if (decisionRoll > 0.5f && characterScript.myStats.currentHyper >= 100f) 
            {
                AI_SpecialMove();
            }
            else
            {
                AI_MoveTowardsEnemy();
            }
            return;
        }

        if (distance > walkForwardRange)
        {
            AI_MoveTowardsEnemy();
            return;
        }
        
        AI_StopAction();
    }

    private void AI_MoveTowardsEnemy()
    {
        float moveInput = (transform.position.x < characterScript.enemyTarget.position.x) ? 1.0f : -1.0f;
        AI_Move(moveInput);
    }
    
    private void AI_MoveAwayFromEnemy()
    {
        float moveInput = (transform.position.x < characterScript.enemyTarget.position.x) ? -1.0f : 1.0f;
        AI_Move(moveInput);
    }
}