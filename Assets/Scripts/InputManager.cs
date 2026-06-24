using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (GameManager.Instance.optionsMenuUI != null &&
            GameManager.Instance.optionsMenuUI.activeSelf)
        {
            GameManager.Instance.CloseOptions();
        }
        else if (GameManager.Instance.leaderboardUI != null &&
                 GameManager.Instance.leaderboardUI.activeSelf)
        {
            GameManager.Instance.CloseLeaderboard();
        }
        else if (GameManager.Instance.IsGameRunning)
        {
            if (GameManager.Instance.pauseMenuUI.activeSelf)
                GameManager.Instance.ResumeGame();
            else
                GameManager.Instance.PauseGame();
        }
    }
}