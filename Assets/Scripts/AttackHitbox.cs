using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Attack Data")]
    public float damage = 50f;
    public float hitStun = 10f; // Amount of stun gauge to fill
    public float meterGain = 10f; // Meter gained by attacker on hit

    [Header("Owner Reference")]
    public FighterStatsManager ownerStats; // Drag YOUR player's StatsManager here

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check if we hit a Character
        CharacterScript enemyChar = other.GetComponent<CharacterScript>();
        
        // 2. Make sure we aren't hitting ourselves
        if (enemyChar != null && enemyChar.gameObject != ownerStats.gameObject)
        {
            // 3. Deal Damage to the Enemy
            // We assume the enemy also has the CharacterScript + FighterStatsManager setup
            enemyChar.GetHit(damage, hitStun);

            // 4. Give Meter to the Attacker (Us)
            if (ownerStats != null)
            {
                ownerStats.OnOffensiveHit(meterGain);
            }

            Debug.Log("Hit " + other.name + " for " + damage + " damage!");
        }
    }
}