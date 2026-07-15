using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws the minimap: a little coloured square for each room, positioned relative to the room
// the player is currently in. Rooms are hidden until discovered, with rooms next to a
// discovered one shown faintly as a hint of where you can go.
public class Minimap : MonoBehaviour
{
    [Header("Layout")]
    public float cellSize = 14f;   // size of each room icon in UI pixels
    public float cellGap  = 3f;    // gap between icons

    [Header("Colors")]
    public Color colorCurrent    = Color.white;                          // the room you're in
    public Color colorDiscovered = new Color(0.55f, 0.55f, 0.55f, 1f);   // rooms already visited
    public Color colorAdjacent   = new Color(0.3f,  0.3f,  0.3f,  0.5f); // faint hint of unvisited neighbours

    private readonly Dictionary<RoomController, RectTransform> icons     = new();   // room -> its icon transform
    private readonly Dictionary<RoomController, Image>         images    = new();   // room -> its icon image
    private readonly HashSet<RoomController>                   discovered = new();  // rooms the player has entered
    private RoomController currentRoom;

    // Builds a (transparent) icon for every room in the floor, then reveals the start room.
    // Called by the FloorGenerator when a floor is created.
    public void Initialize(IEnumerable<RoomController> allRooms, RoomController startRoom)
    {
        // Clear out any icons from the previous floor.
        foreach (var img in images.Values)
            if (img != null) Destroy(img.gameObject);
        icons.Clear();
        images.Clear();
        discovered.Clear();

        foreach (var room in allRooms)
        {
            var go  = new GameObject("RoomIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt  = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cellSize, cellSize);

            var img = go.GetComponent<Image>();
            img.color = Color.clear;   // invisible until discovered

            icons[room]  = rt;
            images[room] = img;
        }

        EnterRoom(startRoom);
    }

    // Marks a room discovered and makes it the current one, then redraws. Called on each room change.
    public void EnterRoom(RoomController room)
    {
        discovered.Add(room);
        currentRoom = room;
        Refresh();
    }

    // Repositions and recolours every icon relative to the current room. Undiscovered rooms that
    // aren't next to a discovered one stay invisible; the rest are placed by their grid offset
    // from the current room and coloured by state (current / discovered / adjacent hint).
    void Refresh()
    {
        if (currentRoom == null) return;

        float step = cellSize + cellGap;

        foreach (var (room, rt) in icons)
        {
            bool isCurrent    = room == currentRoom;
            bool isDiscovered = discovered.Contains(room);
            bool isAdjacent   = !isDiscovered && AdjacentToDiscovered(room);

            Image img = images[room];

            if (!isCurrent && !isDiscovered && !isAdjacent)
            {
                img.color = Color.clear;   // still hidden
                continue;
            }

            Vector2Int offset = room.gridPosition - currentRoom.gridPosition;
            rt.anchoredPosition = new Vector2(offset.x * step, offset.y * step);
            img.color = isCurrent ? colorCurrent : isDiscovered ? colorDiscovered : colorAdjacent;
        }
    }

    // True if any of the room's four neighbours has been discovered — used to decide whether to
    // show it as a faint "you can go here" hint.
    bool AdjacentToDiscovered(RoomController room)
    {
        GameObject[] neighbors = { room.topRoom, room.bottomRoom, room.leftRoom, room.rightRoom };
        foreach (var n in neighbors)
        {
            if (n == null) continue;
            var rc = n.GetComponent<RoomController>();
            if (rc != null && discovered.Contains(rc)) return true;
        }
        return false;
    }
}
