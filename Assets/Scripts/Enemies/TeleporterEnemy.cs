using System.Collections;
using UnityEngine;

// A stationary enemy that fights by shooting and blinking around. Its loop is: telegraph,
// fire a shot at the player, pause, teleport to a random spot away from the player, pause,
// repeat. It never chases (moveSpeed is forced to zero), so it plays as a ranged threat
// the player has to keep chasing down.
public class TeleporterEnemy : Enemy
{
    [Header("Teleporter — Shooting")]
    public GameObject projectilePrefab;
    public float windupDuration  = 0.5f;   // how long the orange telegraph shows before firing
    public float postShotPause   = 0.6f;   // pause after firing, before teleporting

    [Header("Teleporter — Teleporting")]
    public float postTeleportPause  = 0.4f;    // pause after reappearing, before the next shot
    public float teleportHalfTime   = 0.15f;   // duration of the scale-out and scale-in each
    public float minPlayerDistance  = 3f;      // won't reappear closer than this to the player
    public int   maxTeleportAttempts = 20;     // random tries before falling back to a best guess

    private static readonly Color _windupColor = new(1f, 0.35f, 0f);   // orange telegraph tint

    // Force it stationary, then start its behaviour loop after the normal spawn setup.
    protected override void Start()
    {
        moveSpeed = 0f;
        base.Start();
        StartCoroutine(BehaviourLoop());
    }

    // The endless shoot-teleport cycle. Waits for the spawn-in first so it doesn't act
    // while still scaling up.
    private IEnumerator BehaviourLoop()
    {
        yield return new WaitForSeconds(spawnStunDuration);

        while (true)
        {
            yield return ShootAtPlayer();
            yield return new WaitForSeconds(postShotPause);
            yield return TeleportToRandomPosition();
            yield return new WaitForSeconds(postTeleportPause);
        }
    }

    // Flashes orange as a warning, then fires one projectile toward the player's position
    // at that moment. The telegraph gives the player a fair chance to dodge.
    private IEnumerator ShootAtPlayer()
    {
        if (projectilePrefab == null) yield break;

        if (spriteRenderer != null) spriteRenderer.color = _windupColor;
        yield return new WaitForSeconds(windupDuration);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        if (player == null) yield break;

        Vector2 dir = (player.position - transform.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (proj.TryGetComponent<EnemyProjectile>(out var ep))
            ep.Launch(dir, damage, gameObject);
    }

    // The teleport: scale down to nothing, jump to a new spot, scale back up. The idle
    // scale pulse is suppressed during this so it doesn't fight the animation.
    private IEnumerator TeleportToRandomPosition()
    {
        suppressScalePulse = true;

        // Shrink away.
        float elapsed = 0f;
        while (elapsed < teleportHalfTime)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.SmoothStep(0f, 1f, elapsed / teleportHalfTime);
            transform.localScale = Vector3.one * t;
            yield return null;
        }

        Vector2 dest = FindTeleportPosition();
        transform.position = new Vector3(dest.x, dest.y, transform.position.z);

        // Grow back in at the new spot.
        elapsed = 0f;
        while (elapsed < teleportHalfTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / teleportHalfTime);
            transform.localScale = Vector3.one * t;
            yield return null;
        }

        transform.localScale = Vector3.one;
        suppressScalePulse = false;
    }

    // Chooses where to reappear. Samples random points inside the room's spawn area and
    // accepts the first that's far enough from the player. If none qualify after enough
    // tries (small room, player centred), it falls back to the farthest of a few samples
    // so it always returns something and never loops forever.
    private Vector2 FindTeleportPosition()
    {
        RoomController room = GetComponentInParent<RoomController>();
        Vector2 center = room != null ? (Vector2)room.transform.position : (Vector2)transform.position;
        Vector2 half   = room != null ? room.spawnAreaHalfExtents : new Vector2(6f, 3f);

        Vector2 playerPos = player != null ? (Vector2)player.position : center;

        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));

            if (Vector2.Distance(candidate, playerPos) >= minPlayerDistance)
                return candidate;
        }

        // Fallback: whichever of a handful of random points is farthest from the player.
        Vector2 best = center;
        float bestDist = 0f;
        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));
            float d = Vector2.Distance(candidate, playerPos);
            if (d > bestDist) { bestDist = d; best = candidate; }
        }
        return best;
    }
}
