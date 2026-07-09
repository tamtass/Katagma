using UnityEngine;

// A projectile fired by the player (via the Onion item's auto-shooter). Mirrors
// EnemyProjectile, but damages enemies instead of the player.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed    = 10f;
    public float lifetime = 3f;

    private float damage;
    private Vector2 direction;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
    }

    // scaleMultiplier sizes the projectile by the player's current damage, relative
    // to the prefab's authored scale (so the prefab controls the base visual size).
    public void Launch(Vector2 dir, float dmg, float scaleMultiplier)
    {
        direction             = dir.normalized;
        damage                = dmg;
        transform.localScale *= scaleMultiplier;
        Destroy(gameObject, lifetime);
    }

    // Set velocity every physics step so speed stays consistent regardless of
    // sleep state or drag (same approach as EnemyProjectile).
    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;                       // never hit the player
        if (other.GetComponent<PlayerProjectile>() != null) return;   // ignore sibling shots
        if (other.GetComponent<EnemyProjectile>() != null) return;    // pass through enemy fire

        if (other.TryGetComponent<Enemy>(out var enemy))
            enemy.TakeDamage(damage);

        Destroy(gameObject);   // consumed on hitting an enemy or a wall
    }
}
