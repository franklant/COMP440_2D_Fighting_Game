using UnityEngine;

public class PlayerFreeze : MonoBehaviour
{
    public float freezeDuration = 1.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("opponent"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            MonoBehaviour movementScript = collision.GetComponent<MonoBehaviour>(); // replace with actual movement script if needed

            if (rb != null)
                StartCoroutine(FreezePlayer(rb, movementScript));

            // Hide this falling object
            StartCoroutine(HideSelf());
        }
    }

    private System.Collections.IEnumerator FreezePlayer(Rigidbody2D rb, MonoBehaviour movementScript)
    {
        // Disable movement script if exists
        if (movementScript != null)
            movementScript.enabled = false;

        // Save velocity and stop movement
        Vector2 originalVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        // Optional: disable gravity to prevent sliding
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(freezeDuration);

        // Restore velocity and gravity
        rb.gravityScale = originalGravity;
        rb.linearVelocity = originalVelocity;

        if (movementScript != null)
            movementScript.enabled = true;
    }

    private System.Collections.IEnumerator HideSelf()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (sr) sr.enabled = false;
        if (col) col.enabled = false;
        if (rb) rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(2f);

        transform.position += Vector3.up * 6f;

        if (sr) sr.enabled = true;
        if (col) col.enabled = true;
    }
}