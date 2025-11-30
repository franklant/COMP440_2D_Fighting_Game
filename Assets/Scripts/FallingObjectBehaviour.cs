using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingObjectBehaviour : MonoBehaviour
{
    [Header("Initial Drop Stagger")]
    [Tooltip("Min random delay before object begins falling when play starts")]
    public float minStartDelay = 0f;

    [Tooltip("Max random delay before object begins falling when play starts")]
    public float maxStartDelay = 2f;

    [Header("Respawn settings")]
    public float disappearDelay = 2f;    
    public float respawnDelay = 3f;      
    public float respawnHeightOffset = 6f;
    public bool resetRotationOnRespawn = true;

    // internals
    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sprite;

    Vector3 initialSpawnPosition;
    float initialGravity;
    RigidbodyType2D initialBodyType;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        initialBodyType = rb.bodyType;
        initialGravity = rb.gravityScale;
        initialSpawnPosition = transform.position;

        // ✅ Freeze gravity at start so we can delay falling
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    void Start()
    {
        // ✅ Random delay before starting gravity (so all objects don’t drop together)
        float delay = Random.Range(minStartDelay, maxStartDelay);
        StartCoroutine(StartFallAfterDelay(delay));
    }

    IEnumerator StartFallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // restore gravity and let it fall
        rb.gravityScale = initialGravity;
        rb.bodyType = initialBodyType;
        rb.constraints = RigidbodyConstraints2D.None;
    }

    // Call this if you want to programmatically set where this object should respawn
    public void SetInitialSpawnPosition(Vector3 pos)
    {
        initialSpawnPosition = pos;
    }

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

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 respawnPos = initialSpawnPosition;
        if (respawnHeightOffset != 0f)
            respawnPos = new Vector3(
                initialSpawnPosition.x,
                initialSpawnPosition.y + Mathf.Abs(respawnHeightOffset),
                initialSpawnPosition.z
            );

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