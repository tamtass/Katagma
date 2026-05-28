using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject mainMenuUI;
    public GameObject gameSystem;
    public GameObject pauseMenuUI;
    public GameObject playerPrefab;
    public FloorGenerator floorGenerator;

    private bool _isGameRunning = false;

    public bool IsGameRunning {get { return _isGameRunning; } private set { _isGameRunning = value; }}

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            mainMenuUI.SetActive(true);
            gameSystem.SetActive(false);
            pauseMenuUI.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.FadeFromBlack(1f);
    }

    public void StartGame()
    {
        if (!playerPrefab.TryGetComponent<PlayerMovement>(out var pm))
            return;

        pm.canMove = false;

        if (TransitionScreen.Instance != null)
        {
            TransitionScreen.Instance.ShowFloor(1,
                onBlack: () =>
                {
                    mainMenuUI.SetActive(false);
                    gameSystem.SetActive(true);
                    pauseMenuUI.SetActive(false);
                    IsGameRunning = true;
                    floorGenerator.GenerateFloor();
                    playerPrefab.transform.position = Vector3.zero;
                },
                onComplete: () => pm.canMove = true);
        }
        else
        {
            mainMenuUI.SetActive(false);
            gameSystem.SetActive(true);
            IsGameRunning = true;
            floorGenerator.GenerateFloor();
            playerPrefab.transform.position = Vector3.zero;
            pm.canMove = true;
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        floorGenerator.ClearFloor();
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.FadeFromBlack(1f);
        mainMenuUI.SetActive(true);
        gameSystem.SetActive(false);
        pauseMenuUI.SetActive(false);
        IsGameRunning = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
