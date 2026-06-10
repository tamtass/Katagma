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

    public bool IsGameRunning { get { return _isGameRunning; } private set { _isGameRunning = value; } }
    public bool IsPaused { get; private set; }

    private PlayerMovement playerInstance;

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
        if (TransitionScreen.Instance != null)
        {
            TransitionScreen.Instance.ShowFloor(1,
                onBlack: () =>
                {
                    mainMenuUI.SetActive(false);
                    gameSystem.SetActive(true);
                    pauseMenuUI.SetActive(false);
                    IsGameRunning = true;
                    SpawnPlayer();
                    floorGenerator.GenerateFloor();
                },
                onComplete: () => { if (playerInstance != null) playerInstance.canMove = true; });
        }
        else
        {
            mainMenuUI.SetActive(false);
            gameSystem.SetActive(true);
            IsGameRunning = true;
            SpawnPlayer();
            floorGenerator.GenerateFloor();
            if (playerInstance != null) playerInstance.canMove = true;
        }
    }

    private void SpawnPlayer()
    {
        if (playerInstance != null)
            Destroy(playerInstance.gameObject);

        GameObject go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerInstance = go.GetComponent<PlayerMovement>();
        if (playerInstance != null)
            playerInstance.canMove = false;
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        floorGenerator.ClearFloor();

        if (playerInstance != null)
        {
            Destroy(playerInstance.gameObject);
            playerInstance = null;
        }

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
