using System.Collections;
using UnityEngine;

// A brief freeze-frame (hit stop) on impact: time is stopped, or slowed, for a short real-time
// window and then restored, which gives hits a satisfying sense of weight. The tricky part is
// that the game already drives Time.timeScale elsewhere (pause, death, the win screen), so this
// carefully refuses to run, or to restore normal time, if any of those already own the timescale.
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    [Header("Freeze Frame")]
    [Tooltip("How long the freeze lasts, in real (unscaled) seconds.")]
    public float duration = 0.06f;
    [Range(0f, 1f)]
    [Tooltip("Time scale held during the freeze (0 = a full stop, higher = slow-mo).")]
    public float timeScaleDuringFreeze = 0f;

    private Coroutine _routine;   // the running freeze, so a new hit can restart it

    // Standard singleton guard.
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Freeze using the inspector default duration (what PlayerMovement calls when a hit lands).
    public void Freeze() => Freeze(duration);

    // Freeze for a specific duration. Skipped if the game is already halted, and re-triggering
    // restarts the single coroutine rather than stacking freezes.
    public void Freeze(float freezeDuration)
    {
        if (IsGameHalted()) return;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FreezeRoutine(freezeDuration));
    }

    // Sets the freeze timescale, waits out the window in real time (so it elapses while scaled
    // time is near-stopped), then returns to normal — but only if nothing else grabbed the
    // timescale during the freeze (e.g. the player died or paused mid-hit).
    private IEnumerator FreezeRoutine(float freezeDuration)
    {
        Time.timeScale = timeScaleDuringFreeze;

        yield return new WaitForSecondsRealtime(freezeDuration);

        if (!IsGameHalted()) Time.timeScale = 1f;

        _routine = null;
    }

    // True when some other system already owns a frozen game (paused, dead, or not in a run),
    // meaning hit-stop should keep its hands off the timescale.
    private static bool IsGameHalted()
    {
        var gm = GameManager.Instance;
        return gm != null && (!gm.IsGameRunning || gm.IsPaused || gm.IsPlayerDead);
    }
}
