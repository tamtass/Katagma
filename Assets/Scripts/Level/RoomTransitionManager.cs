using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Moves the player between rooms. When the player walks through an open door, this pans the
// camera to the next room, activates it, drops the player at the matching entrance, and
// deactivates the room they left. Only one room is active at a time, which keeps the game light.
public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance { get; private set; }

    [Header("Settings")]
    public float transitionDuration = 0.4f;   // how long the camera pan takes

    [Header("References")]
    public RoomController startingRoom;
    public Camera mainCamera;
    public PlayerMovement playerMovement;
    public Minimap minimap;

    public bool IsTransitioning { get; private set; }   // true during a pan, to block re-entry

    private RoomController currentRoom;

    // Standard singleton guard.
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // Fallback so a scene set up by hand (without the generator) still works for testing.
    void Start()
    {
        if (startingRoom != null)
            Initialize(startingRoom);
    }

    // Called by the FloorGenerator once the floor is built: remember the start room and snap
    // the camera onto it.
    public void Initialize(RoomController startRoom)
    {
        currentRoom = startRoom;
        mainCamera.transform.position = new Vector3(
            startRoom.transform.position.x,
            startRoom.transform.position.y,
            mainCamera.transform.position.z);
    }

    // Entry point called by a door when the player passes through it. Ignored if a transition
    // is already running.
    public void StartTransition(DoorTrigger.Direction direction, GameObject player)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionCoroutine(direction, player));
    }

    // The transition itself: freeze the player, find the target room, activate it, pan the
    // camera over, move the player to the matching entrance, deactivate the old room, and hand
    // control back. The player is repositioned after the pan so they're never seen in the wrong
    // spot mid-move.
    private IEnumerator TransitionCoroutine(DoorTrigger.Direction direction, GameObject player)
    {
        IsTransitioning = true;
        playerMovement.canMove = false;

        RoomController targetRoom = GetAdjacentRoom(direction);
        if (targetRoom == null)
        {
            // No room that way (shouldn't happen through an open door) — just undo the freeze.
            playerMovement.canMove = true;
            IsTransitioning = false;
            yield break;
        }

        // Tell the room where the player will enter so its enemies avoid spawning there.
        targetRoom.SetEntryPoint(GetOppositeSpawn(direction, targetRoom));

        // Turn the target room on before the pan so it's visible as the camera arrives.
        targetRoom.gameObject.SetActive(true);

        // Pan from the current room's centre to the target room's centre.
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
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);   // eased pan
            mainCamera.transform.position = Vector3.Lerp(camStart, camEnd, t);
            yield return null;
        }
        mainCamera.transform.position = camEnd;

        // Place the player at the entrance opposite the door they used.
        Transform spawnPoint = GetOppositeSpawn(direction, targetRoom);
        if (spawnPoint != null)
            player.transform.position = spawnPoint.position;

        // Turn off the room we just left.
        currentRoom.gameObject.SetActive(false);

        currentRoom = targetRoom;
        minimap?.EnterRoom(targetRoom);
        playerMovement.canMove = true;
        IsTransitioning = false;
    }

    // Returns the room in the given direction from the current one, or null if there isn't one.
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

    // Works out which entrance the player should appear at: exiting through the top door means
    // entering the next room from its bottom, and so on.
    private Transform GetOppositeSpawn(DoorTrigger.Direction dir, RoomController room)
    {
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
