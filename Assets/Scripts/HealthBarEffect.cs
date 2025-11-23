using UnityEngine;
using UnityEngine.UI;

public class HealthBarEffect : MonoBehaviour
{
    [Header("References")]
    public Image myImage; // Drag Health_BG here
    public FighterStatsManager myStats; // Drag Player1 (Gojo) here

    [Header("Settings")]
    public float lowHealthThreshold = 0.3f; // 30% Health
    public float flashSpeed = 5f;
    
    [Header("Colors")]
    public Color normalColor = Color.red;   // The standard background color
    public Color flashColor = Color.white; // The color to flash to (Critical)

    void Start()
    {
        // Auto-grab the image if not assigned
        if (myImage == null) myImage = GetComponent<Image>();
        
        // Ensure we start with the normal color
        if (myImage != null) myImage.color = normalColor;
    }

    void Update()
    {
        if (myStats == null || myImage == null) return;

        // *** FIX: Use the capitalized .CurrentHealth property ***
        float healthPercent = myStats.CurrentHealth / myStats.maxHealth;

        // Check if we are in Critical State
        if (healthPercent <= lowHealthThreshold && healthPercent > 0)
        {
            // PingPong bounces value between 0 and 1
            float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
            
            // Smoothly blend between Red and White
            myImage.color = Color.Lerp(normalColor, flashColor, t);
        }
        else
        {
            // Reset to normal if healed or dead
            myImage.color = normalColor;
        }
    }
}