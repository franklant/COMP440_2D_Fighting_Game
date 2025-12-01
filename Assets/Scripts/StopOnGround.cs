using UnityEngine;

public class StopOnGround : MonoBehaviour
{
    public float disappearTime = 2f;   // How long before the object disappears after hitting the ground
    public float respawnDelay = 3f;    // How long to wait before respawning
    public Vector3 respawnPosition;    // Optional: assign in Inspector; otherwise uses starting position

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Save starting position if none assigned
        if (respawnPosition == Vector3.zero)
            respawnPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If the falling object hits the ground
        if (collision.collider.CompareTag("Ground"))
        {
            if (rb != null)
            {
                // Stop movement
                rb.linearVelocity = Vector2.zero;

                // Freeze position so it doesn't keep sliding or falling
                rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

                // Start disappearance & respawn routine
                StartCoroutine(DisappearAndRespawn());
            }
        }
    }

    private System.Collections.IEnumerator DisappearAndRespawn()
    {
        // Wait before disappearing
        yield return new WaitForSeconds(disappearTime);

        // Hide the object
        gameObject.SetActive(false);

        // Wait before respawning
        yield return new WaitForSeconds(respawnDelay);

        // Reset position and enable object
        transform.position = respawnPosition;
        rb.constraints = RigidbodyConstraints2D.None;
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(true);
    }
}