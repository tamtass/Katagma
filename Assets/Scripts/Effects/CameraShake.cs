using UnityEngine;

// Screen shake for the main camera. It's designed to coexist with the room-transition pan, which
// also writes the camera position: rather than remembering a "home" spot, it applies a small
// random offset as a pure overlay. Each frame it undoes last frame's offset before adding a fresh
// one, so the shake never leaks into the camera's real position, and when it ends the camera is
// left exactly where the pan put it.
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Defaults (used by Shake())")]
    public float duration  = 0.25f;   // how long a shake lasts, in seconds
    public float magnitude = 0.15f;   // maximum offset, in world units
    [Tooltip("How the shake decays over its lifetime (x = normalized time, y = strength).")]
    public AnimationCurve falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);   // full strength -> 0

    private float   _timer;             // time left in the current shake
    private float   _activeDuration;    // duration of the current shake
    private float   _activeMagnitude;   // strength of the current shake
    private Vector3 _lastOffset;        // the offset applied last frame, removed at the start of this one

    // Standard singleton guard (on the camera).
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Trigger a shake with the inspector defaults (what PlayerMovement calls on taking damage).
    public void Shake() => Shake(duration, magnitude);

    // Trigger a shake with explicit values. If one is already running, restart the timer but keep
    // whichever magnitude is stronger, so rapid hits don't feel weaker than a single one.
    public void Shake(float shakeDuration, float shakeMagnitude)
    {
        _activeDuration  = shakeDuration;
        _activeMagnitude = _timer > 0f ? Mathf.Max(_activeMagnitude, shakeMagnitude) : shakeMagnitude;
        _timer           = shakeDuration;
    }

    // Runs after everything else has moved the camera for the frame. Undo last frame's offset,
    // then, if a shake is active, compute a new decaying random offset and apply it. Unscaled
    // time so it keeps resolving even if a killing blow has frozen the game.
    void LateUpdate()
    {
        transform.position -= _lastOffset;
        _lastOffset = Vector3.zero;

        if (_timer <= 0f) return;

        _timer -= Time.unscaledDeltaTime;
        if (_timer <= 0f) return;

        // Strength follows the falloff curve over the shake's life; direction is random.
        float strength = falloff.Evaluate(1f - _timer / _activeDuration) * _activeMagnitude;
        Vector2 random = Random.insideUnitCircle * strength;
        _lastOffset = new Vector3(random.x, random.y, 0f);
        transform.position += _lastOffset;
    }
}
