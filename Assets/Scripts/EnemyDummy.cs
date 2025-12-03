using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDummy : MonoBehaviour
{
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure enemy doesn't tip over like a bowling pin
        rb.freezeRotation = true; 
        // Adjust drag so they don't fly forever
        rb.linearDamping = 1.0f; 
    }

    public void TakeHit(float damage, Vector2 knockback)
    {
        // 1. Log Damage
        Debug.Log($"Enemy took {damage} damage!");

        // 2. Reset Velocity (So combos feel snappy, not floaty)
        rb.linearVelocity = Vector2.zero;

        // 3. Apply Knockback Force
        // Impulse mode is best for fighting game hits
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }
}