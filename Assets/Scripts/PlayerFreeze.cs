using UnityEngine;

public class PlayerFreeze : MonoBehaviour
{
    public float freezeDuration = 1.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ✅ UPDATED TAGS
        if (collision.CompareTag("Player1") || collision.CompareTag("Player2"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            MonoBehaviour movementScript = collision.GetComponent<MonoBehaviour>();

            if (rb != null)
                StartCoroutine(FreezePlayer(rb, movementScript));

            StartCoroutine(HideSelf());
        }
    }

    private System.Collections.IEnumerator FreezePlayer(Rigidbody2D rb, MonoBehaviour movementScript)
    {
        if (movementScript != null)
            movementScript.enabled = false;

        Vector2 originalVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        rb.gameObject.GetComponent<SpriteRenderer>().color = Color.aliceBlue;

        yield return new WaitForSeconds(freezeDuration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = originalVelocity;

        rb.gameObject.GetComponent<SpriteRenderer>().color = Color.white;

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