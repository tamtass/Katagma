using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The set of room prefabs for one floor. Grouped so each floor can look different.
[System.Serializable]
public class FloorConfig
{
    public GameObject startingRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject itemRoomPrefab;
    public GameObject bossRoomPrefab;
}

// Procedurally builds a dungeon floor. It works in two phases: first it decides the shape of
// the floor on a grid (a constrained random walk that makes branchy, sparse layouts), then it
// assigns a type to each room (start, boss, item, normal) using distances through the layout.
// Finally it instantiates the room prefabs, links them up, and activates only the start room.
public class FloorGenerator : MonoBehaviour
{
    [Header("Layout Settings")]
    public int targetRoomCount = 10;   // how many rooms the floor should have
    public int maxItemRooms = 2;       // how many item rooms to place

    [Tooltip("Max existing neighbors a candidate cell may already touch (1 = most elongated, 2 = some clusters)")]
    [Range(1, 3)]
    public int maxNewRoomNeighbors = 1;   // lower = thinner, more branching layouts

    [Header("Floors")]
    public FloorConfig[] floors;   // one config per floor, in order

    [Header("World Settings")]
    public Vector2 roomWorldSize = new Vector2(18f, 10f);   // spacing between rooms in world units

    [Header("References")]
    public Minimap minimap;

    public int FloorCount => floors != null ? floors.Length : 0;

    // The four grid directions, reused throughout the generation.
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly List<RoomController> spawnedRooms = new();   // everything spawned this floor, for cleanup
    private FloorConfig _currentFloor;

    // Builds a whole floor for the given index: clear the old one, pick the shape, assign types,
    // spawn and link the rooms, activate the start room, and hand off to the minimap and the
    // room-transition manager.
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

        // Only the start room is active at first; others switch on as the player enters them.
        foreach (var rc in roomMap.Values)
            rc.gameObject.SetActive(rc == startRoom);

        if (minimap != null)
            minimap.Initialize(roomMap.Values, startRoom);

        if (RoomTransitionManager.Instance != null)
            RoomTransitionManager.Instance.Initialize(startRoom);
    }

    // Destroys the current floor's rooms. Called before generating a new floor and on returning
    // to the menu.
    public void ClearFloor()
    {
        foreach (var rc in spawnedRooms)
            if (rc != null) Destroy(rc.gameObject);
        spawnedRooms.Clear();
    }

    // Phase 1: decide which grid cells are rooms. Starts from the origin and grows outward one
    // cell at a time. Each step it picks an existing room to grow from — preferring ones with
    // few neighbours (plus a little randomness) so the floor spreads out rather than clumping —
    // and adds a random adjacent empty cell, as long as that cell wouldn't touch too many
    // existing rooms. The neighbour cap is what keeps corridors thin and branchy. Returns the
    // grid with each cell's assigned type.
    private Dictionary<Vector2Int, RoomType> BuildLayout()
    {
        var positions = new HashSet<Vector2Int> { Vector2Int.zero };
        var positionList = new List<Vector2Int> { Vector2Int.zero };

        int safetyLimit = targetRoomCount * 500;   // avoids an infinite loop if it gets stuck
        int attempts = 0;

        while (positions.Count < targetRoomCount && attempts++ < safetyLimit)
        {
            // Grow from the room with the fewest neighbours; the 0.49 noise breaks ties randomly
            // without ever overriding a real difference in neighbour count.
            Vector2Int source = positionList
                .OrderBy(p => CountNeighbors(p, positions) + Random.value * 0.49f)
                .First();

            // Try the four directions in random order; take the first valid empty cell.
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

    // Counts how many of a cell's four neighbours are already rooms.
    private int CountNeighbors(Vector2Int pos, HashSet<Vector2Int> existing)
        => Directions.Count(d => existing.Contains(pos + d));

    // Phase 2: assign a type to each room. The start is always the origin. The boss is placed in
    // the room farthest from the start (by step count through the layout), so the player has to
    // traverse the floor to reach it. Item rooms are then placed in the rooms farthest from the
    // boss, spreading rewards away from the finish. Everything else is a normal combat room.
    private Dictionary<Vector2Int, RoomType> AssignTypes(HashSet<Vector2Int> positions)
    {
        var neighborCount = new Dictionary<Vector2Int, int>();
        foreach (var pos in positions)
            neighborCount[pos] = Directions.Count(d => positions.Contains(pos + d));

        var dist = BfsDistances(Vector2Int.zero, positions);   // steps from the start to each room

        // Dead ends (rooms with a single neighbour), farthest first. Kept from an earlier design;
        // still computed so the boss can be excluded from it.
        var deadEnds = positions
            .Where(p => p != Vector2Int.zero && neighborCount[p] == 1)
            .OrderByDescending(p => dist[p])
            .ToList();

        var result = new Dictionary<Vector2Int, RoomType>();
        result[Vector2Int.zero] = RoomType.Starting;

        // Boss: the farthest room from the start, whether or not it's a dead end.
        Vector2Int bossPos = positions
            .Where(p => p != Vector2Int.zero)
            .OrderByDescending(p => dist.GetValueOrDefault(p, 0))
            .First();
        result[bossPos] = RoomType.Boss;
        deadEnds.Remove(bossPos);

        // Item rooms: the still-unassigned rooms farthest from the boss.
        var distFromBoss = BfsDistances(bossPos, positions);
        foreach (var pos in positions
            .Where(p => !result.ContainsKey(p))
            .OrderByDescending(p => distFromBoss.GetValueOrDefault(p, 0))
            .Take(maxItemRooms))
        {
            result[pos] = RoomType.Item;
        }

        // Everything left over is a normal combat room.
        foreach (var pos in positions)
            if (!result.ContainsKey(pos))
                result[pos] = RoomType.Normal;

        return result;
    }

    // Standard breadth-first search over the room grid, returning the number of steps from the
    // origin to every reachable room. Used to place the boss (far from start) and item rooms
    // (far from boss).
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

    // Spawns a room prefab for each cell, placing it in the world according to its grid position
    // and room spacing, and records its type and grid position on the RoomController.
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

    // Tells each room which rooms sit next to it in each direction, so it knows where its doors
    // lead and which walls to seal off.
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

    // Picks the prefab for a room type from the current floor's config, falling back to the
    // normal-room prefab if a specific one isn't assigned.
    private GameObject PrefabFor(RoomType type) => type switch
    {
        RoomType.Starting => _currentFloor.startingRoomPrefab != null ? _currentFloor.startingRoomPrefab : _currentFloor.normalRoomPrefab,
        RoomType.Item     => _currentFloor.itemRoomPrefab     != null ? _currentFloor.itemRoomPrefab     : _currentFloor.normalRoomPrefab,
        RoomType.Boss     => _currentFloor.bossRoomPrefab     != null ? _currentFloor.bossRoomPrefab     : _currentFloor.normalRoomPrefab,
        _                 => _currentFloor.normalRoomPrefab
    };
}
