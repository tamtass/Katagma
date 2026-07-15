using System.Collections;
using UnityEngine;

// Base class for all enemies. It handles the behaviour every enemy shares: spawning in
// with a scale-up, chasing the player, dealing contact damage, taking damage with a flash,
// knockback, the idle animation (frame flip + scale pulse), and dying (score, item drop,
// death effect). Specific enemies (teleporter, boss) inherit this and override what they need.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float health      = 50f;
    public float damage      = 10f;    // contact damage dealt to the player
    public float moveSpeed   = 3f;     // chase speed (subclasses set this to 0 to stay put)
    public float spawnWeight = 1f;     // relative likelihood/cost when a room picks enemies
    public int   scoreValue  = 10;     // points awarded on death

    [Header("Knockback")]
    public float knockbackForce    = 5f;
    public float knockbackDuration = 0.5f;   // also doubles as the contact-damage cooldown
    public bool  knockbackImmune   = false;  // if true the enemy is never pushed (used by bosses)

    [Header("Spawn")]
    public float spawnStunDuration = 0.6f;   // scale-in time; the enemy can't move or act during it

    [Header("Animation")]
    public Sprite[] frames;                          // two frames flipped between for the idle animation
    [Min(0.1f)] public float frameFrequency = 8f;    // frame flips per second
    [Min(0.1f)] public float scaleFrequency = 2f;    // how fast the size pulses
    [Range(0f, 0.5f)] public float scaleAmplitude = 0.08f;   // how much the size pulses

    [Header("Item Drop")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;      // chance to drop something on death
    public GameObject[] itemDropPool;     // possible drops, picked at random

    [Header("Death")]
    public GameObject deathEffectPrefab;  // e.g. the smoke puff, spawned when the enemy dies

    protected Rigidbody2D rb;
    protected Transform player;              // the player's transform, found on spawn
    protected SpriteRenderer spriteRenderer;

    private float knockbackTimer;            // while > 0, chase movement is suspended
    private float damageCooldown;            // prevents contact damage every frame of overlap
    private float spawnStunTimer;            // counts down the spawn stun
    private Coroutine damageFlashCoroutine;
    private Color originalColor;             // sprite colour to restore after the red hit flash

    private float frameTimer;                // accumulates toward the next frame flip
    private int   frameIndex;                // which of the two frames is showing
    private bool  spawnDone;                 // true once the spawn scale-in has finished

    protected bool suppressScalePulse;       // subclasses set this to pause the idle scale pulse
    protected bool suppressFrameAnimation;   // subclasses set this to hold a fixed sprite (e.g. a telegraph)

    private static readonly WaitForSeconds _flashDuration = new(0.05f);   // cached to avoid per-hit allocation

    // Grabs references, starts the spawn-in animation, and locates the player. Virtual so
    // subclasses can tweak setup (e.g. force moveSpeed to zero) around the base call.
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

    // Scales the enemy from nothing up to full size over the spawn-stun duration, so it
    // "grows" into existence. Sets spawnDone when finished, which unlocks the idle animation.
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

    // Runs the idle animation once spawned: a gentle size pulse (sine wave) and a two-frame
    // sprite flip. Either can be suppressed by a subclass (the boss holds a telegraph sprite,
    // the teleporter freezes the pulse mid-teleport). Virtual so subclasses can add to it.
    protected virtual void Update()
    {
        if (!spawnDone) return;

        if (!suppressScalePulse)
        {
            float s = 1f + scaleAmplitude * Mathf.Sin(Time.time * scaleFrequency * Mathf.PI * 2f);
            transform.localScale = Vector3.one * s;
        }

        if (!suppressFrameAnimation && frames != null && frames.Length >= 2 && spriteRenderer != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameFrequency)
            {
                frameTimer = 0f;
                frameIndex = 1 - frameIndex;   // toggle between frame 0 and 1
                spriteRenderer.sprite = frames[frameIndex];
            }
        }
    }

    // Physics step: tick down the timers, then chase the player at moveSpeed. Movement is
    // suspended while the spawn stun or a knockback is active. Virtual so subclasses can
    // replace the movement entirely.
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

    // While overlapping the player, deal contact damage (rate-limited by damageCooldown) and,
    // unless immune, bounce off them. The cooldown stops the player being drained every frame
    // of contact.
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (damageCooldown > 0f) return;

        if (other.TryGetComponent<PlayerMovement>(out var pm)) pm.TakeDamage(damage, transform.position);

        if (!knockbackImmune)
        {
            Vector2 bounceDir = (transform.position - other.transform.position).normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(bounceDir * knockbackForce, ForceMode2D.Impulse);
            knockbackTimer = knockbackDuration;
        }

        damageCooldown = knockbackDuration;
    }

    // Called by the player when they strike or bump the enemy, to knock it away. Ignored by
    // immune enemies. The knockback timer briefly stops the enemy re-chasing so the push reads.
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (knockbackImmune) return;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        knockbackTimer = knockbackDuration;
    }

    // Applies damage and plays the hurt sound (on every hit, including the fatal one). Dies
    // at zero HP, otherwise flashes red. Virtual so the boss can hook the phase transition.
    public virtual void TakeDamage(float amount)
    {
        health -= amount;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayEnemyHurt();

        if (health <= 0f) { Die(); return; }

        if (spriteRenderer != null)
        {
            if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = StartCoroutine(DamageFlash());
        }
    }

    // Flashes the sprite red briefly to signal a hit, then restores the original colour.
    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return _flashDuration;
        spriteRenderer.color = originalColor;
        damageFlashCoroutine = null;
    }

    // Handles death: spawn the death effect, award score, bump the kill counter, roll for an
    // item drop, then destroy the object. Virtual, though currently no subclass overrides it —
    // the boss relies on this same path so the room's clear/portal logic still fires.
    protected virtual void Die()
    {
        // Unparented so the effect outlives this object and finishes its own animation.
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

    // Rolls dropChance and, if it passes, spawns a random item from the pool into the room.
    private void TryDropItem()
    {
        if (itemDropPool == null || itemDropPool.Length == 0) return;
        if (Random.value > dropChance) return;

        GameObject prefab = itemDropPool[Random.Range(0, itemDropPool.Length)];
        if (prefab != null)
            Instantiate(prefab, transform.position, Quaternion.identity, transform.parent);
    }
}
