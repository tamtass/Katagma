using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The four kinds of room a floor can contain.
public enum RoomType { Starting, Normal, Item, Boss }

// Controls a single room: which sides have doors vs walls, what spawns inside (enemies, a boss,
// or an item), and when the room counts as "cleared" (which opens the doors and, in a boss room,
// reveals the exit to the next floor). One of these sits on every room prefab.
public class RoomController : MonoBehaviour
{
    [Header("Adjacent Rooms")]
    // The neighbouring rooms in each direction, filled in by the FloorGenerator. Null means
    // there's no room that way, so that side gets a wall instead of a door.
    public GameObject topRoom;
    public GameObject bottomRoom;
    public GameObject leftRoom;
    public GameObject rightRoom;

    [Header("Doors & Walls")]
    // The door and wall objects for each side. Exactly one of each pair is used per side,
    // depending on whether there's a neighbour there.
    public Door topDoor;
    public Door bottomDoor;
    public Door leftDoor;
    public Door rightDoor;

    public GameObject topWall;
    public GameObject bottomWall;
    public GameObject leftWall;
    public GameObject rightWall;

    [Header("Spawn Points")]
    // Where the player appears when entering from each side, and where an item sits in an item room.
    public Transform topSpawnPoint;
    public Transform bottomSpawnPoint;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public Transform itemSpawnPoint;

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;                       // enemy types this room can spawn
    public float minSpawnBudget = 2f;                       // random "budget" spent on enemies
    public float maxSpawnBudget = 5f;                       // (each enemy costs its spawnWeight)
    public Vector2 spawnAreaHalfExtents = new(7f, 3.5f);    // rectangle enemies spawn within
    public float initialSpawnDelay    = 0.4f;               // pause before the first spawn
    public float spawnInterval        = 0.15f;
    public float minDistanceFromEntry = 4f;                 // keep spawns away from where the player enters

    [Header("Boss Spawning")]
    public GameObject[] bossPrefabs;
    public GameObject exitDoor;                    // the door to the next floor, hidden until the boss dies
    public float exitDoorDelay          = 1.5f;    // pause after the boss dies before revealing it
    public float exitDoorRevealDuration = 0.5f;    // how long the reveal animation takes

    [Header("Room Info")]
    public RoomType roomType = RoomType.Normal;              // set by the generator
    [HideInInspector] public Vector2Int gridPosition;        // this room's cell on the floor grid

    private bool isInitialized    = false;
    private bool isRoomCleared    = false;
    private bool hasSpawned       = false;   // spawn contents only once, on first entry
    private bool spawningComplete = false;   // true once all enemies/boss have been spawned
    private Transform entrySpawnPoint;       // where the player came in, so enemies avoid it
    private readonly List<GameObject> spawnedEnemies = new();

    // Told to the room before the player enters, so enemy spawns can steer clear of the entrance.
    public void SetEntryPoint(Transform spawnPoint) => entrySpawnPoint = spawnPoint;

    // Safety net so the room still initializes if it wasn't set up by the generator (e.g. testing).
    void Start()
    {
        if (!isInitialized)
            Initialize();
    }

    // Sets up the doors/walls based on which neighbours exist. Rooms that have nothing to spawn
    // (start rooms, and normal rooms with no enemies) are marked cleared right away so their
    // doors are open. Called by the generator after neighbours are wired.
    public void Initialize()
    {
        isInitialized = true;

        SetupSide(topRoom,    topWall,    topDoor);
        SetupSide(bottomRoom, bottomWall, bottomDoor);
        SetupSide(leftRoom,   leftWall,   leftDoor);
        SetupSide(rightRoom,  rightWall,  rightDoor);

        // Item rooms hold off spawning their item until first entered.
        if (!IsCombatRoom() && !IsBossRoom() && roomType != RoomType.Item)
            ClearRoom();
    }

    // Runs when the room is switched on. The first time it's entered, it spawns its contents —
    // enemies, a boss, or an item, depending on the room type.
    void OnEnable()
    {
        if (!isInitialized || hasSpawned) return;
        if (IsCombatRoom())             { hasSpawned = true; StartCoroutine(SpawnEnemies()); }
        else if (IsBossRoom())          { hasSpawned = true; StartCoroutine(SpawnBoss()); }
        else if (roomType == RoomType.Item) { hasSpawned = true; SpawnItem(); }
    }

