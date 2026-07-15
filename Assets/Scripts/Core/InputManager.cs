using UnityEngine;
using UnityEngine.InputSystem;

// Handles the global Escape key. It's kept separate from player movement input so it
// still works when the player is dead or a menu is open. Everything else (WASD, mouse)
// is read directly in PlayerMovement.
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }   // simple global access point

    // Standard singleton guard — keep the first one, destroy any duplicate.
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

    // Every frame, check for an Escape press and decide what it should close/toggle.
    // The order matters: an open menu takes priority over the in-game pause, so Escape
    // backs out of options/leaderboard first, and only pauses the game if neither is open.
    void Update()
    {
        if (GameManager.Instance == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // Options menu open -> Escape closes it back to the main menu.
        if (GameManager.Instance.optionsMenuUI != null &&
            GameManager.Instance.optionsMenuUI.activeSelf)
        {
            GameManager.Instance.CloseOptions();
        }
        // Leaderboard open -> Escape closes it back to the main menu.
        else if (GameManager.Instance.leaderboardUI != null &&
                 GameManager.Instance.leaderboardUI.activeSelf)
        {
            GameManager.Instance.CloseLeaderboard();
        }
        // Otherwise, if we're mid-run, Escape toggles the pause menu.
        else if (GameManager.Instance.IsGameRunning)
        {
            if (GameManager.Instance.pauseMenuUI.activeSelf)
                GameManager.Instance.ResumeGame();
            else
                GameManager.Instance.PauseGame();
        }
    }
}
