using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [Header("Layout")]
    public float cellSize = 14f;
    public float cellGap  = 3f;

    [Header("Colors")]
    public Color colorCurrent    = Color.white;
    public Color colorDiscovered = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color colorAdjacent   = new Color(0.3f,  0.3f,  0.3f,  0.5f);

    private readonly Dictionary<RoomController, RectTransform> icons     = new();
    private readonly Dictionary<RoomController, Image>         images    = new();
    private readonly HashSet<RoomController>                   discovered = new();
    private RoomController currentRoom;

    public void Initialize(IEnumerable<RoomController> allRooms, RoomController startRoom)
    {
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
            img.color = Color.clear;

            icons[room]  = rt;
            images[room] = img;
        }

        EnterRoom(startRoom);
    }

    public void EnterRoom(RoomController room)
    {
        discovered.Add(room);
        currentRoom = room;
        Refresh();
    }

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
                img.color = Color.clear;
                continue;
            }

            Vector2Int offset = room.gridPosition - currentRoom.gridPosition;
            rt.anchoredPosition = new Vector2(offset.x * step, offset.y * step);
            img.color = isCurrent ? colorCurrent : isDiscovered ? colorDiscovered : colorAdjacent;
        }
    }

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
