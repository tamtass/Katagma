using System;
using System.Collections;
using TMPro;
using UnityEngine;

// The full-screen black overlay used for every fade in the game. It centralises three kinds of
// transition: a plain screen swap, the death sequence (with the "FRACTURED" text), and the floor
// intro (with the floor title). Each works the same way — fade to black, run a callback while
// hidden to swap what's on screen, then fade back — so the player never sees things change. All
// timing runs on unscaled time so fades still play while the game is frozen.
[RequireComponent(typeof(CanvasGroup))]
public class TransitionScreen : MonoBehaviour
{
    public static TransitionScreen Instance { get; private set; }

    [Header("References")]
    public TextMeshProUGUI fracturedText;   // the "FRACTURED" death caption
    public TextMeshProUGUI floorText;       // the floor title (e.g. "Floor 2")

    [Header("Death Timing")]
    // Durations for each stage of the death sequence.
    public float deathFadeIn        = 2f;
    public float deathTextReveal    = 0.5f;
    public float deathHold          = 1f;
    public float deathTextFadeOut   = 0.3f;
    public float deathFadeOut       = 1f;

    [Header("Floor Timing")]
    // Durations for each stage of the floor-intro sequence.
    public float floorFadeIn        = 0.6f;
    public float floorTextReveal    = 0.5f;
    public float floorHold          = 1.2f;
    public float floorTextFadeOut   = 0.3f;
    public float floorFadeOut       = 0.6f;
    public float floorTextStartScale = 0.8f;   // the floor title scales up from this as it fades in

    private CanvasGroup _canvasGroup;   // drives the overlay's alpha
    private Coroutine   _current;       // the running sequence, so a new one can cancel it

    // Singleton setup; start fully transparent and non-blocking with both captions hidden.
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha          = 0f;
        _canvasGroup.blocksRaycasts = false;
        SetAlpha(fracturedText, 0f);
        SetAlpha(floorText,     0f);
    }

    // Used once at launch: the screen starts black, then fades to reveal the menu.
    public void FadeFromBlack(float duration)
    {
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
        Run(Fade(1f, 0f, duration, null));
    }

    // Generic screen swap: fade out, run onBlack to change screens, fade back in, then onComplete.
    public void Transition(float outDuration, float inDuration,
                           Action onBlack = null, Action onComplete = null)
        => Run(TransitionSequence(outDuration, inDuration, onBlack, onComplete));

    // The death transition, with the FRACTURED text. onBlack fires while black (to show game over).
    public void ShowDeath(Action onBlack = null, Action onComplete = null)
        => Run(DeathSequence(onBlack, onComplete));

    // The floor-intro transition, showing the given floor label. onBlack fires while black (to
    // build the new floor).
    public void ShowFloor(string label = null,
                          Action onBlack = null, Action onComplete = null)
    {
        if (floorText != null && label != null) floorText.text = label;
        Run(FloorSequence(onBlack, onComplete));
    }

    // Plain swap: to black, callback, from black, done.
    private IEnumerator TransitionSequence(float outDuration, float inDuration,
                                           Action onBlack, Action onComplete)
    {
        yield return Fade(0f, 1f, outDuration, null);
        onBlack?.Invoke();
        yield return Fade(1f, 0f, inDuration, null);
        onComplete?.Invoke();
    }

    // Death: fade to black, show game over underneath, reveal then hide the FRACTURED text, and
    // fade back to reveal the game-over screen.
    private IEnumerator DeathSequence(Action onBlack, Action onComplete)
    {
        SetAlpha(fracturedText, 0f);
        SetAlpha(floorText, 0f);

        yield return Fade(0f, 1f, deathFadeIn, null);
        onBlack?.Invoke();
        yield return TextFadeIn(fracturedText, deathTextReveal, false);
        yield return new WaitForSecondsRealtime(deathHold);
        yield return TextFade(fracturedText, 1f, 0f, deathTextFadeOut);
        yield return Fade(1f, 0f, deathFadeOut, null);
        onComplete?.Invoke();
    }

    // Floor intro: fade to black, build the floor underneath, pop the title in and out, then
    // fade back to reveal the new floor.
    private IEnumerator FloorSequence(Action onBlack, Action onComplete)
    {
        SetAlpha(fracturedText, 0f);
        SetAlpha(floorText, 0f);

        yield return Fade(0f, 1f, floorFadeIn, null);
        onBlack?.Invoke();
        yield return TextFadeIn(floorText, floorTextReveal, true);
        yield return new WaitForSecondsRealtime(floorHold);
        yield return TextFade(floorText, 1f, 0f, floorTextFadeOut);
        yield return Fade(1f, 0f, floorFadeOut, null);
        onComplete?.Invoke();
    }

    // Runs a sequence, cancelling any that's already playing so they can't overlap.
    private void Run(IEnumerator routine)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(routine);
    }

    // Fades the overlay alpha between two values. Blocks input while it's not fully clear, so
    // clicks don't leak through during a transition.
    private IEnumerator Fade(float from, float to, float duration, Action onComplete)
    {
        _canvasGroup.alpha          = from;
        _canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _canvasGroup.alpha          = to;
        _canvasGroup.blocksRaycasts = to > 0.01f;
        onComplete?.Invoke();
    }

    // Fades a text label's alpha from 0 to 1, optionally scaling it up from floorTextStartScale
    // for a little "pop" (used by the floor title).
    private IEnumerator TextFadeIn(TextMeshProUGUI text, float duration, bool withScale)
    {
        if (text == null) yield break;
        Vector3 startScale = withScale ? Vector3.one * floorTextStartScale : Vector3.one;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            text.alpha = t;
            if (withScale)
                text.transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, t);
            yield return null;
        }
        text.alpha = 1f;
        if (withScale) text.transform.localScale = Vector3.one;
    }

    // Fades a text label's alpha between two values (used to fade captions out).
    private IEnumerator TextFade(TextMeshProUGUI text, float from, float to, float duration)
    {
        if (text == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            text.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        text.alpha = to;
    }

    // Small null-safe helper to set a label's alpha immediately.
    private void SetAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null) text.alpha = alpha;
    }
}
