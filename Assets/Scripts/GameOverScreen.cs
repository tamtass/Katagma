using TMPro;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    [Header("Win / Lose")]
    public GameObject winTitle;
    public GameObject loseTitle;
    public GameObject winImage;
    public GameObject loseImage;

    [Header("Stats")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI enemiesKilledText;
    public TextMeshProUGUI roomsClearedText;
    public TextMeshProUGUI floorsText;

    private bool _isWin;

    public void Show(bool isWin)
    {
        _isWin = isWin;
        gameObject.SetActive(true);

        if (winTitle  != null) winTitle.SetActive(isWin);
        if (loseTitle != null) loseTitle.SetActive(!isWin);
        if (winImage  != null) winImage.SetActive(isWin);
        if (loseImage != null) loseImage.SetActive(!isWin);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (scoreText          != null) scoreText.text          = gm.Score.ToString();
            if (enemiesKilledText  != null) enemiesKilledText.text  = gm.EnemiesKilled.ToString();
            if (roomsClearedText   != null) roomsClearedText.text   = gm.RoomsCleared.ToString();
            if (floorsText         != null) floorsText.text         = gm.FloorsCleared.ToString();
            if (timeText != null)
            {
                int total   = (int)gm.ElapsedTime;
                int hours   = total / 3600;
                int minutes = total % 3600 / 60;
                int seconds = total % 60;
                timeText.text = hours > 0
                    ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
                    : $"{minutes:D2}:{seconds:D2}";
            }
        }
    }

    public void OnContinueClicked()
    {
        if (_isWin)
        {
            if (TransitionScreen.Instance != null)
                TransitionScreen.Instance.Transition(0.5f, 1f, onBlack: () =>
                {
                    gameObject.SetActive(false);
                    if (GameManager.Instance != null) GameManager.Instance.ShowStoryScreen();
                });
            else
            {
                gameObject.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.ShowStoryScreen();
            }
        }
        else
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
        }
    }
}
