using System;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TransitionScreen : MonoBehaviour
{
    public static TransitionScreen Instance { get; private set; }

    [Header("References")]
    public TextMeshProUGUI fracturedText;
    public TextMeshProUGUI floorText;

    [Header("Death Timing")]
    public float deathFadeIn        = 2f;
    public float deathTextReveal    = 0.5f;
    public float deathHold          = 1f;
    public float deathTextFadeOut   = 0.3f;
    public float deathFadeOut       = 1f;

    [Header("Floor Timing")]
    public float floorFadeIn        = 0.6f;
    public float floorTextReveal    = 0.5f;
    public float floorHold          = 1.2f;
    public float floorTextFadeOut   = 0.3f;
    public float floorFadeOut       = 0.6f;
    public float floorTextStartScale = 0.8f;

    private CanvasGroup _canvasGroup;
    private Coroutine   _current;

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

    // ── Public API ────────────────────────────────────────────────────────────

    // Startup only: overlay starts at alpha 0, set to 1 then fade out
    public void FadeFromBlack(float duration)
    {
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
        Run(Fade(1f, 0f, duration, null));
    }

    // Generic screen swap: fade to black → onBlack (swap screens) → fade from black → onComplete
    public void Transition(float outDuration, float inDuration,
                           Action onBlack = null, Action onComplete = null)
        => Run(TransitionSequence(outDuration, inDuration, onBlack, onComplete));

    // Death: fade to black → onBlack (show game over) → FRACTURED in → hold → FRACTURED out → fade from black
    public void ShowDeath(Action onBlack = null, Action onComplete = null)
        => Run(DeathSequence(onBlack, onComplete));

    // Floor: fade to black → onBlack (load floor) → title in → hold → title out → fade from black → onComplete
    public void ShowFloor(string label = null,
                          Action onBlack = null, Action onComplete = null)
    {
        if (floorText != null && label != null) floorText.text = label;
        Run(FloorSequence(onBlack, onComplete));
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    private IEnumerator TransitionSequence(float outDuration, float inDuration,
                                           Action onBlack, Action onComplete)
    {
        yield return Fade(0f, 1f, outDuration, null);   // black in
        onBlack?.Invoke();                               // swap screens
        yield return Fade(1f, 0f, inDuration, null);    // black out
        onComplete?.Invoke();
    }

    private IEnumerator DeathSequence(Action onBlack, Action onComplete)
    {
        SetAlpha(fracturedText, 0f);
        SetAlpha(floorText, 0f);

        yield return Fade(0f, 1f, deathFadeIn, null);   // black in
        onBlack?.Invoke();                               // activate game over screen
        yield return TextFadeIn(fracturedText, deathTextReveal, false);
        yield return new WaitForSecondsRealtime(deathHold);
        yield return TextFade(fracturedText, 1f, 0f, deathTextFadeOut);
        yield return Fade(1f, 0f, deathFadeOut, null);  // black out → game over revealed
        onComplete?.Invoke();
    }

    private IEnumerator FloorSequence(Action onBlack, Action onComplete)
    {
        SetAlpha(fracturedText, 0f);
        SetAlpha(floorText, 0f);

        yield return Fade(0f, 1f, floorFadeIn, null);   // black in
        onBlack?.Invoke();                               // load new floor
        yield return TextFadeIn(floorText, floorTextReveal, true);
        yield return new WaitForSecondsRealtime(floorHold);
        yield return TextFade(floorText, 1f, 0f, floorTextFadeOut);
        yield return Fade(1f, 0f, floorFadeOut, null);  // black out → new floor revealed
        onComplete?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Run(IEnumerator routine)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(routine);
    }

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

    // Fade text alpha 0→1, with optional scale pop for floor title
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

    private void SetAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null) text.alpha = alpha;
    }
}
