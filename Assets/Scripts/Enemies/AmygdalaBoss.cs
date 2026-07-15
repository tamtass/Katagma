using System.Collections;
using UnityEngine;

// The Amygdala ("The Fear"), the first floor's boss. A large, stationary boss that fights
// in two phases split by its health. It inherits from Enemy so that score, death, the boss
// health bar, and the room's "boss defeated -> open the exit" logic all run through the same
// path as a normal enemy — this class only adds the two-phase attack behaviour on top.
public class AmygdalaBoss : Enemy
{
    // The two fight phases; which one is active decides the attack pattern.
    private enum Phase { PhaseOne, PhaseTwo }

    [Header("Amygdala — Phase")]
    [Range(0f, 1f)] public float phaseTwoThreshold = 0.5f;   // HP fraction at which phase 2 begins

    [Header("Amygdala — Telegraphs")]
    public float  telegraphDuration      = 0.5f;   // how long the tell shows before each attack
    public Sprite telegraphSprite;                 // sprite shown during the pre-attack tell
    public float  panicTelegraphDuration = 1f;     // length of the one-off tell at the phase switch
    public Sprite panicSprite;                     // sprite for the panic tell (falls back to telegraphSprite)

    [Header("Amygdala — Projectile")]
    public GameObject projectilePrefab;
    public float projectileDamage = 10f;

    [Header("Amygdala — Radial Burst (Phase 1)")]
    public int   radialCount         = 12;   // projectiles per ring; the gaps between them are the safe spots
    public float radialBurstInterval = 3f;   // seconds between bursts
    public float radialAngleOffset   = 0f;   // rotates the whole ring pattern

    [Header("Amygdala — Spiral (Phase 2)")]
    public int   spiralArms          = 3;      // number of simultaneous spiral streams
    public float spiralFireInterval  = 0.12f;  // time between spiral shots
    public float spiralRotationSpeed = 90f;    // how fast the spiral arms sweep, in degrees per second

    private Phase phase;                    // current phase
    private float initialHealth;            // HP at spawn, used to work out the 50% threshold
    private bool  phaseTransitionStarted;   // guards the transition so it only happens once

    // Make it stationary and unpushable, freeze its physics position entirely, then start
    // the fight. base.Start still runs the normal spawn-in and player lookup.
    protected override void Start()
    {
        moveSpeed       = 0f;
        knockbackImmune = true;
        base.Start();

        // Also freeze the rigidbody so the raw physics of a player collision can't shove it.
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        initialHealth = health;
        phase         = Phase.PhaseOne;

        StartCoroutine(RunFight());
    }

    // Waits out the spawn-in, then kicks off the phase-1 attack loop. The phase-2 spiral is
    // started later by the transition, not here.
    private IEnumerator RunFight()
    {
        yield return new WaitForSeconds(spawnStunDuration);
        StartCoroutine(RadialBurstLoop());
    }

    // Overrides damage to watch for the phase switch. base.TakeDamage does the real work
    // (subtract HP, flash, die); afterwards, the first time HP crosses below the threshold,
    // it triggers the one-time transition. The guard flag stops it firing twice.
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);

        if (!phaseTransitionStarted && health > 0f && health <= initialHealth * phaseTwoThreshold)
        {
            phaseTransitionStarted = true;
            StartCoroutine(PhaseTransition());
        }
    }

    // Plays a longer "panic" tell to signal the escalation, then switches to phase 2: the
    // radial loop sees the phase change and stops itself, and the spiral loop takes over.
    private IEnumerator PhaseTransition()
    {
        yield return ShowTelegraph(panicSprite != null ? panicSprite : telegraphSprite, panicTelegraphDuration);

        phase = Phase.PhaseTwo;
        StartCoroutine(SpiralLoop());
    }

    // Phase 1 attack. On a timer, telegraphs then fires a full ring of evenly spaced shots.
    // The even spacing is what leaves gaps for the player to stand in. Loops until the phase
    // changes, at which point it exits and hands over to the spiral.
    private IEnumerator RadialBurstLoop()
    {
        while (phase == Phase.PhaseOne)
        {
            yield return new WaitForSeconds(radialBurstInterval);

            yield return ShowTelegraph(telegraphSprite, telegraphDuration);
            FireRing(radialCount, radialAngleOffset);
        }
    }

    // Phase 2 attack. Fires a few arms rapidly, nudging the base angle each shot so the arms
    // rotate — turning the ring's static gaps into moving ones that sweep around the room.
    private IEnumerator SpiralLoop()
    {
        float angle = 0f;
        while (phase == Phase.PhaseTwo)
        {
            FireRing(spiralArms, angle);
            angle += spiralRotationSpeed * spiralFireInterval;
            yield return new WaitForSeconds(spiralFireInterval);
        }
    }

    // Fires `count` projectiles evenly spread around a full circle, rotated by an offset.
    // Used by both the radial burst and the spiral, just with different counts and offsets.
    private void FireRing(int count, float angleOffsetDeg)
    {
        if (projectilePrefab == null || count <= 0) return;

        float step = 360f / count;
        for (int i = 0; i < count; i++)
            FireProjectile(angleOffsetDeg + i * step);
    }

    // Spawns a single projectile travelling at the given angle.
    private void FireProjectile(float angleDeg)
    {
        float rad   = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (proj.TryGetComponent<EnemyProjectile>(out var ep))
            ep.Launch(dir, projectileDamage, gameObject);
    }

    // Shows a telegraph sprite for a set time as the tell before an attack. It suppresses the
    // inherited two-frame animation so the sprite stays put instead of being flipped away,
    // then re-enables it, letting the normal animation resume.
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
