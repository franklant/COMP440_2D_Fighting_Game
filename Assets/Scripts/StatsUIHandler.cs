using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsUIHandler : MonoBehaviour
{
    [Header("References")]
    public FighterStatsManager targetFighter;

    [Header("UI Elements")]
    public Image healthBar;
    public Image hyperMeterBar; // Assume fill amount 0 to 1
    public Image stunBar;
    
    [Header("Combo UI")]
    public TextMeshProUGUI comboText;
    public GameObject comboGroup; // Parent object to hide/show

    private void OnEnable()
    {
        if (targetFighter == null) return;

        targetFighter.OnHealthChanged += UpdateHealth;
        targetFighter.OnHyperChanged += UpdateHyper;
        targetFighter.OnStunChanged += UpdateStun;
        targetFighter.OnComboUpdated += UpdateCombo;
        targetFighter.OnComboEnded += HideCombo;
    }

    private void OnDisable()
    {
        if (targetFighter == null) return;

        targetFighter.OnHealthChanged -= UpdateHealth;
        targetFighter.OnHyperChanged -= UpdateHyper;
        targetFighter.OnStunChanged -= UpdateStun;
        targetFighter.OnComboUpdated -= UpdateCombo;
        targetFighter.OnComboEnded -= HideCombo;
    }

    void UpdateHealth(float current, float max)
    {
        if (healthBar) healthBar.fillAmount = current / max;
    }

    void UpdateHyper(float current, float max)
    {
        if (hyperMeterBar) hyperMeterBar.fillAmount = current / max;
    }

    void UpdateStun(float current, float max)
    {
        if (stunBar) stunBar.fillAmount = current / max;
        
        // Optional: Change color if close to dizzy
        if (stunBar && (current/max) > 0.8f) stunBar.color = Color.red;
        else if (stunBar) stunBar.color = Color.yellow;
    }

    void UpdateCombo(int count)
    {
        if (count < 2) return; // Usually don't show "1 Hit Combo"

        if (comboGroup) comboGroup.SetActive(true);
        if (comboText) comboText.text = count + " HITS!";
        
        // Optional: Add a little shake or punch animation to the text here
    }

    void HideCombo()
    {
        if (comboGroup) comboGroup.SetActive(false);
    }
}