using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingFireBall : MonoBehaviour
{
    [Header("Initial Drop Stagger")]
    public float minStartDelay = 0f;
    public float maxStartDelay = 2f;

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;
    public float respawnHeightOffset = 6f;
    public bool resetRotationOnRespawn = true;

    [Header("Fire Impact Animation")]
    public Sprite[] impactFrames;   // <<< Drag sliced fire sprites here
    public float frameRate = 0.05f;

    // Internals
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sprite;

    private Vector3 initialSpawnPosition;
    private float initialGravity;
    private RigidbodyType2D initialBodyType;

    private bool playingImpact;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        initialBodyType = rb.bodyType;
        initialGravity = rb.gravityScale;
        initialSpawnPosition = transform.position;

        // Stop falling at start for staggered drop
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
        ResetPhysics();
    }

    public void SetInitialSpawnPosition(Vector3 pos)
    {
        initialSpawnPosition = pos;
    }

    // ✅ Collide with Ground
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Ground")) return;
        if (playingImpact) return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        StartCoroutine(PlayImpactThenRespawn());
    }

    // ✅ Play fire animation before hiding + respawn
    IEnumerator PlayImpactThenRespawn()
    {
        playingImpact = true;

        // Disable collider during animation
        col.enabled = false;

        // Play fire animation
        if (impactFrames.Length > 0)
        {
            for (int i = 0; i < impactFrames.Length; i++)
            {
                sprite.sprite = impactFrames[i];
                yield return new WaitForSeconds(frameRate);
            }
        }

        // Hide sprite after animation
        sprite.enabled = false;

        // Wait before respawn
        yield return new WaitForSeconds(respawnDelay);

        Respawn();
    }

    // ✅ Reset fireball into starting state
    void Respawn()
    {
        Vector3 respawnPos = initialSpawnPosition;
        respawnPos.y += Mathf.Abs(respawnHeightOffset);
        transform.position = respawnPos;

        if (resetRotationOnRespawn)
            transform.rotation = Quaternion.identity;

        sprite.enabled = true;
        col.enabled = true;

        playingImpact = false;

        ResetPhysics();

        // Reset sprite to first frame
        if (impactFrames.Length > 0)
            sprite.sprite = impactFrames[0];
    }

    // ✅ Restore physics so the fireball falls again
    void ResetPhysics()
    {
        rb.constraints = RigidbodyConstraints2D.None;
        rb.gravityScale = initialGravity;
        rb.bodyType = initialBodyType;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}