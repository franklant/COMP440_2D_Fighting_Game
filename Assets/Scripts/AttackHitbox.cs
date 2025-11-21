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
            // --- NEW HUD LOGIC START ---
            if (CombatHUDManager.Instance != null)
            {
                // 1. Check First Attack
                CombatHUDManager.Instance.CheckFirstAttack(ownerStats.name);

                // 2. Check Counter Hit (If enemy was attacking when hit)
                if (enemyChar.currentState == 4) // 4 is ATTACKING enum index
                {
                    CombatHUDManager.Instance.ShowCounterHit();
                }
                
                // 3. Reversal Logic (Optional implementation)
                // This is harder to detect without a "JustWokeUp" timer in CharacterScript,
                // but if you add a float `wakeupTimer` to CharacterScript, check it here.
            }
            // --- NEW HUD LOGIC END ---

            // 3. Deal Damage to the Enemy
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