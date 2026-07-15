using UnityEngine;

// The passive ability the Onion item grants. Once attached to the player it automatically
// fires a projectile at the nearest enemy, on a timer of half the player's attack speed.
// Kept as a separate component (added on pickup) so the ability doesn't exist at all until
// the item is collected.
[RequireComponent(typeof(PlayerMovement))]
public class AutoShooter : MonoBehaviour
{
    public GameObject projectilePrefab;           // the PlayerProjectile to spawn
    public float attackSpeedMultiplier = 0.5f;    // 0.5 = fire at half the player's attack rate

    private PlayerMovement player;
    private float cooldown;                        // time left until the next shot

    // Cache the player it's attached to.
    void Awake() => player = GetComponent<PlayerMovement>();

    // Called by the Onion item when picked up, to hand over the projectile prefab.
    public void Configure(GameObject prefab) => projectilePrefab = prefab;

    // Counts the cooldown down and, when ready, fires at the closest enemy. If there are
    // no enemies it does nothing and keeps the cooldown at zero, so it fires the instant
    // one appears. Paused while the game is paused.
    void Update()
    {
        if (projectilePrefab == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = FindClosestEnemy();
        if (target == null) return;

        Fire(target);
        // Half attack speed means double the interval between shots.
        cooldown = 1f / (player.attackSpeed * attackSpeedMultiplier);
    }

    // Spawns a projectile aimed at the target. The size multiplier is the ratio of current
    // damage to the player's starting damage, so the shot visibly grows as damage upgrades.
    private void Fire(Enemy target)
    {
        Vector2 dir      = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float   sizeMult = player.BaseDamage > 0f ? player.damage / player.BaseDamage : 1f;

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (proj.TryGetComponent<PlayerProjectile>(out var pp))
            pp.Launch(dir, player.damage, sizeMult);
    }

    // Returns the nearest enemy currently in the scene, or null if there are none. Uses
    // squared distance to avoid the square root, since we only care about ordering. Because
    // only the active room's enemies exist, this is effectively "nearest enemy in the room".
    private Enemy FindClosestEnemy()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy closest = null;
        float best = float.MaxValue;

        foreach (var e in enemies)
        {
            float d = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < best) { best = d; closest = e; }
        }
        return closest;
    }
}
