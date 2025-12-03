using UnityEngine;

public class MeleeDamage : MonoBehaviour
{
    // Removing specific script references to make it universal
    // public GojoCombat combatScript; <--- OLD
    // public MadaraCombat madaraScript; <--- OLD

    // Universal reference to the parent character
    public MonoBehaviour combatScript; 

    [Header("Current Attack Stats")]
    public float damage = 10f;
    public Vector2 knockback = new Vector2(2f, 0f);
    
    [HideInInspector] public float facingDirection = 1f;

    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.CompareTag("Enemy"))
        {
            // 1. Tell the Combat Script we landed a hit (Universal)
            if (combatScript != null)
            {
                // This calls the function "RegisterHit" on WHATEVER script is attached
                combatScript.SendMessage("RegisterHit", SendMessageOptions.DontRequireReceiver);
            }

            // 2. Apply Damage to Enemy
            EnemyDummy enemy = col.GetComponent<EnemyDummy>();
            if(enemy != null)
            {
                Vector2 finalKnockback = new Vector2(knockback.x * facingDirection, knockback.y);
                enemy.TakeHit(damage, finalKnockback);
            }
        }
    }
}