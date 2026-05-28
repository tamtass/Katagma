using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloorGenerator : MonoBehaviour
{
    [Header("Layout Settings")]
    public int targetRoomCount = 10;
    public int maxItemRooms = 2;

    [Tooltip("Max existing neighbors a candidate cell may already touch (1 = most elongated, 2 = some clusters)")]
    [Range(1, 3)]
    public int maxNewRoomNeighbors = 1;

    [Header("Room Prefabs")]
    public GameObject startingRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject itemRoomPrefab;   // falls back to normalRoomPrefab if null
    public GameObject bossRoomPrefab;   // falls back to normalRoomPrefab if null

    [Header("World Settings")]
    // Should match the visual size of your room prefab
    public Vector2 roomWorldSize = new Vector2(18f, 10f);

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly List<RoomController> spawnedRooms = new();

    // -------------------------------------------------------------------------

    public void GenerateFloor()
    {
        ClearFloor();

        Dictionary<Vector2Int, RoomType> layout = BuildLayout();
        Dictionary<Vector2Int, RoomController> roomMap = InstantiateRooms(layout);
        WireAdjacency(roomMap);

        foreach (var rc in roomMap.Values)
            rc.Initialize();

        RoomController startRoom = roomMap[Vector2Int.zero];

        // Only the starting room begins active
        foreach (var rc in roomMap.Values)
            rc.gameObject.SetActive(rc == startRoom);

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
            // Prefer dead ends (fewer neighbors) so branches stay long before splitting.
            // Add a small random tiebreak so we don't always pick the exact same room.
            Vector2Int source = positionList
                .OrderBy(p => CountNeighbors(p, positions) + Random.value * 0.49f)
                .First();

            // Try directions in random order
            foreach (var dir in Directions.OrderBy(_ => Random.value))
            {
                Vector2Int candidate = source + dir;

                // Only place a room if it would touch at most maxNewRoomNeighbors
                // existing rooms (including the source we are expanding from).
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
        // Build adjacency counts
        var neighborCount = new Dictionary<Vector2Int, int>();
        foreach (var pos in positions)
        {
            int count = Directions.Count(d => positions.Contains(pos + d));
            neighborCount[pos] = count;
        }

        // BFS distances from start
        var dist = BfsDistances(Vector2Int.zero, positions);

        // Dead ends: exactly 1 neighbor, not the starting room
        var deadEnds = positions
            .Where(p => p != Vector2Int.zero && neighborCount[p] == 1)
            .OrderByDescending(p => dist[p])
            .ToList();

        var result = new Dictionary<Vector2Int, RoomType>();
        result[Vector2Int.zero] = RoomType.Starting;

        // Boss = farthest dead end from start
        if (deadEnds.Count > 0)
        {
            result[deadEnds[0]] = RoomType.Boss;
            deadEnds.RemoveAt(0);
        }

        // Item rooms = next dead ends up to maxItemRooms
        for (int i = 0; i < Mathf.Min(maxItemRooms, deadEnds.Count); i++)
            result[deadEnds[i]] = RoomType.Item;

        // Everything else = Normal
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
            rc.roomType = type;
            roomMap[gridPos] = rc;
            spawnedRooms.Add(rc);
        }

        return roomMap;
    }

    private void WireAdjacency(Dictionary<Vector2Int, RoomController> roomMap)
    {
        foreach (var (pos, rc) in roomMap)
        {
            if (roomMap.TryGetValue(pos + Vector2Int.up,    out var top))    rc.topRoom    = top.gameObject;
            if (roomMap.TryGetValue(pos + Vector2Int.down,  out var bot))    rc.bottomRoom = bot.gameObject;
            if (roomMap.TryGetValue(pos + Vector2Int.left,  out var left))   rc.leftRoom   = left.gameObject;
            if (roomMap.TryGetValue(pos + Vector2Int.right, out var right))  rc.rightRoom  = right.gameObject;
        }
    }

    private GameObject PrefabFor(RoomType type) => type switch
    {
        RoomType.Starting => startingRoomPrefab != null ? startingRoomPrefab : normalRoomPrefab,
        RoomType.Item     => itemRoomPrefab     != null ? itemRoomPrefab     : normalRoomPrefab,
        RoomType.Boss     => bossRoomPrefab     != null ? bossRoomPrefab     : normalRoomPrefab,
        _                 => normalRoomPrefab
    };
}
