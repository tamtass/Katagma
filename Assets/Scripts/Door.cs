using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject openState;
    public GameObject closedState;

    public bool IsOpen { get; private set; } = false;

    public void OpenDoor()
    {
        IsOpen = true;
        if (openState != null) openState.SetActive(true);
        if (closedState != null) closedState.SetActive(false);
    }

    public void CloseDoor()
    {
        IsOpen = false;
        if (openState != null) openState.SetActive(false);
        if (closedState != null) closedState.SetActive(true);
    }
}
