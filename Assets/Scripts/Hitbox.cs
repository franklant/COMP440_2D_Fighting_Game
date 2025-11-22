using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [Header("Attack Data")]
    public float damage = 50f;
    public float stun = 15f;
    public float meterGainOnHit = 10f; // How much meter YOU get for landing this

    [Header("Targeting")]
    public string enemyTag = "Player2";

    [Header("References")]
    // We need a reference to OURSELVES to give ourselves meter
    public FighterStatsManager myStats; 

    void Start()
    {
        // Automatically find the stats manager on the parent (Gojo)
        myStats = GetComponentInParent<FighterStatsManager>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if we hit the correct enemy Hurtbox
        if (collision.CompareTag(enemyTag) || collision.gameObject.name == enemyTag)
        {
            // 1. Get the Enemy's Script
            CharacterScript enemyScript = collision.GetComponentInParent<CharacterScript>();

            if (enemyScript != null)
            {
                Debug.Log("Hit Confirmed on " + enemyScript.name);

                // 2. Deal Damage to Enemy
                // We pass the damage and stun values to the enemy's script
                enemyScript.GetHit(damage, stun);

                // 3. Reward Ourselves (Meter Gain)
                if (myStats != null)
                {
                    myStats.OnOffensiveHit(meterGainOnHit);
                }

                // 4. Disable Hitbox so it doesn't hit 60 times per second
                gameObject.SetActive(false);
            }
        }
    }
}