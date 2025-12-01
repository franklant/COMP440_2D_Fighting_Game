using UnityEngine;

public class PlayerFreeze : MonoBehaviour
{
    public float freezeDuration = 1.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Freeze either fighter
        if (collision.CompareTag("Player") || collision.CompareTag("Opponent"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();

            if (rb != null)
                StartCoroutine(Freeze(rb));
        }
    }

    private System.Collections.IEnumerator Freeze(Rigidbody2D rb)
    {
        // Save the original constraints
        RigidbodyConstraints2D originalConstraints = rb.constraints;

        // Freeze completely
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // Wait
        yield return new WaitForSeconds(freezeDuration);

        // Restore original constraints
        rb.constraints = originalConstraints;
    }
}