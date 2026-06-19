using UnityEngine;

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
