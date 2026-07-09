using System.Collections;
using UnityEngine;

// Plays looping background music, crossfading between a menu track and a gameplay
// track. Persists across the whole session. Muting is handled globally by
// AudioListener.volume (the mute button), so this class doesn't touch it.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks")]
    public AudioClip menuTrack;
    public AudioClip gameplayTrack;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeDuration = 1f;

    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;
    private AudioClip   _currentClip;
    private Coroutine   _fade;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Two sources so we can crossfade one out while the other fades in.
        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _a, _b })
        {
            s.loop        = true;
            s.playOnAwake = false;
            s.volume      = 0f;
        }
        _active = _a;
    }

    public void PlayMenu()     => Play(menuTrack);
    public void PlayGameplay() => Play(gameplayTrack);

    private void Play(AudioClip clip)
    {
        if (clip == null || clip == _currentClip) return;   // already on this track
        _currentClip = clip;

        AudioSource next = _active == _a ? _b : _a;
        next.clip   = clip;
        next.volume = 0f;
        next.Play();

        if (_fade != null) StopCoroutine(_fade);
        _fade   = StartCoroutine(Crossfade(_active, next));
        _active = next;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float t         = 0f;
        float startFrom = from.volume;

        // Unscaled so the fade keeps running while the game is paused (timeScale 0).
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k     = fadeDuration <= 0f ? 1f : t / fadeDuration;
            to.volume   = Mathf.Lerp(0f, volume, k);
            from.volume = Mathf.Lerp(startFrom, 0f, k);
            yield return null;
        }

        to.volume   = volume;
        from.volume = 0f;
        from.Stop();
    }
}
