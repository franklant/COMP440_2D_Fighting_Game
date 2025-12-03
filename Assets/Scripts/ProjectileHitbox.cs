using UnityEngine;

public class ProjectileHitbox : MonoBehaviour
{
    public float damage = 10f;
    public string targetTag = "Enemy"; // Ensure your enemy object has this Tag
    public GameObject hitEffect; // Optional: Explosion particle

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(targetTag))
        {
            Debug.Log($"{gameObject.name} hit {collision.name} for {damage} damage!");
            
            // Add your enemy health logic here, e.g.:
            // collision.GetComponent<EnemyHealth>().TakeDamage(damage);

            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);

            // Destroy projectile unless it's "Blue" (which stays out)
            if (!gameObject.name.Contains("Blue")) 
                Destroy(gameObject);
        }
    }
}