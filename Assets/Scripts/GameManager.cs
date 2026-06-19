using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject mainMenuUI;
    public GameObject gameSystem;
    public GameObject pauseMenuUI;
    public GameObject playerPrefab;
    public FloorGenerator floorGenerator;

    [Header("Items")]
    public GameObject[] itemPrefabs;

    [Header("UI")]
    public GameOverScreen gameOverScreen;

    private List<GameObject> _remainingItems = new();

    private bool _isGameRunning = false;

    public bool IsGameRunning { get { return _isGameRunning; } private set { _isGameRunning = value; } }
    public bool IsPaused { get; private set; }
    public bool IsPlayerDead { get; private set; }
    public int Score { get; private set; }
    public float ElapsedTime { get; private set; }
    public int CurrentFloor { get; private set; }
    public int EnemiesKilled { get; private set; }
    public int RoomsCleared { get; private set; }
    public int FloorsCleared { get; private set; }

    private int _currentFloorIndex;

    public void OnPlayerDied() => IsPlayerDead = true;
    public void OnEnemyKilled() => EnemiesKilled++;
    public void OnCombatRoomCleared() => RoomsCleared++;

    public void AddScore(int points) => Score += points;

    public void ShowGameOver(bool isWin)
    {
        Time.timeScale = 0f;
        if (playerInstance != null) playerInstance.canMove = false;
        if (gameOverScreen != null) gameOverScreen.Show(isWin);
    }

    public GameObject TakeRandomItem()
    {
        if (_remainingItems.Count == 0) return null;
        int index = Random.Range(0, _remainingItems.Count);
        GameObject prefab = _remainingItems[index];
        _remainingItems.RemoveAt(index);
        return prefab;
    }

    public void AdvanceFloor()
    {
        FloorsCleared++;

        int nextIndex = _currentFloorIndex + 1;
        if (nextIndex >= floorGenerator.FloorCount)
        {
            ShowGameOver(true);
            return;
        }

        _currentFloorIndex = nextIndex;
        CurrentFloor       = nextIndex + 1;

        if (playerInstance != null) playerInstance.canMove = false;

        if (TransitionScreen.Instance != null)
        {
            TransitionScreen.Instance.ShowFloor(
                onBlack: () =>
                {
                    floorGenerator.ClearFloor();
                    floorGenerator.GenerateFloor(_currentFloorIndex);
                    if (playerInstance != null)
                        playerInstance.transform.position = Vector3.zero;
                },
                onComplete: () =>
                {
                    if (playerInstance != null) playerInstance.canMove = true;
                });
        }
        else
        {
            floorGenerator.ClearFloor();
            floorGenerator.GenerateFloor(_currentFloorIndex);
            if (playerInstance != null)
            {
                playerInstance.transform.position = Vector3.zero;
                playerInstance.canMove = true;
            }
        }
    }

    private float _penaltyTimer;
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
            TransitionScreen.Instance.ShowFloor(
                onBlack: () =>
                {
                    mainMenuUI.SetActive(false);
                    gameSystem.SetActive(true);
                    pauseMenuUI.SetActive(false);
                    IsGameRunning = true;
                    SpawnPlayer();
                    floorGenerator.GenerateFloor(0);
                },
                onComplete: () => { if (playerInstance != null) playerInstance.canMove = true; });
        }
        else
        {
            mainMenuUI.SetActive(false);
            gameSystem.SetActive(true);
            IsGameRunning = true;
            SpawnPlayer();
            floorGenerator.GenerateFloor(0);
            if (playerInstance != null) playerInstance.canMove = true;
        }
    }

    void Update()
    {
        if (!IsGameRunning || IsPaused || IsPlayerDead) return;
        float dt = Time.deltaTime;
        ElapsedTime   += dt;
        _penaltyTimer += dt;
        if (_penaltyTimer >= 1f)
        {
            _penaltyTimer -= 1f;
            Score = Mathf.Max(0, Score - 1);
        }
    }

    private void SpawnPlayer()
    {
        IsPlayerDead       = false;
        Score              = 0;
        ElapsedTime        = 0f;
        _penaltyTimer      = 0f;
        _currentFloorIndex = 0;
        CurrentFloor       = 1;
        EnemiesKilled      = 0;
        RoomsCleared       = 0;
        FloorsCleared      = 0;
        _remainingItems    = new List<GameObject>(itemPrefabs);

        if (playerInstance != null)
            Destroy(playerInstance.gameObject);

        GameObject go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerInstance = go.GetComponent<PlayerMovement>();
        if (playerInstance != null)
            playerInstance.canMove = false;
    }

    public void PauseGame()
    {
        if (IsPlayerDead) return;
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
        IsPlayerDead = false;
        Time.timeScale = 1f;
        if (gameOverScreen != null) gameOverScreen.gameObject.SetActive(false);
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
