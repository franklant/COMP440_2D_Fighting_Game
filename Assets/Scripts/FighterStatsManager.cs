using UnityEngine;
using UnityEngine.Events;
using System;

public class FighterStatsManager : MonoBehaviour
{
    [Header("--- Configuration ---")]
    public float maxHealth = 1000f;
    public float maxHyperMeter = 300f;
    public float maxStun = 100f;
    public float stunRecoveryRate = 15f;
    public float comboResetTime = 2.5f;

    [Header("--- Damage Scaling ---")]
    [Range(0.1f, 1.0f)] public float scalingFactor = 0.9f;
    [Range(0.1f, 1.0f)] public float minScalingCap = 0.2f;

    [Header("--- Debug View ---")]
    [SerializeField] private float currentHealth;
    [SerializeField] public float currentHyper;
    [SerializeField] private float currentStun;
    [SerializeField] private int currentComboCount;
    [SerializeField] private bool isDizzied;

    // Public Accessors
    public float CurrentHealth => currentHealth;
    public float CurrentHyper => currentHyper;
    public float CurrentStun => currentStun;

    private float lastHitTime;

    // Events
    public event Action<float, float> OnHealthChanged; 
    public event Action<float, float> OnHyperChanged;  
    public event Action<float, float> OnStunChanged;   
    public event Action<int> OnComboUpdated;
    public event Action OnComboEnded;

    [Header("--- Gameplay Events ---")]
    public UnityEvent OnDeath;
    public UnityEvent OnDizzyStart;
    public UnityEvent OnDizzyEnd;

    private void Start()
    {
        currentHealth = maxHealth;
        currentHyper = maxHyperMeter; 
        currentStun = 0f;
        currentComboCount = 0;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHyperChanged?.Invoke(currentHyper, maxHyperMeter);
    }

    private void Update()
    {
        HandleComboTimer();
        HandleStunRecovery();

        // DEBUG KEYS
        if (Input.GetKeyDown(KeyCode.Z)) TakeDamage(100f, 10f, 0f);
        if (Input.GetKeyDown(KeyCode.X)) { currentHealth = maxHealth; OnHealthChanged?.Invoke(currentHealth, maxHealth); }
        if (Input.GetKeyDown(KeyCode.M)) AddHyperMeter(maxHyperMeter);
    }

    
    public void OnOffensiveHit(float meterGain)
    {
        AddHyperMeter(meterGain);
    }

    public void TakeDamage(float baseDamage, float meterGainOnHit, float stunDamage)
    {
        if (currentHealth <= 0) return;
        UpdateComboState();

        float currentScale = Mathf.Pow(scalingFactor, currentComboCount);
        currentScale = Mathf.Max(currentScale, minScalingCap);
        float finalDamage = baseDamage * currentScale;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        AddStun(stunDamage);
        AddHyperMeter(meterGainOnHit);

        if (currentHealth <= 0)
        {
            Debug.LogWarning("Health is low.");
        }
        if (currentHealth <= 0) OnDeath.Invoke();
    }

    public void BlockAttack(float chipDamage, float meterGainOnBlock)
    {
        if (currentHealth <= 0) return;
        currentHealth -= chipDamage; 
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        AddHyperMeter(meterGainOnBlock);
    }

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

    private void UpdateComboState()
    {
        lastHitTime = Time.time;
        currentComboCount++;
        // DEBUG: Did the math update?
        Debug.Log("COMBO COUNT IS NOW: " + currentComboCount);
        
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

    private void AddStun(float amount)
    {
        if (isDizzied) return; 
        currentStun += amount;
        lastHitTime = Time.time; 
        if (currentStun >= maxStun)
        {
            currentStun = maxStun;
            isDizzied = true;
            OnDizzyStart.Invoke();
            Invoke(nameof(EndDizzy), 3.0f); 
        }
        OnStunChanged?.Invoke(currentStun, maxStun);
    }

    private void HandleStunRecovery()
    {
        if (!isDizzied && currentStun > 0 && Time.time > lastHitTime + comboResetTime)
        {
            currentStun -= stunRecoveryRate * Time.deltaTime;
            currentStun = Mathf.Max(currentStun, 0);
            OnStunChanged?.Invoke(currentStun, maxStun);
        }
    }

    public void EndDizzy()
    {
        isDizzied = false;
        currentStun = 0;
        OnStunChanged?.Invoke(currentStun, maxStun);
        OnDizzyEnd.Invoke();
    }
}