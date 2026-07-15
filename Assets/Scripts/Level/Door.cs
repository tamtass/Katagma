using UnityEngine;

// A door between two rooms. It's really just two visuals — an open sprite and a closed one —
// swapped by the room controller. IsOpen also gates whether walking into it triggers a room
// transition (see DoorTrigger).
public class Door : MonoBehaviour
{
    public GameObject openState;     // shown when the door is open
    public GameObject closedState;   // shown when the door is closed

    public bool IsOpen { get; private set; } = false;

    // Open the door: show the open visual, hide the closed one.
    public void OpenDoor()
    {
        IsOpen = true;
        if (openState != null) openState.SetActive(true);
        if (closedState != null) closedState.SetActive(false);
    }

    // Close the door: show the closed visual, hide the open one.
    public void CloseDoor()
    {
        IsOpen = false;
        if (openState != null) openState.SetActive(false);
        if (closedState != null) closedState.SetActive(true);
    }
}
