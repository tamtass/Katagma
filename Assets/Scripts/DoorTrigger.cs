using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public enum Direction { Top, Bottom, Left, Right }
    public Direction doorDirection;

    private Door door;

    void Awake()
    {
        door = GetComponentInParent<Door>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (door == null || !door.IsOpen) return;
        if (RoomTransitionManager.Instance == null || RoomTransitionManager.Instance.IsTransitioning) return;

        RoomTransitionManager.Instance.StartTransition(doorDirection, other.gameObject);
    }
}
