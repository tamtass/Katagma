using System.Collections;
using UnityEngine;

// Plays the looping background music and crossfades between two tracks: one for the menu and one
// for active gameplay. It keeps two AudioSources so it can fade one out while fading the other
// in, giving smooth changes rather than a hard cut. Muting is handled globally by the mute
// button (AudioListener.volume), so this class doesn't touch it.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks")]
    public AudioClip menuTrack;       // plays on the menu and other non-gameplay screens
    public AudioClip gameplayTrack;   // plays during a run

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.5f;   // target music volume
    public float fadeDuration = 1f;               // length of a crossfade

    private AudioSource _a;            // the two sources used for crossfading
    private AudioSource _b;
    private AudioSource _active;       // whichever one is currently playing
    private AudioClip   _currentClip;  // the track currently playing, to avoid restarting it
    private Coroutine   _fade;         // the running crossfade, so a new one can cancel it

    // Singleton setup plus two looping, silent AudioSources to crossfade between.
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

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

    // The two public switches, called by the GameManager at the right moments.
    public void PlayMenu()     => Play(menuTrack);
    public void PlayGameplay() => Play(gameplayTrack);

    // Crossfades to a new track. Does nothing if that track is already playing, so it's safe to
    // call repeatedly. It starts the incoming track on the idle source and fades between them.
    private void Play(AudioClip clip)
    {
        if (clip == null || clip == _currentClip) return;
        _currentClip = clip;

        AudioSource next = _active == _a ? _b : _a;   // the idle source becomes the new active one
        next.clip   = clip;
        next.volume = 0f;
        next.Play();

        if (_fade != null) StopCoroutine(_fade);
        _fade   = StartCoroutine(Crossfade(_active, next));
        _active = next;
    }

    // Fades one source's volume down to zero while bringing the other up to the target volume,
    // then stops the outgoing one. Uses unscaled time so the fade continues even while the game
    // is paused (timeScale 0).
    private IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float t         = 0f;
        float startFrom = from.volume;

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
