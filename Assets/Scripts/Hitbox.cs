using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [Header("Attack Data")]
    public float damage = 50f;
    public float stun = 15f;
    public float meterGainOnHit = 10f;

    [Header("Targeting")]
    public string enemyTag = "Player2";

    [Header("References")]
    public FighterStatsManager myStats; // Reference to YOUR stats (to gain meter)

    // --- ONE HIT LOGIC ---
    // Prevents the hitbox from registering multiple hits in a single swing
    private bool hasHit = false; 

    void OnEnable()
    {
        // Reset the flag every time the Animation turns this object ON
        hasHit = false;
    }

    void Start()
    {
        // Attempt to find the stats manager on the parent (Gojo)
        myStats = GetComponentInParent<FighterStatsManager>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. If we already hit someone this swing, stop.
        if (hasHit) return; 

        // 2. Check if we hit the correct enemy
        if (collision.CompareTag(enemyTag) || collision.gameObject.name == enemyTag)
        {
            CharacterScript enemyScript = collision.GetComponentInParent<CharacterScript>();

            if (enemyScript != null)
            {
                // 3. Mark as hit immediately
                hasHit = true; 

                Debug.Log("HIT CONFIRMED: " + collision.name);

                // 4. Deal Damage & Stun
                enemyScript.GetHit(damage, stun);

                // 5. Reward Meter to Attacker
                if (myStats != null)
                {
                    myStats.AddHyperMeter(meterGainOnHit);
                }

                // 6. Trigger Hitstop (Game Feel)
                // 0.08f is snappy for melee hits
                if (GameFeelManager.instance != null)
                {
                    GameFeelManager.instance.HitStop(0.08f); 
                }
            }
        }
    }
}