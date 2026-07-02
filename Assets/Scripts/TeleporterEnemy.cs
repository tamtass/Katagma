using System.Collections;
using UnityEngine;

public class TeleporterEnemy : Enemy
{
    [Header("Teleporter — Shooting")]
    public GameObject projectilePrefab;
    public float windupDuration  = 0.5f;   // telegraph flash before firing
    public float postShotPause   = 0.6f;   // pause between shot and teleport

    [Header("Teleporter — Teleporting")]
    public float postTeleportPause  = 0.4f;
    public float teleportHalfTime   = 0.15f;  // seconds for scale-out and scale-in each
    public float minPlayerDistance  = 3f;
    public int   maxTeleportAttempts = 20;

    private static readonly Color _windupColor = new(1f, 0.35f, 0f);

    protected override void Start()
    {
        moveSpeed = 0f;
        base.Start();
        StartCoroutine(BehaviourLoop());
    }

    private IEnumerator BehaviourLoop()
    {
        // Wait for the spawn animation to finish
        yield return new WaitForSeconds(spawnStunDuration);

        while (true)
        {
            yield return ShootAtPlayer();
            yield return new WaitForSeconds(postShotPause);
            yield return TeleportToRandomPosition();
            yield return new WaitForSeconds(postTeleportPause);
        }
    }

    private IEnumerator ShootAtPlayer()
    {
        if (projectilePrefab == null) yield break;

        // Orange telegraph
        if (spriteRenderer != null) spriteRenderer.color = _windupColor;
        yield return new WaitForSeconds(windupDuration);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        if (player == null) yield break;

        Vector2 dir = (player.position - transform.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (proj.TryGetComponent<EnemyProjectile>(out var ep))
            ep.Launch(dir, damage, gameObject);
    }

    private IEnumerator TeleportToRandomPosition()
    {
        suppressScalePulse = true;

        // Scale out
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

        // Scale in
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

    private Vector2 FindTeleportPosition()
    {
        RoomController room = GetComponentInParent<RoomController>();
        Vector2 center = room != null ? (Vector2)room.transform.position : (Vector2)transform.position;
        Vector2 half   = room != null ? room.spawnAreaHalfExtents : new Vector2(6f, 3f);

        Vector2 playerPos = player != null ? (Vector2)player.position : center;

        // Try random positions until one is far enough from the player
        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));

            if (Vector2.Distance(candidate, playerPos) >= minPlayerDistance)
                return candidate;
        }

        // Fallback: pick the farthest of several random candidates
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
