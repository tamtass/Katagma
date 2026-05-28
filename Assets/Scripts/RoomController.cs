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
    public Vector2 spawnAreaHalfExtents = new Vector2(7f, 3.5f);

    [Header("Room Info")]
    public RoomType roomType = RoomType.Normal;
    [HideInInspector] public Vector2Int gridPosition;

    private bool isInitialized = false;
    private bool isRoomCleared = false;
    private bool hasSpawned    = false;
    private readonly List<GameObject> spawnedEnemies = new();

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
        float remaining = Random.Range(minSpawnBudget, maxSpawnBudget);

        // Build pool with cached weights; entries are removed when they no longer fit
        var pool = new List<(GameObject prefab, float weight)>();
        foreach (var prefab in enemyPrefabs)
        {
            float w = Mathf.Max(prefab.GetComponent<Enemy>()?.spawnWeight ?? 1f, 0.01f);
            pool.Add((prefab, w));
        }
        pool.RemoveAll(e => e.weight > remaining);

        while (pool.Count > 0)
        {
            var (prefab, weight) = pool[Random.Range(0, pool.Count)];

            Vector3 pos = transform.position + new Vector3(
                Random.Range(-spawnAreaHalfExtents.x, spawnAreaHalfExtents.x),
                Random.Range(-spawnAreaHalfExtents.y, spawnAreaHalfExtents.y),
                0f);
            spawnedEnemies.Add(Instantiate(prefab, pos, Quaternion.identity, transform));

            remaining -= weight;
            pool.RemoveAll(e => e.weight > remaining);
        }
    }

    void Update()
    {
        if (isRoomCleared || !hasSpawned) return;

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
            wall?.SetActive(false);
            door?.gameObject.SetActive(true);
            door?.CloseDoor();
        }
        else
        {
            wall?.SetActive(true);
            door?.gameObject.SetActive(false);
        }
    }

    void ClearRoom()
    {
        isRoomCleared = true;
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
