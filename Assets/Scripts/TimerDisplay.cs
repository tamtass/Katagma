using TMPro;
using UnityEngine;

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

        timerText.text = hours > 0
            ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
            : $"{minutes:D2}:{seconds:D2}";
    }
}
