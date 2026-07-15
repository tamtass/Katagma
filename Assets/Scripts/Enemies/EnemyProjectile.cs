using UnityEngine;

// A projectile fired by enemies (the teleporter and the boss). Flies straight, damages the
// player on contact, and passes harmlessly through enemies and other enemy shots.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed   = 6f;
    public float lifetime = 4f;    // self-destructs after this long

    private float damage;
    private GameObject spawner;     // who fired it, so it never hits its own shooter
    private Vector2 direction;
    private Rigidbody2D rb;

    // Kill gravity and drag so it flies flat at a constant speed.
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
    }

    // Called by the enemy that fires it: dir is the aim, dmg the damage, src the shooter.
    public void Launch(Vector2 dir, float dmg, GameObject src = null)
    {
        direction = dir.normalized;
        damage    = dmg;
        spawner   = src;
        Destroy(gameObject, lifetime);
    }

    // Re-applies velocity each physics step so speed stays consistent no matter what.
    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    // On contact: hurt the player and disappear; pass through the shooter, other enemies,
    // and sibling shots. The sibling check matters because the boss fires whole rings from
    // one point, so the shots overlap at spawn and would otherwise destroy each other.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == spawner) return;
        if (other.GetComponent<Enemy>() != null) return;
        if (other.GetComponent<EnemyProjectile>() != null) return;

        if (other.CompareTag("Player") && other.TryGetComponent<PlayerMovement>(out var pm))
            pm.TakeDamage(damage, transform.position);

        Destroy(gameObject);
    }
}
