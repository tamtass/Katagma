using UnityEngine;

// Plays all the game's sound effects from one place. It holds a clip per event and fires them
// as one-shots through a single shared AudioSource. Muting is handled globally by the mute
// button (AudioListener.volume), so this class never touches volume beyond its own scale. It
// persists for the whole session as a singleton.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Gameplay")]
    public AudioClip playerAttack;   // one clip per game event, assigned in the inspector
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip enemyHurt;
    public AudioClip healPickup;
    public AudioClip itemPickup;

    [Header("UI")]
    public AudioClip buttonClick;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 1f;   // overall SFX volume scale

    private AudioSource _source;   // the shared source all one-shots play through

    // Singleton setup and a hidden AudioSource to fire clips from.
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    // Plays a clip once. Safe with a null clip (does nothing), so effects can be left unassigned
    // during development without errors. PlayOneShot isn't affected by Time.timeScale, so the
    // death and hit sounds still play even when the game freezes.
    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        _source.PlayOneShot(clip, volume);
    }

    // Named shortcuts so call sites read clearly (e.g. SoundManager.Instance.PlayPlayerHurt()).
    public void PlayPlayerAttack() => Play(playerAttack);
    public void PlayPlayerHurt()   => Play(playerHurt);
    public void PlayPlayerDeath()  => Play(playerDeath);
    public void PlayEnemyHurt()    => Play(enemyHurt);
    public void PlayHealPickup()   => Play(healPickup);
    public void PlayItemPickup()   => Play(itemPickup);
    public void PlayButtonClick()  => Play(buttonClick);
}
