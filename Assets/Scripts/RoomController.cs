using UnityEngine;

public enum RoomType { Starting, Normal, Item, Boss }

public class RoomController : MonoBehaviour
{
    public GameObject topRoom;
    public GameObject bottomRoom;
    public GameObject leftRoom;
    public GameObject rightRoom;

    public Door topDoor;
    public Door bottomDoor;
    public Door leftDoor;
    public Door rightDoor;

    public GameObject topWall;
    public GameObject bottomWall;
    public GameObject leftWall;
    public GameObject rightWall;

    public GameObject[] enemies;

    [Header("Spawn Points")]
    public Transform topSpawnPoint;
    public Transform bottomSpawnPoint;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    [Header("Room Info")]
    public RoomType roomType = RoomType.Normal;

    private bool isInitialized = false;
    private bool isRoomCleared = false;

    void Start()
    {
        if (!isInitialized)
            Initialize();
    }

    // Called by FloorGenerator after wiring adjacent room references,
    // or by Start() for inspector-configured rooms.
    public void Initialize()
    {
        isInitialized = true;

        // HANDLE TOP
        if (topRoom != null)
        {
            topWall?.SetActive(false);
            topDoor?.gameObject.SetActive(true);
            topDoor?.CloseDoor();
        }
        else
        {
            topWall?.SetActive(true);
            topDoor?.gameObject.SetActive(false);
        }

        // HANDLE BOTTOM
        if (bottomRoom != null)
        {
            bottomWall?.SetActive(false);
            bottomDoor?.gameObject.SetActive(true);
            bottomDoor?.CloseDoor();
        }
        else
        {
            bottomWall?.SetActive(true);
            bottomDoor?.gameObject.SetActive(false);
        }

        // HANDLE LEFT
        if (leftRoom != null)
        {
            leftWall?.SetActive(false);
            leftDoor?.gameObject.SetActive(true);
            leftDoor?.CloseDoor();
        }
        else
        {
            leftWall?.SetActive(true);
            leftDoor?.gameObject.SetActive(false);
        }

        // HANDLE RIGHT
        if (rightRoom != null)
        {
            rightWall?.SetActive(false);
            rightDoor?.gameObject.SetActive(true);
            rightDoor?.CloseDoor();
        }
        else
        {
            rightWall?.SetActive(true);
            rightDoor?.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (enemies.Length == 0 && !isRoomCleared)
        {
            isRoomCleared = true;
            OpenDoors();
        }
    }

    void OpenDoors()
    {
        if (topRoom != null) topDoor.OpenDoor();
        if (bottomRoom != null) bottomDoor.OpenDoor();
        if (leftRoom != null) leftDoor.OpenDoor();
        if (rightRoom != null) rightDoor.OpenDoor();
    }
}
