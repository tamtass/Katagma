using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float health      = 50f;
    public float damage      = 10f;
    public float moveSpeed   = 3f;
    public float spawnWeight = 1f;

    [Header("Knockback")]
    public float knockbackForce    = 5f;
    public float knockbackDuration = 0.5f;

    [Header("Item Drop")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;
    public GameObject[] itemDropPool;

    protected Rigidbody2D rb;
    protected Transform player;

    private float knockbackTimer;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected virtual void FixedUpdate()
    {
        if (player == null) return;

        Vector2 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        knockbackTimer -= Time.fixedDeltaTime;
        if (knockbackTimer > 0f) return;

        rb.linearVelocity = dir * moveSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (knockbackTimer > 0f) return;

        PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.TakeDamage(damage);

        Vector2 bounceDir = (transform.position - collision.transform.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir * knockbackForce, ForceMode2D.Impulse);
        knockbackTimer = knockbackDuration;
    }

    public virtual void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
            Die();
    }

    protected virtual void Die()
    {
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