    // Spawns enemies until a random "budget" is used up. Each enemy costs its spawn weight, and
    // only enemies the remaining budget can still afford stay eligible, so cheaper enemies fill
    // the leftover budget. Spawn positions are random within the room but nudged away from the
    // player's entry point so they don't appear right on top of them.
    private IEnumerator SpawnEnemies()
    {
        float remaining = Random.Range(minSpawnBudget, maxSpawnBudget);

        var pool = new List<(GameObject prefab, float weight)>();
        foreach (var prefab in enemyPrefabs)
        {
            float w = prefab.TryGetComponent<Enemy>(out var ec) ? Mathf.Max(ec.spawnWeight, 0.01f) : 1f;
            pool.Add((prefab, w));
        }
        pool.RemoveAll(e => e.weight > remaining);   // drop anything already unaffordable

        yield return new WaitForSeconds(initialSpawnDelay);

        while (pool.Count > 0)
        {
            var (prefab, weight) = pool[Random.Range(0, pool.Count)];

            // Try a few random spots until one is far enough from the entrance (give up after 10).
            Vector3 pos;
            int attempts = 0;
            do
            {
                pos = transform.position + new Vector3(
                    Random.Range(-spawnAreaHalfExtents.x, spawnAreaHalfExtents.x),
                    Random.Range(-spawnAreaHalfExtents.y, spawnAreaHalfExtents.y),
                    0f);
                attempts++;
            }
            while (entrySpawnPoint != null
                && Vector2.Distance(pos, entrySpawnPoint.position) < minDistanceFromEntry
                && attempts < 10);

            spawnedEnemies.Add(Instantiate(prefab, pos, Quaternion.identity, transform));

            remaining -= weight;
            pool.RemoveAll(e => e.weight > remaining);   // re-filter now that budget shrank
        }

        spawningComplete = true;
    }

    // Spawns the boss in the centre and hooks it up to the room's boss health bar.
    private IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        GameObject prefab   = bossPrefabs[Random.Range(0, bossPrefabs.Length)];
        GameObject bossObj  = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        spawnedEnemies.Add(bossObj);

        BossHealthBar bar = GetComponentInChildren<BossHealthBar>(true);
        if (bar != null && bossObj.TryGetComponent<Enemy>(out var enemy))
            bar.SetBoss(enemy);

        spawningComplete = true;
    }

    // Draws one random item from the run's pool and places it, then clears the room (item rooms
    // have no enemies to fight).
    private void SpawnItem()
    {
        if (GameManager.Instance != null && itemSpawnPoint != null)
        {
            GameObject prefab = GameManager.Instance.TakeRandomItem();
            if (prefab != null)
                Instantiate(prefab, itemSpawnPoint.position, Quaternion.identity, transform);
        }
        ClearRoom();
    }

    // After the boss dies, waits a beat then reveals the exit door with a horizontal scale-in.
    private IEnumerator RevealExitDoor()
    {
        yield return new WaitForSeconds(exitDoorDelay);

        exitDoor.SetActive(true);
        Vector3 scale = exitDoor.transform.localScale;
        exitDoor.transform.localScale = new Vector3(0f, scale.y, scale.z);   // start squashed to zero width

        float elapsed = 0f;
        while (elapsed < exitDoorRevealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / exitDoorRevealDuration);
            scale = exitDoor.transform.localScale;
            exitDoor.transform.localScale = new Vector3(t, scale.y, scale.z);
            yield return null;
        }

        exitDoor.transform.localScale = new Vector3(1f, exitDoor.transform.localScale.y, exitDoor.transform.localScale.z);
    }

    // Watches for the room being cleared: once spawning is done and every spawned enemy is gone,
    // clear the room.
    void Update()
    {
        if (isRoomCleared || !spawningComplete) return;

        foreach (var e in spawnedEnemies)
            if (e != null) return;   // something's still alive, not cleared yet

        ClearRoom();
    }

    // A combat room is a normal room that actually has enemies and a budget to spend.
    bool IsCombatRoom() =>
        roomType == RoomType.Normal
        && enemyPrefabs != null
        && enemyPrefabs.Length > 0
        && maxSpawnBudget > 0f;

    // A boss room is one flagged as Boss that has a boss prefab to spawn.
    bool IsBossRoom() =>
        roomType == RoomType.Boss
        && bossPrefabs != null
        && bossPrefabs.Length > 0;

    // For one side of the room: if there's a neighbour that way, hide the wall and show a
    // (closed) door; otherwise show the wall and hide the door.
    void SetupSide(GameObject adjacent, GameObject wall, Door door)
    {
        if (adjacent != null)
        {
            if (wall != null) wall.SetActive(false);
            if (door != null) { door.gameObject.SetActive(true); door.CloseDoor(); }
        }
        else
        {
            if (wall != null) wall.SetActive(true);
            if (door != null) door.gameObject.SetActive(false);
        }
    }

    // Marks the room cleared. For combat/boss rooms this awards score, bumps the cleared-room
    // counter, and gives the player their random stat upgrade; boss rooms also reveal the exit.
    // Either way, the doors open so the player can move on.
    void ClearRoom()
    {
        isRoomCleared = true;

        if (IsCombatRoom() || IsBossRoom())
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(100);
                GameManager.Instance.OnCombatRoomCleared();
            }
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) player.OnRoomCleared();
        }

        if (IsBossRoom() && exitDoor != null)
            StartCoroutine(RevealExitDoor());
        OpenDoors();
    }

    // Opens every door that actually leads somewhere.
    void OpenDoors()
    {
        if (topRoom != null)    topDoor.OpenDoor();
        if (bottomRoom != null) bottomDoor.OpenDoor();
        if (leftRoom != null)   leftDoor.OpenDoor();
        if (rightRoom != null)  rightDoor.OpenDoor();
    }
}
