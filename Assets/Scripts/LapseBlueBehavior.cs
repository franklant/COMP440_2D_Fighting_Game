using UnityEngine;

public class LapseBlueBehavior : MonoBehaviour
{
    private Transform rootParent; // Gojo's transform
    private float timer = 0f;
    private float facingDir = 1f; // 1 for Right, -1 for Left

    [Header("Configuration")]
    public float orbitRadius = 2.0f; // Converted from MUGEN's 60 pixels
    public float orbitSpeed = 5.0f;  // Speed of the Sine wave
    public float pulseSpeed = 5.0f;
    public float baseScale = 2.5f;   // Our manual size fix

    public void Initialize(Transform parent, bool isFacingLeft)
    {
        rootParent = parent;
        facingDir = isFacingLeft ? -1f : 1f;
        transform.localScale = Vector3.zero; // Start invisible
    }

    void Update()
    {
        if (rootParent == null) { Destroy(gameObject); return; }

        timer += Time.deltaTime;

        // --- 1. REPLICATE MOVEMENT MATH ---
        // Calculates a Sine wave to oscillate the X position
        float xOffset = orbitRadius * Mathf.Sin(timer * orbitSpeed) * facingDir;
        float yOffset = 0.5f * Mathf.Sin(timer * orbitSpeed); // Slight bobbing

        transform.position = rootParent.position + new Vector3(xOffset, 1.5f + yOffset, 0);

        // --- 2. REPLICATE SCALING MATH ---
        // Calculates the pulsing effect
        float pulse = 1.0f + 0.4f * Mathf.Sin(timer * pulseSpeed);
        
        // Combine base scale with the pulse
        float finalScale = baseScale * pulse;
        
        // Intro grow effect (first 0.2 seconds)
        if (timer < 0.2f) finalScale *= (timer / 0.2f);

        transform.localScale = new Vector3(finalScale, finalScale, 1f);

        // --- 3. AUTO-DESTROY ---
        // Destroys after ~2.5 seconds (matches MUGEN duration)
        if (timer > 2.5f) Destroy(gameObject);
    }
}