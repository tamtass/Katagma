using UnityEngine;

// A projectile fired by the player, specifically by the Onion item's auto-shooter. It
// flies in a straight line and damages the first enemy it touches. It mirrors the enemy
// projectile, but the roles are swapped: this one hurts enemies, not the player.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed    = 10f;   // travel speed in units per second
    public float lifetime = 3f;    // auto-destroys after this long so stray shots don't pile up

    private float damage;          // set by the shooter at launch
    private Vector2 direction;     // travel direction, normalized
    private Rigidbody2D rb;

    // Zero out gravity and drag so the shot travels flat and at a constant speed.
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
    }

    // Fired by the auto-shooter. dir is the aim direction, dmg the damage to deal, and
    // scaleMultiplier resizes the projectile relative to the prefab's own scale, so the
    // prefab controls the base visual size and this just scales it by the damage ratio.
    public void Launch(Vector2 dir, float dmg, float scaleMultiplier)
    {
        direction             = dir.normalized;
        damage                = dmg;
        transform.localScale *= scaleMultiplier;
        Destroy(gameObject, lifetime);
    }

    // Re-applies the velocity every physics step so the speed is always consistent,
    // regardless of sleep state or drag.
    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    // On contact: damage an enemy if that's what we hit, then destroy. Ignores the player,
    // other player shots, and incoming enemy fire, so it only reacts to enemies and walls.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        if (other.GetComponent<PlayerProjectile>() != null) return;
        if (other.GetComponent<EnemyProjectile>() != null) return;

        if (other.TryGetComponent<Enemy>(out var enemy))
            enemy.TakeDamage(damage);

        Destroy(gameObject);   // consumed on hitting an enemy or a wall
    }
}
