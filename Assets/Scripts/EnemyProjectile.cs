using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed   = 6f;
    public float lifetime = 4f;

    private float damage;
    private GameObject spawner;
    private Vector2 direction;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
    }

    public void Launch(Vector2 dir, float dmg, GameObject src = null)
    {
        direction = dir.normalized;
        damage    = dmg;
        spawner   = src;
        Destroy(gameObject, lifetime);
    }

    // Set velocity every physics step so speed is always consistent regardless
    // of sleep state, drag, or external forces.
    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == spawner) return;
        if (other.GetComponent<Enemy>() != null) return;
        if (other.GetComponent<EnemyProjectile>() != null) return;   // ring shots overlap at spawn; ignore each other

        if (other.CompareTag("Player") && other.TryGetComponent<PlayerMovement>(out var pm))
            pm.TakeDamage(damage, transform.position);

        Destroy(gameObject);
    }
}
