using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance { get; private set; }

    [Header("Settings")]
    public float transitionDuration = 0.4f;

    [Header("References")]
    public RoomController startingRoom;
    public Camera mainCamera;
    public PlayerMovement playerMovement;
    public Minimap minimap;

    public bool IsTransitioning { get; private set; }

    private RoomController currentRoom;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // Fallback for inspector-based testing without FloorGenerator
    void Start()
    {
        if (startingRoom != null)
            Initialize(startingRoom);
    }

    // Called by FloorGenerator after the floor is built
    public void Initialize(RoomController startRoom)
    {
        currentRoom = startRoom;
        mainCamera.transform.position = new Vector3(
            startRoom.transform.position.x,
            startRoom.transform.position.y,
            mainCamera.transform.position.z);
    }

    public void StartTransition(DoorTrigger.Direction direction, GameObject player)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionCoroutine(direction, player));
    }

    private IEnumerator TransitionCoroutine(DoorTrigger.Direction direction, GameObject player)
    {
        IsTransitioning = true;
        playerMovement.canMove = false;

        RoomController targetRoom = GetAdjacentRoom(direction);
        if (targetRoom == null)
        {
            playerMovement.canMove = true;
            IsTransitioning = false;
            yield break;
        }

        // Activate the target room before the pan so it's visible as the camera arrives
        targetRoom.gameObject.SetActive(true);

        // Camera pan from current room center to target room center
        Vector3 camStart = new Vector3(currentRoom.transform.position.x,
                                       currentRoom.transform.position.y,
                                       mainCamera.transform.position.z);
        Vector3 camEnd   = new Vector3(targetRoom.transform.position.x,
                                       targetRoom.transform.position.y,
                                       mainCamera.transform.position.z);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            mainCamera.transform.position = Vector3.Lerp(camStart, camEnd, t);
            yield return null;
        }
        mainCamera.transform.position = camEnd;

        // Reposition player to opposite spawn in new room (after pan so they're never seen out of place)
        Transform spawnPoint = GetOppositeSpawn(direction, targetRoom);
        if (spawnPoint != null)
            player.transform.position = spawnPoint.position;

        // Deactivate the room we just left
        currentRoom.gameObject.SetActive(false);

        currentRoom = targetRoom;
        minimap?.EnterRoom(targetRoom);
        playerMovement.canMove = true;
        IsTransitioning = false;
    }

    private RoomController GetAdjacentRoom(DoorTrigger.Direction dir)
    {
        GameObject go = dir switch
        {
            DoorTrigger.Direction.Top    => currentRoom.topRoom,
            DoorTrigger.Direction.Bottom => currentRoom.bottomRoom,
            DoorTrigger.Direction.Left   => currentRoom.leftRoom,
            DoorTrigger.Direction.Right  => currentRoom.rightRoom,
            _                            => null
        };
        return go != null ? go.GetComponent<RoomController>() : null;
    }

    private Transform GetOppositeSpawn(DoorTrigger.Direction dir, RoomController room)
    {
        // Player exits Top → enters new room from Bottom → spawn at bottomSpawnPoint
        return dir switch
        {
            DoorTrigger.Direction.Top    => room.bottomSpawnPoint,
            DoorTrigger.Direction.Bottom => room.topSpawnPoint,
            DoorTrigger.Direction.Left   => room.rightSpawnPoint,
            DoorTrigger.Direction.Right  => room.leftSpawnPoint,
            _                            => null
        };
    }
}
