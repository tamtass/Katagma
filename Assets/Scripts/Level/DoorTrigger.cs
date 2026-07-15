using UnityEngine;

// The trigger zone sitting in a doorway. When the player walks into it and the door is open,
// it kicks off the room transition in this door's direction. Lives on a child of the Door.
public class DoorTrigger : MonoBehaviour
{
    public enum Direction { Top, Bottom, Left, Right }
    public Direction doorDirection;   // which way this door leads

    private Door door;   // the parent door, checked for being open

    // Find the door this trigger belongs to.
    void Awake()
    {
        door = GetComponentInParent<Door>();
    }

    // When the player enters, and the door is open and no transition is already running, start
    // moving to the next room.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (door == null || !door.IsOpen) return;
        if (RoomTransitionManager.Instance == null || RoomTransitionManager.Instance.IsTransitioning) return;

        RoomTransitionManager.Instance.StartTransition(doorDirection, other.gameObject);
    }
}
