using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// The player character: movement, aiming, the melee cone attack, taking damage with
// invulnerability frames, and the stat upgrades gained from clearing rooms. This is the
// biggest single script because it's the heart of the moment-to-moment gameplay.
public class PlayerMovement : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth     = 100f;
    public float health        = 100f;   // current HP, reset to maxHealth on spawn
    public float movementSpeed = 5f;
    public float attackSpeed   = 2f;     // attacks per second
    public float damage        = 10f;    // damage per bolt that connects
    public float attackRange   = 1.5f;   // radius of the melee cone
    public int   projectileCount = 5;    // number of lightning bolts; also widens the cone

    [Tooltip("Fraction a stat grows by on each room-clear upgrade (0.1 = 10%).")]
    public float upgradePercent = 0.1f;   // per-room-clear stat boost, tunable

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite frontSprite;   // one sprite per facing direction, chosen from the aim angle
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Effects")]
    public LightningConeEffect lightningCone;   // the visual for the attack

    [Header("Upgrade UI")]
    public GameObject upgradeCanvas;   // popup shown briefly when a stat is upgraded
    // One text label per stat, in this order: MaxHealth, MovementSpeed, AttackSpeed,
    // Damage, AttackRange, ProjectileCount. Indexed by the stat picked in OnRoomCleared.
    public TextMeshProUGUI[] upgradeTexts;

    [Header("Movement Feel")]
    public float acceleration = 50f;   // how quickly the player speeds up
    public float deceleration = 20f;   // how quickly they slow to a stop (lower = more sliding)

    [Header("Knockback & Invulnerability")]
    public float knockbackForce          = 8f;    // push on the player when hit
    public float enemyKnockbackForce     = 6f;    // push applied to enemies the player bumps
    public float invulnerabilityDuration = 1f;    // i-frames after taking a hit
    public float flickerFrequency        = 10f;   // sprite flicker rate during i-frames
    public float pickupFlickerFrequency  = 3f;    // slower flicker during an item pickup freeze

    public bool canMove = true;   // toggled off during transitions, pickups, death

    // The starting damage as authored on the prefab, captured before any upgrades. Used by
    // the Onion's auto-shooter to scale its projectile relative to the base value.
    public float BaseDamage { get; private set; }

    private Rigidbody2D rb;
    private Vector2 moveInput;              // current WASD/arrow direction, normalized
    private float attackCooldown;           // counts down; attack allowed when <= 0
    private float facingAngle = 0f;         // aim angle in degrees, from the cursor
    private float invulnerabilityTimer;     // remaining i-frame time
    private float knockbackTimer;           // brief window where movement input is ignored after a hit
    private bool  _pickupFrozen;            // true during the pickup animation freeze

    // Grab references, snap HP to full, and remember the base damage. Rotation is frozen
    // because the sprite faces via swapping images, not by rotating the transform.
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (lightningCone == null)
            lightningCone = GetComponentInChildren<LightningConeEffect>();

        health = maxHealth;
        BaseDamage = damage;
        transform.rotation = Quaternion.identity;
    }

    // Called by enemies/projectiles when they hit the player. Ignored during i-frames.
    // Applies damage, screen shake, a hurt sound, and a knockback push away from the source,
    // then dies if HP hit zero. sourcePosition is used to work out the knockback direction.
    public void TakeDamage(float amount, Vector2 sourcePosition)
    {
        if (invulnerabilityTimer > 0f) return;

        health = Mathf.Max(health - amount, 0f);

        if (CameraShake.Instance != null) CameraShake.Instance.Shake();
        if (health > 0f && SoundManager.Instance != null) SoundManager.Instance.PlayPlayerHurt();

        Vector2 knockDir = ((Vector2)transform.position - sourcePosition).normalized;
        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        knockbackTimer       = 0.15f;
        invulnerabilityTimer = invulnerabilityDuration;

        if (health == 0f)
            Die();
    }

    // Restore HP, clamped so it never exceeds the maximum.
    public void Heal(float amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    // Raising the max also heals by the same amount, so the new headroom is filled.
    public void UpgradeMaxHealth(float amount)
    {
        maxHealth += amount;
        Heal(amount);
    }

    // One-line stat bumps, called by the item scripts and by the room-clear upgrade.
    public void UpgradeMovementSpeed(float amount)   => movementSpeed   += amount;
    public void UpgradeAttackSpeed(float amount)     => attackSpeed     += amount;
    public void UpgradeDamage(float amount)          => damage          += amount;
    public void UpgradeAttackRange(float amount)     => attackRange     += amount;
    public void UpgradeProjectileCount(int amount)   => projectileCount += amount;

    // Called by RoomController when a combat room is cleared. Picks one of five stats at
    // random and boosts it by upgradePercent (projectile count is a flat +1 since it's an
    // integer), then flashes which stat went up. This is the core progression loop of a run.
    public void OnRoomCleared()
    {
        int stat = Random.Range(0, 5);
        switch (stat)
        {
            case 0: UpgradeMovementSpeed(movementSpeed * upgradePercent);   break;
            case 1: UpgradeAttackSpeed(attackSpeed     * upgradePercent);   break;
            case 2: UpgradeDamage(damage               * upgradePercent);   break;
            case 3: UpgradeAttackRange(attackRange     * upgradePercent);   break;
            case 4: UpgradeProjectileCount(1);                              break;
        }

        if (upgradeCanvas != null && upgradeTexts != null && stat < upgradeTexts.Length)
            StartCoroutine(FlashUpgrade(stat));
    }

    // Cached waits so the flash coroutine doesn't allocate garbage each time it runs.
    private static readonly WaitForSeconds _waitShow  = new(0.6f);
    private static readonly WaitForSeconds _waitOff   = new(0.08f);
    private static readonly WaitForSeconds _waitOn    = new(0.08f);

    // Shows the upgrade popup: reveal only the relevant stat's label, hold it, then blink
    // it a few times before hiding the whole canvas.
    private IEnumerator FlashUpgrade(int index)
    {
        if (upgradeTexts[index] == null) yield break;

        upgradeCanvas.SetActive(true);

        foreach (var t in upgradeTexts)
            if (t != null) t.gameObject.SetActive(false);

        TextMeshProUGUI text = upgradeTexts[index];
        text.gameObject.SetActive(true);
        text.alpha = 1f;

        yield return _waitShow;

        for (int i = 0; i < 4; i++)
        {
            text.alpha = 0f;
            yield return _waitOff;
            text.alpha = 1f;
            yield return _waitOn;
        }

        upgradeCanvas.SetActive(false);
    }

    // Death: stop movement, freeze the game, play the death sound, then hand off to the
    // transition screen which fades to black and shows the game-over stats.
    void Die()
    {
        canMove = false;
        rb.linearVelocity = Vector2.zero;
        Time.timeScale = 0f;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayPlayerDeath();
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.ShowDeath(
                onBlack: () => { if (GameManager.Instance != null) GameManager.Instance.ShowGameOver(false); });
        else if (GameManager.Instance != null)
            GameManager.Instance.ShowGameOver(false);
    }

    // Reads movement input every frame and, unless paused, updates facing, attacking, and
    // the invulnerability flicker. Movement itself is applied in FixedUpdate (physics).
    void Update()
    {
        moveInput = Keyboard.current != null
            ? new Vector2(
                (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0),
                (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1 : 0))
            : Vector2.zero;

        moveInput = moveInput.normalized;   // so diagonals aren't faster than straight lines

        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (!paused)
        {
            FaceCursor();
            HandleAttack();
        }
        HandleInvulnerability();
    }

    // Physics-step movement. Skips input for a brief moment after a hit so the knockback
    // isn't immediately cancelled. Otherwise eases the velocity toward the target using
    // acceleration when moving and deceleration when stopping, giving weighty movement.
    void FixedUpdate()
    {
        knockbackTimer -= Time.fixedDeltaTime;
        if (knockbackTimer > 0f) return;

        Vector2 target = canMove ? moveInput * movementSpeed : Vector2.zero;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, rate * Time.fixedDeltaTime);
    }

    // Points the player at the mouse. Rather than rotating, it picks the dominant axis
    // (left/right vs up/down) and swaps to the matching sprite, and stores the aim angle
    // used by the attack. This keeps the pixel art upright.
    void FaceCursor()
    {
        if (Mouse.current == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = mouseWorld - transform.position;

        if (dir.sqrMagnitude < 0.001f) return;   // cursor on top of player, keep current facing

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            facingAngle = dir.x >= 0 ? 0f : 180f;
            if (spriteRenderer != null)
                spriteRenderer.sprite = dir.x >= 0 ? rightSprite : leftSprite;
        }
        else
        {
            facingAngle = dir.y >= 0 ? 90f : -90f;
            if (spriteRenderer != null)
                spriteRenderer.sprite = dir.y >= 0 ? backSprite : frontSprite;
        }
    }

    // Fires the attack when the left mouse button is held and the cooldown is up. The
    // cooldown is the reciprocal of attack speed, so upgrading attack speed fires faster.
    void HandleAttack()
    {
        attackCooldown -= Time.deltaTime;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && attackCooldown <= 0f)
        {
            Attack();
            attackCooldown = 1f / attackSpeed;
        }
    }

    // Public entry point for the item pickup freeze (used by the Item base class).
    public void FreezeForPickup(float duration) => StartCoroutine(PickupFreezeCoroutine(duration));

    // Freezes the player for the pickup animation and flickers the sprite black/white so
    // it's clear they can't act, then restores control.
    private IEnumerator PickupFreezeCoroutine(float duration)
    {
        _pickupFrozen = true;
        canMove = false;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bool white = Mathf.FloorToInt(elapsed * pickupFlickerFrequency) % 2 == 0;
            if (spriteRenderer != null)
                spriteRenderer.color = white ? Color.white : Color.black;
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        canMove = true;
        _pickupFrozen = false;
    }

    // Counts down the invulnerability timer and flickers the sprite's alpha while it's
    // active. Skipped during a pickup freeze so the two flicker effects don't fight.
    void HandleInvulnerability()
    {
        if (_pickupFrozen) return;
        if (invulnerabilityTimer <= 0f) return;

        invulnerabilityTimer -= Time.deltaTime;

        if (invulnerabilityTimer <= 0f)
        {
            invulnerabilityTimer = 0f;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            return;
        }

        if (spriteRenderer != null)
        {
            // Toggle between dim and full alpha at the flicker frequency.
            bool dim = Mathf.FloorToInt(invulnerabilityTimer * flickerFrequency) % 2 == 0;
            spriteRenderer.color = new Color(1f, 1f, 1f, dim ? 0.2f : 1f);
        }
    }

    // The melee cone attack. Finds enemies within range, keeps only those inside the aim
    // cone, and for each one counts how many of the individual bolts would pass through it
    // (a wider spread means more bolts can hit the same target). Damage scales with the
    // number of bolts that connect. Landing a hit triggers a screen freeze for punch, and
    // the lightning visual is played over the cone.
    void Attack()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayPlayerAttack();

        float coneHalfAngle = projectileCount * 9f / 2f;
        Vector2 facingDir = new(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        var enemyHits = new Dictionary<Enemy, int>();   // enemy -> number of bolts that hit it

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.TryGetComponent<Enemy>(out var enemy)) continue;
            if (enemyHits.ContainsKey(enemy)) continue;   // already processed this enemy

            Vector2 dirToTarget = (Vector2)hit.transform.position - (Vector2)transform.position;

            // Approximate the enemy as a circle of this radius (from its collider bounds).
            float dist        = dirToTarget.magnitude;
            float enemyRadius = Mathf.Max(hit.bounds.extents.x, hit.bounds.extents.y);

            // Cone gate, widened by the enemy's angular size so an enemy that's only partly
            // inside the cone still counts. Without this, a large enemy whose centre sits just
            // outside the cone edge would be rejected even though its body clearly overlaps.
            float angularRadius = dist > 0.01f ? Mathf.Asin(Mathf.Clamp01(enemyRadius / dist)) * Mathf.Rad2Deg : 90f;
            if (Vector2.Angle(facingDir, dirToTarget) - angularRadius > coneHalfAngle) continue;

            // Treat each bolt as a ray from the player. The 2D cross product |P x D| gives the
            // perpendicular distance from the enemy's centre to that ray; if it's within the
            // enemy's radius, that bolt counts as a hit.

            int boltHits = 0;
            for (int i = 0; i < projectileCount; i++)
            {
                float t       = projectileCount > 1 ? (float)i / (projectileCount - 1) : 0.5f;
                float boltRad = (facingAngle + Mathf.Lerp(-coneHalfAngle, coneHalfAngle, t)) * Mathf.Deg2Rad;
                Vector2 boltDir = new(Mathf.Cos(boltRad), Mathf.Sin(boltRad));
                float perpDist = Mathf.Abs(dirToTarget.x * boltDir.y - dirToTarget.y * boltDir.x);
                if (perpDist <= enemyRadius)
                    boltHits++;
            }

            enemyHits[enemy] = Mathf.Max(boltHits, 1);   // at least 1 if it was in the cone
        }

        foreach (var kvp in enemyHits)
            kvp.Key.TakeDamage(damage * kvp.Value);

        // Freeze-frame only when the swing actually connects, so whiffs don't stutter.
        if (enemyHits.Count > 0 && HitStop.Instance != null) HitStop.Instance.Freeze();

        if (lightningCone != null)
        {
            lightningCone.coneAngle = projectileCount * 9f;
            lightningCone.Play(transform.position, facingAngle, attackRange, projectileCount);
        }
    }

    // Physically bumping into an enemy shoves that enemy away (the player's own knockback
    // from the hit is handled in TakeDamage).
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<Enemy>(out var enemy)) return;
        Vector2 dir = (collision.transform.position - transform.position).normalized;
        enemy.ApplyKnockback(dir, enemyKnockbackForce);
    }

    // Editor-only: draws the two edges of the attack cone when the player is selected, to
    // make tuning the range and spread easier.
    void OnDrawGizmosSelected()
    {
        float halfAngle = projectileCount * 9f / 2f;
        Vector3 facing  = new(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
        Gizmos.color    = Color.red;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f,  halfAngle) * facing * attackRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f, -halfAngle) * facing * attackRange);
    }
}
