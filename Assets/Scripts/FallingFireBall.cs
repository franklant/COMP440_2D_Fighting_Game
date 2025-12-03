using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingFireBall : MonoBehaviour
{
    [Header("Initial Drop Stagger")]
    public float minStartDelay = 0f;
    public float maxStartDelay = 2f;

    [Header("Respawn settings")]
    public float disappearDelay = 2f;
    public float respawnDelay = 3f;
    public float respawnHeightOffset = 6f;
    public bool resetRotationOnRespawn = true;

    // Internals
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sprite;

    private Vector3 initialSpawnPosition;
    private float initialGravity;
    private RigidbodyType2D initialBodyType;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        initialBodyType = rb.bodyType;
        initialGravity = rb.gravityScale;
        initialSpawnPosition = transform.position;

        // Stop falling at start for stagger
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    void Start()
    {
        float delay = Random.Range(minStartDelay, maxStartDelay);
        StartCoroutine(StartFallAfterDelay(delay));
    }

    IEnumerator StartFallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        rb.gravityScale = initialGravity;
        rb.bodyType = initialBodyType;
        rb.constraints = RigidbodyConstraints2D.None;
    }

    public void SetInitialSpawnPosition(Vector3 pos)
    {
        initialSpawnPosition = pos;
    }

    // ✅ FIX: Collision instead of Trigger
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

            StartCoroutine(HideThenRespawnRoutine());
        }
    }

    private IEnumerator HideThenRespawnRoutine()
    {
        yield return new WaitForSeconds(disappearDelay);

        if (sprite) sprite.enabled = false;
        if (col) col.enabled = false;

        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 respawnPos = initialSpawnPosition;

        if (respawnHeightOffset != 0f)
        {
            respawnPos.y += Mathf.Abs(respawnHeightOffset);
        }

        transform.position = respawnPos;

        if (resetRotationOnRespawn)
            transform.rotation = Quaternion.identity;

        if (sprite) sprite.enabled = true;
        if (col) col.enabled = true;

        rb.constraints = RigidbodyConstraints2D.None;
        rb.bodyType = initialBodyType;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = initialGravity;
    }
}