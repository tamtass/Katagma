using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject      mainMenuUI;
    public GameObject      gameSystem;
    public GameObject      pauseMenuUI;
    public GameObject      playerPrefab;
    public FloorGenerator  floorGenerator;

    [Header("Items")]
    public GameObject[] itemPrefabs;

    [Header("UI Screens")]
    public GameOverScreen          gameOverScreen;
    public StoryProgressionScreen  storyProgressionScreen;
    public GameObject              optionsMenuUI;
    public GameObject              leaderboardUI;

    // ── Stats ────────────────────────────────────────────────────────────────
    public bool  IsGameRunning { get; private set; }
    public bool  IsPaused      { get; private set; }
    public bool  IsPlayerDead  { get; private set; }
    public int   Score         { get; private set; }
    public float ElapsedTime   { get; private set; }
    public int   CurrentFloor  { get; private set; }
    public int   EnemiesKilled { get; private set; }
    public int   RoomsCleared  { get; private set; }
    public int   FloorsCleared { get; private set; }

    public bool IsGameMuted
    {
        get => AudioListener.volume == 0f;
        set => AudioListener.volume = value ? 0f : 1f;
    }

    public void OnEnemyKilled()        => EnemiesKilled++;
    public void OnCombatRoomCleared()  => RoomsCleared++;
    public void AddScore(int points)   => Score += points;

    // ── Item pool ────────────────────────────────────────────────────────────
    private List<GameObject> _remainingItems = new();

    public GameObject TakeRandomItem()
    {
        if (_remainingItems.Count == 0) return null;
        int index = Random.Range(0, _remainingItems.Count);
        GameObject prefab = _remainingItems[index];
        _remainingItems.RemoveAt(index);
        return prefab;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private float          _penaltyTimer;
    private int            _currentFloorIndex;
    private PlayerMovement _playerInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            mainMenuUI.SetActive(true);
            gameSystem.SetActive(false);
            pauseMenuUI.SetActive(false);
            if (gameOverScreen         != null) gameOverScreen.gameObject.SetActive(false);
            if (storyProgressionScreen != null) storyProgressionScreen.gameObject.SetActive(false);
            if (optionsMenuUI          != null) optionsMenuUI.SetActive(false);
            if (leaderboardUI          != null) leaderboardUI.SetActive(false);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.FadeFromBlack(1f);
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

    // ── Game flow ────────────────────────────────────────────────────────────

    public void StartGame()
    {
        if (TransitionScreen.Instance != null)
        {
            TransitionScreen.Instance.ShowFloor("Floor 1",
                onBlack: () =>
                {
                    mainMenuUI.SetActive(false);
                    gameSystem.SetActive(true);
                    IsGameRunning = true;
                    SpawnPlayer();
                    floorGenerator.GenerateFloor(0);
                },
                onComplete: () =>
                {
                    if (_playerInstance != null) _playerInstance.canMove = true;
                });
        }
        else
        {
            mainMenuUI.SetActive(false);
            gameSystem.SetActive(true);
            IsGameRunning = true;
            SpawnPlayer();
            floorGenerator.GenerateFloor(0);
            if (_playerInstance != null) _playerInstance.canMove = true;
        }
    }

    public void AdvanceFloor()
    {
        FloorsCleared++;
        int nextIndex = _currentFloorIndex + 1;

        if (nextIndex >= floorGenerator.FloorCount)
        {
            // Last floor completed — show win screen
            Time.timeScale = 0f;
            if (_playerInstance != null) _playerInstance.canMove = false;

            if (TransitionScreen.Instance != null)
                TransitionScreen.Instance.Transition(0.5f, 1f,
                    onBlack: () => ShowGameOver(true));
            else
                ShowGameOver(true);
            return;
        }

        _currentFloorIndex = nextIndex;
        CurrentFloor       = nextIndex + 1;
        if (_playerInstance != null) _playerInstance.canMove = false;

        if (TransitionScreen.Instance != null)
        {
            TransitionScreen.Instance.ShowFloor($"Floor {CurrentFloor}",
                onBlack: () =>
                {
                    floorGenerator.ClearFloor();
                    floorGenerator.GenerateFloor(_currentFloorIndex);
                    if (_playerInstance != null) _playerInstance.transform.position = Vector3.zero;
                },
                onComplete: () =>
                {
                    if (_playerInstance != null) _playerInstance.canMove = true;
                });
        }
        else
        {
            floorGenerator.ClearFloor();
            floorGenerator.GenerateFloor(_currentFloorIndex);
            if (_playerInstance != null)
            {
                _playerInstance.transform.position = Vector3.zero;
                _playerInstance.canMove = true;
            }
        }
    }

    // Called while the screen is already black (from ShowDeath or FadeToBlack callbacks)
    public void ShowGameOver(bool isWin)
    {
        Time.timeScale = 0f;
        if (_playerInstance != null) _playerInstance.canMove = false;
        if (gameOverScreen  != null) gameOverScreen.Show(isWin);
    }

    public void ShowStoryScreen()
    {
        if (storyProgressionScreen == null) return;
        storyProgressionScreen.gameObject.SetActive(true);
        storyProgressionScreen.Show();
    }

    public void OpenOptions()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.Transition(0.5f, 0.5f, onBlack: () =>
            {
                mainMenuUI.SetActive(false);
                if (optionsMenuUI != null) optionsMenuUI.SetActive(true);
            });
        else
        {
            mainMenuUI.SetActive(false);
            if (optionsMenuUI != null) optionsMenuUI.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.Transition(0.5f, 0.5f, onBlack: () =>
            {
                if (optionsMenuUI != null) optionsMenuUI.SetActive(false);
                mainMenuUI.SetActive(true);
            });
        else
        {
            if (optionsMenuUI != null) optionsMenuUI.SetActive(false);
            mainMenuUI.SetActive(true);
        }
    }

    public void OpenLeaderboard()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.Transition(0.5f, 0.5f, onBlack: () =>
            {
                mainMenuUI.SetActive(false);
                if (leaderboardUI != null) leaderboardUI.SetActive(true);
            });
        else
        {
            mainMenuUI.SetActive(false);
            if (leaderboardUI != null) leaderboardUI.SetActive(true);
        }
    }

    public void CloseLeaderboard()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.Transition(0.5f, 0.5f, onBlack: () =>
            {
                if (leaderboardUI != null) leaderboardUI.SetActive(false);
                mainMenuUI.SetActive(true);
            });
        else
        {
            if (leaderboardUI != null) leaderboardUI.SetActive(false);
            mainMenuUI.SetActive(true);
        }
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

        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.Transition(0.5f, 1f,
                onBlack: CleanupAndShowMenu);
        else
            CleanupAndShowMenu();
    }

    private void CleanupAndShowMenu()
    {
        IsPlayerDead  = false;
        IsGameRunning = false;
        Time.timeScale = 1f;

        if (gameOverScreen         != null) gameOverScreen.gameObject.SetActive(false);
        if (storyProgressionScreen != null) storyProgressionScreen.gameObject.SetActive(false);
        if (pauseMenuUI            != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI          != null) optionsMenuUI.SetActive(false);
        if (leaderboardUI          != null) leaderboardUI.SetActive(false);

        floorGenerator.ClearFloor();

        if (_playerInstance != null)
        {
            Destroy(_playerInstance.gameObject);
            _playerInstance = null;
        }

        mainMenuUI.SetActive(true);
        gameSystem.SetActive(false);
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

        if (_playerInstance != null) Destroy(_playerInstance.gameObject);

        GameObject go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        _playerInstance = go.GetComponent<PlayerMovement>();
        if (_playerInstance != null) _playerInstance.canMove = false;
    }

    public void QuitGame() => Application.Quit();
}
