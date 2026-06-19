using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class FloorConfig
{
    public GameObject startingRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject itemRoomPrefab;
    public GameObject bossRoomPrefab;
}

public class FloorGenerator : MonoBehaviour
{
    [Header("Layout Settings")]
    public int targetRoomCount = 10;
    public int maxItemRooms = 2;

    [Tooltip("Max existing neighbors a candidate cell may already touch (1 = most elongated, 2 = some clusters)")]
    [Range(1, 3)]
    public int maxNewRoomNeighbors = 1;

    [Header("Floors")]
    public FloorConfig[] floors;

    [Header("World Settings")]
    public Vector2 roomWorldSize = new Vector2(18f, 10f);

    [Header("References")]
    public Minimap minimap;

    public int FloorCount => floors != null ? floors.Length : 0;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly List<RoomController> spawnedRooms = new();
    private FloorConfig _currentFloor;

    // -------------------------------------------------------------------------

    public void GenerateFloor(int floorIndex)
    {
        ClearFloor();
        _currentFloor = floors[Mathf.Clamp(floorIndex, 0, floors.Length - 1)];

        Dictionary<Vector2Int, RoomType> layout = BuildLayout();
        Dictionary<Vector2Int, RoomController> roomMap = InstantiateRooms(layout);
        WireAdjacency(roomMap);

        foreach (var rc in roomMap.Values)
            rc.Initialize();

        RoomController startRoom = roomMap[Vector2Int.zero];

        foreach (var rc in roomMap.Values)
            rc.gameObject.SetActive(rc == startRoom);

        if (minimap != null)
            minimap.Initialize(roomMap.Values, startRoom);

        if (RoomTransitionManager.Instance != null)
            RoomTransitionManager.Instance.Initialize(startRoom);
    }

    public void ClearFloor()
    {
        foreach (var rc in spawnedRooms)
            if (rc != null) Destroy(rc.gameObject);
        spawnedRooms.Clear();
    }

    // -------------------------------------------------------------------------
    // Layout generation (random walk expansion)

    private Dictionary<Vector2Int, RoomType> BuildLayout()
    {
        var positions = new HashSet<Vector2Int> { Vector2Int.zero };
        var positionList = new List<Vector2Int> { Vector2Int.zero };

        int safetyLimit = targetRoomCount * 500;
        int attempts = 0;

        while (positions.Count < targetRoomCount && attempts++ < safetyLimit)
        {
            Vector2Int source = positionList
                .OrderBy(p => CountNeighbors(p, positions) + Random.value * 0.49f)
                .First();

            foreach (var dir in Directions.OrderBy(_ => Random.value))
            {
                Vector2Int candidate = source + dir;

                if (!positions.Contains(candidate) &&
                    CountNeighbors(candidate, positions) <= maxNewRoomNeighbors)
                {
                    positions.Add(candidate);
                    positionList.Add(candidate);
                    break;
                }
            }
        }

        return AssignTypes(positions);
    }

    private int CountNeighbors(Vector2Int pos, HashSet<Vector2Int> existing)
        => Directions.Count(d => existing.Contains(pos + d));

    private Dictionary<Vector2Int, RoomType> AssignTypes(HashSet<Vector2Int> positions)
    {
        var neighborCount = new Dictionary<Vector2Int, int>();
        foreach (var pos in positions)
            neighborCount[pos] = Directions.Count(d => positions.Contains(pos + d));

        var dist = BfsDistances(Vector2Int.zero, positions);

        var deadEnds = positions
            .Where(p => p != Vector2Int.zero && neighborCount[p] == 1)
            .OrderByDescending(p => dist[p])
            .ToList();

        var result = new Dictionary<Vector2Int, RoomType>();
        result[Vector2Int.zero] = RoomType.Starting;

        // Boss = always the farthest non-starting room, dead end or not
        Vector2Int bossPos = positions
            .Where(p => p != Vector2Int.zero)
            .OrderByDescending(p => dist.GetValueOrDefault(p, 0))
            .First();
        result[bossPos] = RoomType.Boss;
        deadEnds.Remove(bossPos);

        // Item rooms = farthest unassigned rooms from the boss room
        var distFromBoss = BfsDistances(bossPos, positions);
        foreach (var pos in positions
            .Where(p => !result.ContainsKey(p))
            .OrderByDescending(p => distFromBoss.GetValueOrDefault(p, 0))
            .Take(maxItemRooms))
        {
            result[pos] = RoomType.Item;
        }

        foreach (var pos in positions)
            if (!result.ContainsKey(pos))
                result[pos] = RoomType.Normal;

        return result;
    }

    private Dictionary<Vector2Int, int> BfsDistances(Vector2Int origin, HashSet<Vector2Int> positions)
    {
        var dist = new Dictionary<Vector2Int, int> { [origin] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            foreach (var dir in Directions)
            {
                var neighbor = pos + dir;
                if (positions.Contains(neighbor) && !dist.ContainsKey(neighbor))
                {
                    dist[neighbor] = dist[pos] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return dist;
    }

    // -------------------------------------------------------------------------
    // Instantiation and wiring

    private Dictionary<Vector2Int, RoomController> InstantiateRooms(Dictionary<Vector2Int, RoomType> layout)
    {
        var roomMap = new Dictionary<Vector2Int, RoomController>();

        foreach (var (gridPos, type) in layout)
        {
            GameObject prefab = PrefabFor(type);
            Vector3 worldPos = new Vector3(gridPos.x * roomWorldSize.x, gridPos.y * roomWorldSize.y, 0f);
            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            go.name = $"Room_{gridPos.x}_{gridPos.y}_{type}";

            var rc = go.GetComponent<RoomController>();
            rc.roomType     = type;
            rc.gridPosition = gridPos;
            roomMap[gridPos] = rc;
            spawnedRooms.Add(rc);
        }

        return roomMap;
    }

    private void WireAdjacency(Dictionary<Vector2Int, RoomController> roomMap)
    {
        foreach (var (pos, rc) in roomMap)
        {
            if (roomMap.TryGetValue(pos + Vector2Int.up,    out var top))   rc.topRoom    = top.gameObject;
            if (roomMap.TryGetValue(pos + Vector2Int.down,  out var bot))   rc.bottomRoom = bot.gameObject;
            if (roomMap.TryGetValue(pos + Vector2Int.left,  out var left))  rc.leftRoom   = left.gameObject;
            if (roomMap.TryGetValue(pos + Vector2Int.right, out var right)) rc.rightRoom  = right.gameObject;
        }
    }

    private GameObject PrefabFor(RoomType type) => type switch
    {
        RoomType.Starting => _currentFloor.startingRoomPrefab != null ? _currentFloor.startingRoomPrefab : _currentFloor.normalRoomPrefab,
        RoomType.Item     => _currentFloor.itemRoomPrefab     != null ? _currentFloor.itemRoomPrefab     : _currentFloor.normalRoomPrefab,
        RoomType.Boss     => _currentFloor.bossRoomPrefab     != null ? _currentFloor.bossRoomPrefab     : _currentFloor.normalRoomPrefab,
        _                 => _currentFloor.normalRoomPrefab
    };
}
