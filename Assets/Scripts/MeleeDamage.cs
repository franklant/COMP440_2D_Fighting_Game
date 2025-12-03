using UnityEngine;

public class MeleeDamage : MonoBehaviour
{
    public MonoBehaviour combatScript; 

    [Header("Attack Stats")]
    public float damage = 10f;
    public float stunDamage = 20f;
    public Vector2 knockback = new Vector2(2f, 0f);

    [Header("Visual Effects")]
    public GameObject hitEffectPrefab; // <--- ASSIGN YOUR PREFAB HERE
    
    [HideInInspector] public float facingDirection = 1f;

    void OnTriggerEnter2D(Collider2D col)
    {
        // Prevent hitting self
        if (col.transform.root == transform.root) return;

        bool hitConnected = false;

        // --- 1. CHECK FOR LEGACY CHARACTER ---
        CharacterScript enemyChar = col.GetComponent<CharacterScript>();
        if (enemyChar == null) enemyChar = col.GetComponentInParent<CharacterScript>();

        if (enemyChar != null)
        {
            hitConnected = true;

            // Notify Combat Script
            if (combatScript != null) 
                combatScript.SendMessage("RegisterHit", SendMessageOptions.DontRequireReceiver);

            // Launcher Logic
            bool isLauncher = knockback.y > 1.0f;
            enemyChar.GetHit(damage, stunDamage, isLauncher);
            
            // Apply Physics
            float finalX = knockback.x * facingDirection;
            if (isLauncher) enemyChar.velocity = new Vector3(finalX, knockback.y, 0);
            else enemyChar.velocity = new Vector3(finalX, enemyChar.velocity.y, 0);
        }
        // --- 2. CHECK FOR DUMMY ---
        else
        {
            EnemyDummy dummy = col.GetComponent<EnemyDummy>();
            if (dummy != null)
            {
                hitConnected = true;
                if (combatScript != null) 
                    combatScript.SendMessage("RegisterHit", SendMessageOptions.DontRequireReceiver);
                    
                dummy.TakeHit(damage, new Vector2(knockback.x * facingDirection, knockback.y));
            }
        }

        // --- 3. SPAWN HIT EFFECT ---
        if (hitConnected && hitEffectPrefab != null)
        {
            // Find the exact point where our fist/foot touched the enemy collider
            Vector3 hitPos = col.ClosestPoint(transform.position);
            
            // Spawn the effect
            Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
        }
    }
}