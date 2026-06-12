using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Starting, Normal, Item, Boss }

public class RoomController : MonoBehaviour
{
    [Header("Adjacent Rooms")]
    public GameObject topRoom;
    public GameObject bottomRoom;
    public GameObject leftRoom;
    public GameObject rightRoom;

    [Header("Doors & Walls")]
    public Door topDoor;
    public Door bottomDoor;
    public Door leftDoor;
    public Door rightDoor;

    public GameObject topWall;
    public GameObject bottomWall;
    public GameObject leftWall;
    public GameObject rightWall;

    [Header("Spawn Points")]
    public Transform topSpawnPoint;
    public Transform bottomSpawnPoint;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    public float minSpawnBudget = 2f;
    public float maxSpawnBudget = 5f;
    public Vector2 spawnAreaHalfExtents = new(7f, 3.5f);
    public float initialSpawnDelay    = 0.4f;
    public float spawnInterval        = 0.15f;
    public float minDistanceFromEntry = 4f;

    [Header("Room Info")]
    public RoomType roomType = RoomType.Normal;
    [HideInInspector] public Vector2Int gridPosition;

    private bool isInitialized    = false;
    private bool isRoomCleared    = false;
    private bool hasSpawned       = false;
    private bool spawningComplete = false;
    private Transform entrySpawnPoint;
    private readonly List<GameObject> spawnedEnemies = new();

    public void SetEntryPoint(Transform spawnPoint) => entrySpawnPoint = spawnPoint;

    void Start()
    {
        if (!isInitialized)
            Initialize();
    }

    // Called by FloorGenerator after wiring adjacent room references.
    public void Initialize()
    {
        isInitialized = true;

        SetupSide(topRoom,    topWall,    topDoor);
        SetupSide(bottomRoom, bottomWall, bottomDoor);
        SetupSide(leftRoom,   leftWall,   leftDoor);
        SetupSide(rightRoom,  rightWall,  rightDoor);

        // Non-combat rooms open doors immediately
        if (!IsCombatRoom())
            ClearRoom();
    }

    // Fires each time the room is activated — spawn enemies on first entry only
    void OnEnable()
    {
        if (!isInitialized || hasSpawned || !IsCombatRoom()) return;
        hasSpawned = true;
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        float remaining = Random.Range(minSpawnBudget, maxSpawnBudget);

        var pool = new List<(GameObject prefab, float weight)>();
        foreach (var prefab in enemyPrefabs)
        {
            float w = prefab.TryGetComponent<Enemy>(out var ec) ? Mathf.Max(ec.spawnWeight, 0.01f) : 1f;
            pool.Add((prefab, w));
        }
        pool.RemoveAll(e => e.weight > remaining);

        yield return new WaitForSeconds(initialSpawnDelay);

        while (pool.Count > 0)
        {
            var (prefab, weight) = pool[Random.Range(0, pool.Count)];

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
            pool.RemoveAll(e => e.weight > remaining);
        }

        spawningComplete = true;
    }

    void Update()
    {
        if (isRoomCleared || !spawningComplete) return;

        foreach (var e in spawnedEnemies)
            if (e != null) return;

        ClearRoom();
    }

    bool IsCombatRoom() =>
        roomType == RoomType.Normal
        && enemyPrefabs != null
        && enemyPrefabs.Length > 0
        && maxSpawnBudget > 0f;

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

    void ClearRoom()
    {
        isRoomCleared = true;

        if (IsCombatRoom())
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) player.OnRoomCleared();
        }

        OpenDoors();
    }

    void OpenDoors()
    {
        if (topRoom != null)    topDoor.OpenDoor();
        if (bottomRoom != null) bottomDoor.OpenDoor();
        if (leftRoom != null)   leftDoor.OpenDoor();
        if (rightRoom != null)  rightDoor.OpenDoor();
    }
}
