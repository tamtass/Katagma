using System.Collections.Generic;
using UnityEngine;

// The central controller of the whole game. It owns the high-level state (menu,
// running, paused, dead), holds the run statistics, and drives every screen transition:
// starting a run, advancing floors, pausing, dying, winning, and returning to the menu.
// Most other scripts talk to it through the static Instance.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }   // global access point

    public GameObject      mainMenuUI;        // the main menu screen root
    public GameObject      gameSystem;        // parent of all in-game objects, toggled on during a run
    public GameObject      pauseMenuUI;       // the pause overlay
    public GameObject      playerPrefab;      // the player, spawned fresh each run
    public FloorGenerator  floorGenerator;    // builds and clears the dungeon floors

    [Header("Items")]
    public GameObject[] itemPrefabs;          // the full pool of item-room items for a run

    [Header("UI Screens")]
    public GameOverScreen          gameOverScreen;          // shown on death and on winning
    public StoryProgressionScreen  storyProgressionScreen;  // lore screen shown after a win
    public GameObject              optionsMenuUI;
    public GameObject              leaderboardUI;

    // Run statistics. Private setters so only the GameManager changes them; everything
    // else (stats screen, score submission) just reads them.
    public bool  IsGameRunning { get; private set; }   // true while a run is active
    public bool  IsPaused      { get; private set; }
    public bool  IsPlayerDead  { get; private set; }
    public int   Score         { get; private set; }
    public float ElapsedTime   { get; private set; }   // seconds since the run started
    public int   CurrentFloor  { get; private set; }   // 1-based, for display
    public int   EnemiesKilled { get; private set; }
    public int   RoomsCleared  { get; private set; }
    public int   FloorsCleared { get; private set; }

    // Mute is just the global audio volume being zero or one; the mute button reads/writes this.
    public bool IsGameMuted
    {
        get => AudioListener.volume == 0f;
        set => AudioListener.volume = value ? 0f : 1f;
    }

    // Small hooks other systems call to bump the run counters.
    public void OnEnemyKilled()        => EnemiesKilled++;
    public void OnCombatRoomCleared()  => RoomsCleared++;
    public void AddScore(int points)   => Score += points;

    // The items still available this run. Copied from itemPrefabs at the start of each
    // run and drawn from without replacement, so the same item can't appear twice.
    private List<GameObject> _remainingItems = new();

    // Pulls one random item out of the remaining pool and removes it. Returns null when
    // the pool is empty (item rooms just spawn nothing after that).
    public GameObject TakeRandomItem()
    {
        if (_remainingItems.Count == 0) return null;
        int index = Random.Range(0, _remainingItems.Count);
        GameObject prefab = _remainingItems[index];
        _remainingItems.RemoveAt(index);
        return prefab;
    }

    private float          _penaltyTimer;        // counts up to 1 second for the score decay
    private int            _currentFloorIndex;   // 0-based index into the floor list
    private PlayerMovement _playerInstance;      // the currently spawned player, if any

    // Sets up the singleton and makes sure every screen starts in the right state:
    // main menu visible, gameplay and all overlays hidden.
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

    // Fade the black overlay away on launch and start the menu music.
    void Start()
    {
        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.FadeFromBlack(1f);

        if (MusicManager.Instance != null) MusicManager.Instance.PlayMenu();
    }

    // Ticks the run timer and the slow score decay. Only runs while actually playing,
    // so paused/dead time doesn't count. The decay drops the score by 1 every second,
    // which rewards clearing floors quickly.
    void Update()
    {
        if (!IsGameRunning || IsPaused || IsPlayerDead) return;
        float dt = Time.deltaTime;
        ElapsedTime   += dt;
        _penaltyTimer += dt;
        if (_penaltyTimer >= 1f)
        {
            _penaltyTimer -= 1f;
            Score = Mathf.Max(0, Score - 1);   // never below zero
        }
    }

    // Begins a new run: switch to gameplay music, then (behind a fade to black) hide the
    // menu, spawn the player, and generate the first floor. The player is only allowed to
    // move once the fade finishes, so they don't act during the transition.
    public void StartGame()
    {
        if (MusicManager.Instance != null) MusicManager.Instance.PlayGameplay();

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
            // Fallback path if there's no transition screen wired up (mostly for testing).
            mainMenuUI.SetActive(false);
            gameSystem.SetActive(true);
            IsGameRunning = true;
            SpawnPlayer();
            floorGenerator.GenerateFloor(0);
            if (_playerInstance != null) _playerInstance.canMove = true;
        }
    }

    // Called when the player takes the exit door. Either moves to the next floor or, if
    // that was the last one, triggers the win screen. The rebuild happens while the screen
    // is black so the player never sees the old floor being torn down.
    public void AdvanceFloor()
    {
        FloorsCleared++;
        int nextIndex = _currentFloorIndex + 1;

        if (nextIndex >= floorGenerator.FloorCount)
        {
            // No floors left — the player has beaten the game.
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

    // Shows the end-of-run stats screen. Called for both outcomes: isWin true after
    // clearing the last floor, false on death. Expected to be called while the screen is
    // already black (from the death animation or a fade). Freezes the game and swaps back
    // to menu music since the run is over.
    public void ShowGameOver(bool isWin)
    {
        Time.timeScale = 0f;
        if (_playerInstance != null) _playerInstance.canMove = false;
        if (gameOverScreen  != null) gameOverScreen.Show(isWin);

        if (MusicManager.Instance != null) MusicManager.Instance.PlayMenu();
    }

    // Opens the lore/story screen shown after a win.
    public void ShowStoryScreen()
    {
        if (storyProgressionScreen == null) return;
        storyProgressionScreen.gameObject.SetActive(true);
        storyProgressionScreen.Show();
    }

    // The four menu navigation methods below all follow the same shape: fade to black,
    // swap which screen is active while hidden, then fade back. The plain else-branch is
    // a no-transition fallback.

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

    // Pause freezes time (timeScale 0) and shows the overlay. Ignored if the player is
    // already dead, since the death screen owns the frozen state at that point.
    public void PauseGame()
    {
        if (IsPlayerDead) return;
        IsPaused = true;
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
    }

    // Unfreeze and hide the pause overlay.
    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
    }

    // Leaves the current run and heads back to the menu, fading through black so the
    // teardown is hidden.
    public void ReturnToMainMenu()
    {
        IsPaused = false;

        if (TransitionScreen.Instance != null)
            TransitionScreen.Instance.Transition(0.5f, 1f,
                onBlack: CleanupAndShowMenu);
        else
            CleanupAndShowMenu();
    }

    // Tears down the run: reset state flags and time, stop menu music, hide every overlay,
    // clear the floor, destroy the player, and show the main menu again. Runs while the
    // screen is black.
    private void CleanupAndShowMenu()
    {
        IsPlayerDead  = false;
        IsGameRunning = false;
        Time.timeScale = 1f;

        if (MusicManager.Instance != null) MusicManager.Instance.PlayMenu();

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

    // Resets every run statistic to its starting value, refills the item pool, and spawns
    // a fresh player at the origin. The player starts frozen; StartGame unfreezes it once
    // the intro fade is done.
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
        _remainingItems    = new List<GameObject>(itemPrefabs);   // fresh copy of the pool

        if (_playerInstance != null) Destroy(_playerInstance.gameObject);

        GameObject go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        _playerInstance = go.GetComponent<PlayerMovement>();
        if (_playerInstance != null) _playerInstance.canMove = false;
    }

    // Quits the application (no effect in the editor, works in a build).
    public void QuitGame() => Application.Quit();
}
