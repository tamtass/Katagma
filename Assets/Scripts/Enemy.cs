using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float health      = 50f;
    public float damage      = 10f;
    public float moveSpeed   = 3f;
    public float spawnWeight = 1f;
    public int   scoreValue  = 10;

    [Header("Knockback")]
    public float knockbackForce    = 5f;
    public float knockbackDuration = 0.5f;

    [Header("Spawn")]
    public float spawnStunDuration = 0.6f;

    [Header("Animation")]
    public Sprite[] frames;
    [Min(0.1f)] public float frameFrequency = 8f;   // frame flips per second
    [Min(0.1f)] public float scaleFrequency = 2f;   // scale pulse cycles per second
    [Range(0f, 0.5f)] public float scaleAmplitude = 0.08f;

    [Header("Item Drop")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;
    public GameObject[] itemDropPool;

    [Header("Death")]
    public GameObject deathEffectPrefab;   // e.g. a smoke puff, spawned when the enemy dies

    protected Rigidbody2D rb;
    protected Transform player;
    protected SpriteRenderer spriteRenderer;

    private float knockbackTimer;
    private float damageCooldown;
    private float spawnStunTimer;
    private Coroutine damageFlashCoroutine;
    private Color originalColor;

    private float frameTimer;
    private int   frameIndex;
    private bool  spawnDone;

    protected bool suppressScalePulse;
    protected bool suppressFrameAnimation;

    private static readonly WaitForSeconds _flashDuration = new(0.05f);

    protected virtual void Start()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        rb.freezeRotation = true;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        spawnStunTimer = spawnStunDuration;
        transform.localScale = Vector3.zero;
        StartCoroutine(SpawnScale());

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private IEnumerator SpawnScale()
    {
        float elapsed = 0f;
        while (elapsed < spawnStunDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / spawnStunDuration);
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;
        spawnDone = true;
    }

    protected virtual void Update()
    {
        if (!spawnDone) return;

        // Scale pulse
        if (!suppressScalePulse)
        {
            float s = 1f + scaleAmplitude * Mathf.Sin(Time.time * scaleFrequency * Mathf.PI * 2f);
            transform.localScale = Vector3.one * s;
        }

        // Frame flip
        if (!suppressFrameAnimation && frames != null && frames.Length >= 2 && spriteRenderer != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameFrequency)
            {
                frameTimer = 0f;
                frameIndex = 1 - frameIndex;
                spriteRenderer.sprite = frames[frameIndex];
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (player == null) return;

        spawnStunTimer -= Time.fixedDeltaTime;
        knockbackTimer -= Time.fixedDeltaTime;
        damageCooldown -= Time.fixedDeltaTime;
        if (spawnStunTimer > 0f || knockbackTimer > 0f) return;

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (damageCooldown > 0f) return;

        if (other.TryGetComponent<PlayerMovement>(out var pm)) pm.TakeDamage(damage, transform.position);

        Vector2 bounceDir = (transform.position - other.transform.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir * knockbackForce, ForceMode2D.Impulse);

        damageCooldown = knockbackDuration;
        knockbackTimer = knockbackDuration;
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        knockbackTimer = knockbackDuration;
    }

    public virtual void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) { Die(); return; }

        if (spriteRenderer != null)
        {
            if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = StartCoroutine(DamageFlash());
        }
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return _flashDuration;
        spriteRenderer.color = originalColor;
        damageFlashCoroutine = null;
    }

    protected virtual void Die()
    {
        // Spawned unparented so it outlives this GameObject and finishes on its own.
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
            GameManager.Instance.OnEnemyKilled();
        }
        TryDropItem();
        Destroy(gameObject);
    }

    void TryDropItem()
    {
        if (itemDropPool == null || itemDropPool.Length == 0) return;
        if (Random.value > dropChance) return;

        GameObject prefab = itemDropPool[Random.Range(0, itemDropPool.Length)];
        if (prefab != null)
            Instantiate(prefab, transform.position, Quaternion.identity, transform.parent);
    }
}
