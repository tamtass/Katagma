using UnityEngine;

// Passive ability granted by the Onion item. Automatically fires a PlayerProjectile
// at the nearest enemy in the room at half the player's attack speed. Each shot is
// scaled by the player's current damage, so the projectile grows/shrinks as damage
// changes.
[RequireComponent(typeof(PlayerMovement))]
public class AutoShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float attackSpeedMultiplier = 0.5f;   // fires at half the player's attack speed

    private PlayerMovement player;
    private float cooldown;

    void Awake() => player = GetComponent<PlayerMovement>();

    // Called by the Onion item on pickup.
    public void Configure(GameObject prefab) => projectilePrefab = prefab;

    void Update()
    {
        if (projectilePrefab == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = FindClosestEnemy();
        if (target == null) return;   // no enemies in the room — hold fire, stay ready

        Fire(target);
        // Half attack speed → double the interval between shots.
        cooldown = 1f / (player.attackSpeed * attackSpeedMultiplier);
    }

    private void Fire(Enemy target)
    {
        Vector2 dir      = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        // 1x at the player's authored (prefab) damage, scaling up/down as damage changes.
        float   sizeMult = player.BaseDamage > 0f ? player.damage / player.BaseDamage : 1f;

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (proj.TryGetComponent<PlayerProjectile>(out var pp))
            pp.Launch(dir, player.damage, sizeMult);
    }

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
