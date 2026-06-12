using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth     = 100f;
    public float health        = 100f;
    public float movementSpeed = 5f;
    public float attackSpeed   = 2f;
    public float damage        = 10f;
    public float attackRange   = 1.5f;
    public int   projectileCount = 5;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Effects")]
    public LightningConeEffect lightningCone;

    [Header("Upgrade UI")]
    public GameObject upgradeCanvas;
    // One text per stat: MaxHealth, MovementSpeed, AttackSpeed, Damage, AttackRange, ProjectileCount
    public TextMeshProUGUI[] upgradeTexts;

    [Header("Movement Feel")]
    public float acceleration = 50f;
    public float deceleration = 20f;

    [Header("Knockback & Invulnerability")]
    public float knockbackForce          = 8f;
    public float enemyKnockbackForce     = 6f;
    public float invulnerabilityDuration = 1f;
    public float flickerFrequency        = 10f;

    public bool canMove = true;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float attackCooldown;
    private float facingAngle = 0f;
    private float invulnerabilityTimer;
    private float knockbackTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (lightningCone == null)
            lightningCone = GetComponentInChildren<LightningConeEffect>();

        health = maxHealth;
        transform.rotation = Quaternion.identity;
    }

    public void TakeDamage(float amount, Vector2 sourcePosition)
    {
        if (invulnerabilityTimer > 0f) return;

        health = Mathf.Max(health - amount, 0f);

        Vector2 knockDir = ((Vector2)transform.position - sourcePosition).normalized;
        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        knockbackTimer       = 0.15f;
        invulnerabilityTimer = invulnerabilityDuration;

        if (health == 0f)
            Die();
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    public void UpgradeMaxHealth(float amount)
    {
        maxHealth += amount;
        Heal(amount);
    }

    public void UpgradeMovementSpeed(float amount)   => movementSpeed   += amount;
    public void UpgradeAttackSpeed(float amount)     => attackSpeed     += amount;
    public void UpgradeDamage(float amount)          => damage          += amount;
    public void UpgradeAttackRange(float amount)     => attackRange     += amount;
    public void UpgradeProjectileCount(int amount)   => projectileCount += amount;

    public void OnRoomCleared()
    {
        int stat = Random.Range(0, 5);
        switch (stat)
        {
            case 0: UpgradeMovementSpeed(movementSpeed * 0.1f);     break;
            case 1: UpgradeAttackSpeed(attackSpeed   * 0.1f);       break;
            case 2: UpgradeDamage(damage             * 0.1f);       break;
            case 3: UpgradeAttackRange(attackRange   * 0.1f);       break;
            case 4: UpgradeProjectileCount(1);                      break;
        }

        if (upgradeCanvas != null && upgradeTexts != null && stat < upgradeTexts.Length)
            StartCoroutine(FlashUpgrade(stat));
    }

    private static readonly WaitForSeconds _waitShow  = new(0.6f);
    private static readonly WaitForSeconds _waitOff   = new(0.08f);
    private static readonly WaitForSeconds _waitOn    = new(0.08f);

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

    void Die()
    {
        canMove = false;
        rb.linearVelocity = Vector2.zero;
        Time.timeScale = 0f;
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied();
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.ShowDeath();
    }

    void Update()
    {
        moveInput = Keyboard.current != null
            ? new Vector2(
                (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0),
                (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1 : 0))
            : Vector2.zero;

        moveInput = moveInput.normalized;

        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (!paused)
        {
            FaceCursor();
            HandleAttack();
        }
        HandleInvulnerability();
    }

    void FixedUpdate()
    {
        knockbackTimer -= Time.fixedDeltaTime;
        if (knockbackTimer > 0f) return;

        Vector2 target = canMove ? moveInput * movementSpeed : Vector2.zero;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, rate * Time.fixedDeltaTime);
    }

    void FaceCursor()
    {
        if (Mouse.current == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = mouseWorld - transform.position;

        if (dir.sqrMagnitude < 0.001f) return;

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

    void HandleAttack()
    {
        attackCooldown -= Time.deltaTime;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && attackCooldown <= 0f)
        {
            Attack();
            attackCooldown = 1f / attackSpeed;
        }
    }

    void HandleInvulnerability()
    {
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
            bool dim = Mathf.FloorToInt(invulnerabilityTimer * flickerFrequency) % 2 == 0;
            spriteRenderer.color = new Color(1f, 1f, 1f, dim ? 0.2f : 1f);
        }
    }

    void Attack()
    {
        float coneHalfAngle = projectileCount * 9f / 2f;
        Vector2 facingDir = new(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        var enemyHits = new Dictionary<Enemy, int>();

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.TryGetComponent<Enemy>(out var enemy)) continue;
            if (enemyHits.ContainsKey(enemy)) continue;

            Vector2 dirToTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            if (Vector2.Angle(facingDir, dirToTarget) > coneHalfAngle) continue;

            // Treat each bolt as a ray; count how many rays pass through the enemy's collider.
            // 2D cross product |P × D| = perpendicular distance from enemy center to the ray.
            float enemyRadius = Mathf.Max(hit.bounds.extents.x, hit.bounds.extents.y);

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

            enemyHits[enemy] = Mathf.Max(boltHits, 1);
        }

        foreach (var kvp in enemyHits)
            kvp.Key.TakeDamage(damage * kvp.Value);

        if (lightningCone != null)
        {
            lightningCone.coneAngle = projectileCount * 9f;
            lightningCone.Play(transform.position, facingAngle, attackRange, projectileCount);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<Enemy>(out var enemy)) return;
        Vector2 dir = (collision.transform.position - transform.position).normalized;
        enemy.ApplyKnockback(dir, enemyKnockbackForce);
    }

    void OnDrawGizmosSelected()
    {
        float halfAngle = projectileCount * 9f / 2f;
        Vector3 facing  = new(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
        Gizmos.color    = Color.red;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f,  halfAngle) * facing * attackRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f, -halfAngle) * facing * attackRange);
    }
}
