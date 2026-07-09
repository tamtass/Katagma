using System.Collections;
using UnityEngine;

// The Amygdala ("The Fear") — first floor boss.
// A large, near-stationary boss that fights in two health-gated phases.
// Derives from Enemy so score attribution, death handling, the boss health bar,
// and the room-controller clear/portal sequence all run through the existing path.
public class AmygdalaBoss : Enemy
{
    private enum Phase { PhaseOne, PhaseTwo }

    [Header("Amygdala — Phase")]
    [Range(0f, 1f)] public float phaseTwoThreshold = 0.5f;   // HP fraction that triggers phase 2

    [Header("Amygdala — Telegraphs")]
    public float  telegraphDuration      = 0.5f;             // how long the tell is shown before each attack
    public Sprite telegraphSprite;                           // shown during the pre-attack tell
    public float  panicTelegraphDuration = 1f;               // one-off tell on the phase transition
    public Sprite panicSprite;                               // shown during the panic tell (falls back to telegraphSprite)

    [Header("Amygdala — Projectile")]
    public GameObject projectilePrefab;
    public float projectileDamage = 10f;

    [Header("Amygdala — Radial Burst (Phase 1)")]
    public int   radialCount         = 12;                   // projectiles per ring (evenly spaced gaps between them)
    public float radialBurstInterval = 3f;                   // seconds between bursts
    public float radialAngleOffset   = 0f;                   // rotates the whole rosette

    [Header("Amygdala — Spiral (Phase 2)")]
    public int   spiralArms          = 3;                    // simultaneous streams
    public float spiralFireInterval  = 0.12f;               // time between spiral shots
    public float spiralRotationSpeed = 90f;                 // degrees the arms rotate per second

    private Phase phase;
    private float initialHealth;
    private bool  phaseTransitionStarted;

    protected override void Start()
    {
        moveSpeed = 0f;             // stationary boss — does not chase (base FixedUpdate yields zero velocity)
        base.Start();

        initialHealth = health;
        phase         = Phase.PhaseOne;

        StartCoroutine(RunFight());
    }

    // Starts the radial burst loop once the spawn telegraph has finished.
    private IEnumerator RunFight()
    {
        yield return new WaitForSeconds(spawnStunDuration);
        StartCoroutine(RadialBurstLoop());
        // The spiral loop is started later, by the phase transition.
    }

    // ── Phase transition ──────────────────────────────────────────────────────
    // Fires once, the first time HP drops below the threshold. base.TakeDamage
    // handles the actual health subtraction, damage flash, and death.
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);

        if (!phaseTransitionStarted && health > 0f && health <= initialHealth * phaseTwoThreshold)
        {
            phaseTransitionStarted = true;
            StartCoroutine(PhaseTransition());
        }
    }

    private IEnumerator PhaseTransition()
    {
        // Brief "panic" tell before phase 2 ramps up. Uses the panic sprite if set,
        // otherwise falls back to the regular telegraph sprite.
        yield return ShowTelegraph(panicSprite != null ? panicSprite : telegraphSprite, panicTelegraphDuration);

        phase = Phase.PhaseTwo;          // radial bursts stop, spiral takes over
        StartCoroutine(SpiralLoop());
    }

    // ── Attack: radial burst (phase 1 only) ───────────────────────────────────
    // A full ring of evenly spaced projectiles. The gaps between them are the
    // safe spots the player stands in. Phase 2 replaces this with the spiral.
    private IEnumerator RadialBurstLoop()
    {
        while (phase == Phase.PhaseOne)
        {
            yield return new WaitForSeconds(radialBurstInterval);

            yield return ShowTelegraph(telegraphSprite, telegraphDuration);
            FireRing(radialCount, radialAngleOffset);
        }
    }

    // ── Attack: rotating spiral (phase 2 only) ────────────────────────────────
    // A handful of arms fired on a short interval, with the base angle rotating
    // each tick so the safe gaps sweep around the room.
    private IEnumerator SpiralLoop()
    {
        float angle = 0f;
        while (phase == Phase.PhaseTwo)
        {
            FireRing(spiralArms, angle);
            angle += spiralRotationSpeed * spiralFireInterval;   // advance the arms
            yield return new WaitForSeconds(spiralFireInterval);
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────
    private void FireRing(int count, float angleOffsetDeg)
    {
        if (projectilePrefab == null || count <= 0) return;

        float step = 360f / count;
        for (int i = 0; i < count; i++)
            FireProjectile(angleOffsetDeg + i * step);
    }

    private void FireProjectile(float angleDeg)
    {
        float rad   = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (proj.TryGetComponent<EnemyProjectile>(out var ep))
            ep.Launch(dir, projectileDamage, gameObject);
    }

    // The tell before each attack: swap in a dedicated telegraph sprite and hold it,
    // suppressing the base two-frame animation so it isn't overwritten. When the tell
    // ends, re-enabling the animation lets the normal frame flipping resume.
    private IEnumerator ShowTelegraph(Sprite sprite, float duration)
    {
        if (spriteRenderer != null && sprite != null)
        {
            suppressFrameAnimation = true;
            spriteRenderer.sprite  = sprite;
        }

        yield return new WaitForSeconds(duration);

        suppressFrameAnimation = false;
    }
}
