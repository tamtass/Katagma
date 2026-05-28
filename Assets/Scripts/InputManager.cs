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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (GameManager.Instance.pauseMenuUI.activeSelf)
                {
                    GameManager.Instance.ResumeGame();
                }
                else
                {
                    GameManager.Instance.PauseGame();
                }
            }
        }
    }
}