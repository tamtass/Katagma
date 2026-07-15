using UnityEngine;

// The exit that appears in the boss room after the boss is beaten. Walking into it advances to
// the next floor (or wins the game if it was the last one). Guarded so it only fires once.
public class ExitDoor : MonoBehaviour
{
    private bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        if (GameManager.Instance != null) GameManager.Instance.AdvanceFloor();
    }
}
