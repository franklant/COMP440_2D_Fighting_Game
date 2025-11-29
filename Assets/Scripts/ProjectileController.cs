using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float damage = 200f; // This gets overwritten by CharacterScript
    public Rigidbody2D rb;
    
    // FIX: Add this variable so CharacterScript can assign it
    public string ownerTag; 

    [Header("On Hit VFX")]
    public GameObject hitEffectPrefab; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Fly forward based on rotation
        rb.linearVelocity = transform.right * speed;
        
        // Self-destruct after 3 seconds if it hits nothing
        Destroy(gameObject, 3f); 
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. Ignore the Shooter (Player1/Player2) based on ownerTag
        if (hitInfo.CompareTag(ownerTag)) return;

        // 2. Check for Enemy
        CharacterScript enemy = hitInfo.GetComponent<CharacterScript>();
        // Fallback: Check parent if we hit a child hurtbox
        if (enemy == null) enemy = hitInfo.GetComponentInParent<CharacterScript>();

        if (enemy != null)
        {
            // 3. Deal Damage
            // FIX: Call GetHit with only 1 argument to match CharacterScript
            enemy.GetHit(damage); 

            // 4. Trigger Hitstop 
            if (GameFeelManager.instance != null)
            {
                GameFeelManager.instance.HitStop(0.12f); 
            }

            // 5. Small Camera Shake on Impact
            CameraShake shaker = Camera.main.GetComponent<CameraShake>();
            if (shaker != null) 
            {
                StartCoroutine(shaker.Shake(0.15f, 0.1f)); 
            }

            // 6. Spawn Explosion VFX
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // 7. Destroy Bullet
            Destroy(gameObject); 
        }
        
        // 8. Destroy if hitting the ground/wall
        if (hitInfo.CompareTag("Ground")) 
        {
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}