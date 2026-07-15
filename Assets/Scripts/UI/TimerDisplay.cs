using TMPro;
using UnityEngine;

// Shows the run timer on the HUD, formatted as MM:SS (or HH:MM:SS once past an hour). Reads
// the elapsed time from the GameManager each frame.
public class TimerDisplay : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (GameManager.Instance == null) return;
        int total   = (int)GameManager.Instance.ElapsedTime;
        int hours   = total / 3600;
        int minutes = total % 3600 / 60;
        int seconds = total % 60;

        // Only show the hours field once there actually are hours, to keep it compact.
        timerText.text = hours > 0
            ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
            : $"{minutes:D2}:{seconds:D2}";
    }
}
