using TMPro;
using UnityEngine;

// One row in the leaderboard list: rank, name, and score. Instantiated per entry by the
// leaderboard screen and filled in via SetData.
public class LeaderboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    // Fills the row's labels from a fetched entry and its position in the list.
    public void SetData(int rank, LeaderboardEntry entry)
    {
        if (rankText  != null) rankText.text  = $"#{rank}";
        if (nameText  != null) nameText.text  = entry.name;
        if (scoreText != null) scoreText.text = entry.score.ToString();
    }
}
