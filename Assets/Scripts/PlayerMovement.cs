using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float movementSpeed = 5f;
    public float attackSpeed = 2f;   // attacks per second
    public float damage = 10f;
    public float attackRange = 1.5f;
    public int projectileCount = 5;

    [Header("Effects")]
    public LightningConeEffect lightningCone;

    public bool canMove = true;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float attackCooldown;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        health = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        health = Mathf.Max(health - amount, 0f);
        if (health == 0f)
            Die();
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    void Die()
    {
        // TODO: handle player death
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

        FaceCursor();
        HandleAttack();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = canMove ? moveInput * movementSpeed : Vector2.zero;
    }

    void FaceCursor()
    {
        if (Mouse.current == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = mouseWorld - transform.position;

        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
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

    void Attack()
    {
        float coneHalfAngle = projectileCount * 9f / 2f;
        const float boltHalfAngle = 4.5f; // each bolt covers half of its 9-degree slice

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        for (int i = 0; i < projectileCount; i++)
        {
            float t     = projectileCount > 1 ? (float)i / (projectileCount - 1) : 0.5f;
            float angle = transform.eulerAngles.z + Mathf.Lerp(-coneHalfAngle, coneHalfAngle, t);
            float rad   = angle * Mathf.Deg2Rad;
            Vector2 boltDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                Vector2 dirToTarget = hit.transform.position - transform.position;
                if (Vector2.Angle(boltDir, dirToTarget) > boltHalfAngle) continue;

                // hit.GetComponent<IDamageable>()?.TakeDamage(damage);
            }
        }

        if (lightningCone != null)
        {
            lightningCone.coneAngle = projectileCount * 9f;
            lightningCone.Play(transform.position, transform.eulerAngles.z, attackRange, projectileCount);
        }
    }

    void OnDrawGizmosSelected()
    {
        float halfAngle = projectileCount * 9f / 2f;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f,  halfAngle) * transform.right * attackRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f, -halfAngle) * transform.right * attackRange);
    }
}
