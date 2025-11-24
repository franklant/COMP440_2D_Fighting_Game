using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StatsUIHandler : MonoBehaviour
{
    [Header("Target")]
    public FighterStatsManager targetFighter;

    [Header("Health Bar")]
    public Image healthBarFill;
    public Image healthBarBackground; // Assign the background image to flash it red
    [Tooltip("Health percentage to start flashing red (e.g. 0.25 for 25%)")]
    public float criticalHealthThreshold = 0.25f;
    public Color normalHealthColor = Color.green;
    public Color criticalHealthColor = Color.red;

    [Header("Hyper Meter")]
    public Image hyperMeterFill;
    public GameObject hyperMaxGlowFX; // Assign a glow image/particle to enable when full
    [Tooltip("Number of meter bars (visual only, logic is in StatsManager)")]
    public int maxMeterLevels = 3; 

    [Header("Combo UI")]
    public GameObject comboGroup;
    public TextMeshProUGUI comboCountText;
    public TextMeshProUGUI comboLabelText; // Optional: "Hits", "Awesome!", etc.

    private bool isFlashingLowHealth = false;

    private void Start()
    {
        // Ensure visual defaults
        if(comboGroup) comboGroup.SetActive(false);
        if(hyperMaxGlowFX) hyperMaxGlowFX.SetActive(false);
        if(healthBarFill) healthBarFill.color = normalHealthColor;
    }

    private void OnEnable()
    {
        if (targetFighter == null) return;

        targetFighter.OnHealthChanged += UpdateHealth;
        targetFighter.OnHyperChanged += UpdateHyper;
        targetFighter.OnComboUpdated += UpdateCombo;
        targetFighter.OnComboEnded += HideCombo;
    }

    private void OnDisable()
    {
        if (targetFighter == null) return;

        targetFighter.OnHealthChanged -= UpdateHealth;
        targetFighter.OnHyperChanged -= UpdateHyper;
        targetFighter.OnComboUpdated -= UpdateCombo;
        targetFighter.OnComboEnded -= HideCombo;
    }

    // --- Health Logic ---
    void UpdateHealth(float current, float max)
    {
        float pct = current / max;
        if (healthBarFill) healthBarFill.fillAmount = pct;

        // Check for Low Health Flashing
        if (pct <= criticalHealthThreshold && !isFlashingLowHealth)
        {
            StartCoroutine(FlashLowHealth());
        }
        else if (pct > criticalHealthThreshold)
        {
            isFlashingLowHealth = false; // Stops coroutine loop
            if (healthBarBackground) healthBarBackground.color = Color.black; // Reset to default
            if (healthBarFill) healthBarFill.color = normalHealthColor;
        }
    }

    IEnumerator FlashLowHealth()
    {
        isFlashingLowHealth = true;
        bool flashToggle = false;

        while (isFlashingLowHealth)
        {
            // Toggle bar color or background color
            if (healthBarFill) 
                healthBarFill.color = flashToggle ? criticalHealthColor : Color.white;
            
            flashToggle = !flashToggle;
            yield return new WaitForSeconds(0.2f);
        }
    }

    // --- Hyper Meter Logic ---
    void UpdateHyper(float current, float max)
    {
        float pct = current / max;
        if (hyperMeterFill) hyperMeterFill.fillAmount = pct;

        // Glow when full (approximate float check)
        if (hyperMaxGlowFX)
        {
            bool isFull = current >= max - 1f; 
            hyperMaxGlowFX.SetActive(isFull);
        }
    }

    // --- Combo Logic ---
    void UpdateCombo(int count)
    {
        // DEBUG: Did the UI hear the shout?
        Debug.Log("UI RECEIVED COMBO: " + count);
        if (count < 2) return; // Hide 1-hit combos

        if (comboGroup) comboGroup.SetActive(true);
        
        if (comboCountText) 
        {
            comboCountText.text = count.ToString();
            // Simple punch scaling effect
            comboCountText.transform.localScale = Vector3.one * 1.5f;
            StopCoroutine("ResetComboScale"); // Stop existing logic if any
            StartCoroutine(ResetComboScale(comboCountText.transform));
        }
        
        if (comboLabelText) comboLabelText.text = "HITS";
    }

    IEnumerator ResetComboScale(Transform target)
    {
        float t = 0;
        while(t < 1f)
        {
            t += Time.deltaTime * 5f;
            target.localScale = Vector3.Lerp(target.localScale, Vector3.one, t);
            yield return null;
        }
    }

    void HideCombo()
    {
        if (comboGroup) comboGroup.SetActive(false);
    }
}