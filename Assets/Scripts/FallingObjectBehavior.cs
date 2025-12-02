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

    [Header("Freeze settings")]
    public float freezeDuration = 1.5f;

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

        // Freeze gravity at start so we can stagger falling
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Handle hitting the ground
        if (collision.CompareTag("Ground"))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

            StartCoroutine(HideThenRespawnRoutine());
        }

        // Handle freezing the player
        if (collision.CompareTag("Player") || collision.CompareTag("opponent"))
        {
            Rigidbody2D targetRb = collision.GetComponent<Rigidbody2D>();
            if (targetRb != null)
                StartCoroutine(FreezePlayerRoutine(targetRb));

            // Also hide/resapwn the falling object immediately
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

    private IEnumerator FreezePlayerRoutine(Rigidbody2D targetRb)
    {
        RigidbodyConstraints2D originalConstraints = targetRb.constraints;

        targetRb.linearVelocity = Vector2.zero;
        targetRb.constraints = RigidbodyConstraints2D.FreezeAll;

        yield return new WaitForSeconds(freezeDuration);

        targetRb.constraints = originalConstraints;
    }
}