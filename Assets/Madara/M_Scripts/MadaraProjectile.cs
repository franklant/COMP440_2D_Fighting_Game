using UnityEngine;

public class MadaraProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float lifetime = 3f;

    void Start()
    {
        // Destroy the fireball after 'lifetime' seconds so it doesn't clutter the game
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move in the direction we are facing (handled by the negative scale in SpawnVFX)
        // Mathf.Sign ensures we move Left if scale is -1, and Right if scale is 1
        float direction = Mathf.Sign(transform.localScale.x);
        transform.Translate(Vector2.right * speed * direction * Time.deltaTime);
    }
}