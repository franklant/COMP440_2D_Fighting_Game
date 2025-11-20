using UnityEngine;
using UnityEngine.Events;
using System;

public class FighterStatsManager : MonoBehaviour
{
    [Header("--- Configuration ---")]
    [Tooltip("Total health points.")]
    public float maxHealth = 1000f;
    
    [Tooltip("Max amount of super meter (e.g., 300 for 3 bars).")]
    public float maxHyperMeter = 300f;
    
    [Tooltip("Amount of Stun accumulation before becoming Dizzied.")]
    public float maxStun = 100f;
    
    [Tooltip("How fast Stun decreases per second when not taking damage.")]
    public float stunRecoveryRate = 15f;

    [Tooltip("Time in seconds before the combo counter resets.")]
    public float comboResetTime = 2.5f;

    [Header("--- Damage Scaling ---")]
    [Tooltip("Percentage of damage kept per hit in combo (e.g. 0.9 means 10% reduction per hit).")]
    [Range(0.1f, 1.0f)]
    public float scalingFactor = 0.9f;
    [Tooltip("Minimum damage scaling (never deal less than this % of base damage).")]
    [Range(0.1f, 1.0f)]
    public float minScalingCap = 0.2f;

    // --- Runtime State (Read Only for debug) ---
    [Header("--- Debug View ---")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentHyper;
    [SerializeField] private float currentStun;
    [SerializeField] private int currentComboCount;
    [SerializeField] private bool isDizzied;

    private float lastHitTime;

    // --- Events (Hook UI or Animation scripts to these) ---
    // Using System.Action for high-performance code-based listeners
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float, float> OnHyperChanged;  // current, max
    public event Action<float, float> OnStunChanged;   // current, max
    public event Action<int> OnComboUpdated;
    public event Action OnComboEnded;

    // Using UnityEvents for easier dragging/dropping in Inspector (e.g. Play Sound, Trigger Anim)
    [Header("--- Gameplay Events ---")]
    public UnityEvent OnDeath;
    public UnityEvent OnDizzyStart;
    public UnityEvent OnDizzyEnd;

    private void Start()
    {
        currentHealth = maxHealth;
        currentHyper = 0f;
        currentStun = 0f;
        currentComboCount = 0;
        
        // Initialize UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHyperChanged?.Invoke(currentHyper, maxHyperMeter);
    }

    private void Update()
    {
        HandleComboTimer();
        HandleStunRecovery();
    }

    // --- A. Health & Damage Logic ---

    /// <summary>
    /// Call this from your Hitbox script or CharacterScript when this character gets hit.
    /// </summary>
    public void TakeDamage(float baseDamage, float meterGainOnHit, float stunDamage)
    {
        if (currentHealth <= 0) return;

        // 1. Update Combo
        UpdateComboState();

        // 2. Calculate Scaling
        // Formula: Base * (Scaling ^ ComboCount)
        float currentScale = Mathf.Pow(scalingFactor, currentComboCount);
        currentScale = Mathf.Max(currentScale, minScalingCap); // Clamp to minimum
        float finalDamage = baseDamage * currentScale;

        // 3. Apply Health Loss
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 4. Apply Stun
        AddStun(stunDamage);

        // 5. Apply Meter Gain (Defensive Meter Build)
        AddHyperMeter(meterGainOnHit);

        // 6. Check Death
        if (currentHealth <= 0)
        {
            OnDeath.Invoke();
            // Logic to disable controls usually goes here or listener checks health
        }
    }

    /// <summary>
    /// Call this when the character successfully blocks an attack.
    /// </summary>
    public void BlockAttack(float chipDamage, float meterGainOnBlock)
    {
        if (currentHealth <= 0) return;

        // Tiny chip damage, usually no scaling applied or fixed scaling
        currentHealth -= chipDamage; 
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Defending player gains generic meter on block
        AddHyperMeter(meterGainOnBlock);
    }

    /// <summary>
    /// Call this when THIS character hits the ENEMY (to gain offensive meter).
    /// </summary>
    public void OnOffensiveHit(float meterGain)
    {
        AddHyperMeter(meterGain);
    }

    // --- B. Hyper Meter Logic ---
    
    public void AddHyperMeter(float amount)
    {
        currentHyper += amount;
        currentHyper = Mathf.Clamp(currentHyper, 0, maxHyperMeter);
        OnHyperChanged?.Invoke(currentHyper, maxHyperMeter);
    }

    public bool TrySpendMeter(float amount)
    {
        if (currentHyper >= amount)
        {
            currentHyper -= amount;
            OnHyperChanged?.Invoke(currentHyper, maxHyperMeter);
            return true;
        }
        return false;
    }

    // --- C. Combo Counter Logic ---

    private void UpdateComboState()
    {
        lastHitTime = Time.time;
        currentComboCount++;
        OnComboUpdated?.Invoke(currentComboCount);
    }

    private void HandleComboTimer()
    {
        if (currentComboCount > 0 && Time.time > lastHitTime + comboResetTime)
        {
            currentComboCount = 0;
            OnComboEnded?.Invoke();
        }
    }

    // --- D. Stun Gauge Logic ---

    private void AddStun(float amount)
    {
        if (isDizzied) return; // Already stunned

        currentStun += amount;
        lastHitTime = Time.time; // Stun recovery pauses when hit

        if (currentStun >= maxStun)
        {
            currentStun = maxStun;
            StartDizzy();
        }
        
        OnStunChanged?.Invoke(currentStun, maxStun);
    }

    private void HandleStunRecovery()
    {
        // Don't recover stun if currently being combo'd or if fully dizzied (dizzy usually has its own timer in CharacterScript)
        if (!isDizzied && currentStun > 0 && Time.time > lastHitTime + comboResetTime)
        {
            currentStun -= stunRecoveryRate * Time.deltaTime;
            currentStun = Mathf.Max(currentStun, 0);
            OnStunChanged?.Invoke(currentStun, maxStun);
        }
    }

    private void StartDizzy()
    {
        isDizzied = true;
        OnDizzyStart.Invoke();
        // Note: Your CharacterScript should listen to OnDizzyStart to disable input
        // You might use a coroutine here or in CharacterScript to wait 2-3 seconds then call EndDizzy()
        Invoke(nameof(EndDizzy), 3.0f); // Hardcoded 3s dizzy for example
    }

    public void EndDizzy()
    {
        isDizzied = false;
        currentStun = 0;
        OnStunChanged?.Invoke(currentStun, maxStun);
        OnDizzyEnd.Invoke();
    }
}