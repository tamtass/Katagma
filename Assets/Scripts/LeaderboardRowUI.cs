using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    public void SetData(int rank, LeaderboardEntry entry)
    {
        if (rankText  != null) rankText.text  = $"#{rank}";
        if (nameText  != null) nameText.text  = entry.name;
        if (scoreText != null) scoreText.text = entry.score.ToString();
    }
}
