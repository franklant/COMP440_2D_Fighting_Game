using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public float damage = 30f;
    public string enemyTag; // Set via Initialize
    public FighterStatsManager myStats;
    private bool hasHit = false;

    void OnEnable() { hasHit = false; }

    // FIX 1: Add this missing method
    public void Initialize(string targetTag, FighterStatsManager stats)
    {
        enemyTag = targetTag;
        myStats = stats;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        if (string.IsNullOrEmpty(enemyTag)) return;

        if (collision.CompareTag(enemyTag) || collision.gameObject.name == enemyTag)
        {
            CharacterScript enemyScript = collision.GetComponentInParent<CharacterScript>();
            if (enemyScript != null)
            {
                hasHit = true;
                Debug.Log("HIT: " + collision.name);
                
                // FIX 2: Call GetHit with 1 argument for now to match CharacterScript
                enemyScript.GetHit(damage); 
                
                if (myStats != null) myStats.AddHyperMeter(10f);
            }
        }
    }
}