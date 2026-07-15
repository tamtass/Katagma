using TMPro;
using UnityEngine;

// Shows the live score on the HUD. Just reads the score from the GameManager each frame.
public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Update()
    {
        if (GameManager.Instance != null)
            scoreText.text = $"Score: {GameManager.Instance.Score}";
    }
}
