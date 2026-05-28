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

    [Header("Floor Announcement Timing")]
    public float floorFadeIn      = 0.6f;
    public float floorHold        = 1.2f;
    public float floorFadeOut     = 0.6f;
    public float textRevealDuration = 0.5f;
    public float textStartScale     = 0.8f;

    [Header("Death Timing")]
    public float deathFadeIn  = 2f;
    public float deathHold    = 1f;
    public float deathFadeOut = 1f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        SetVisible(fracturedText, false);
        SetVisible(floorText, false);
    }

    // Fade from black without any text — used for menu fade-in on startup
    public void FadeFromBlack(float duration)
    {
        SetVisible(fracturedText, false);
        SetVisible(floorText, false);
        canvasGroup.alpha = 1f;
        StartCoroutine(Fade(1f, 0f, duration));
    }

    // Show "FLOOR X" then call onComplete when fully faded out
    public void ShowFloor(int floor, Action onBlack = null, Action onComplete = null)
    {
        StartCoroutine(FloorSequence(floor, onBlack, onComplete));
    }

    // Show FRACTURED death screen then return to menu
    public void ShowDeath()
    {
        StartCoroutine(DeathSequence());
    }

    IEnumerator FloorSequence(int floor, Action onBlack, Action onComplete)
    {
        // Fade to black over whatever is currently showing (e.g. main menu)
        SetVisible(fracturedText, false);
        SetVisible(floorText, false);
        yield return Fade(0f, 1f, floorFadeIn);

        // Screen is fully black — safe to swap UI and load the floor
        onBlack?.Invoke();

        // Animate floor text in: fade alpha + grow scale simultaneously
        if (floorText != null) floorText.text = $"FLOOR {floor}";
        yield return RevealText(floorText);

        yield return new WaitForSecondsRealtime(floorHold);

        // Fade the whole overlay out, text disappears with it
        yield return Fade(1f, 0f, floorFadeOut);

        onComplete?.Invoke();
    }

    IEnumerator DeathSequence()
    {
        SetVisible(floorText, false);
        SetVisible(fracturedText, true);

        yield return Fade(0f, 1f, deathFadeIn);
        yield return new WaitForSecondsRealtime(deathHold);

        GameManager.Instance.ReturnToMainMenu();

        SetVisible(fracturedText, false);
        yield return Fade(1f, 0f, deathFadeOut);
    }

    IEnumerator RevealText(TextMeshProUGUI text)
    {
        if (text == null) yield break;

        Vector3 startScale = Vector3.one * textStartScale;
        float elapsed = 0f;
        while (elapsed < textRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / textRevealDuration);
            text.alpha = t;
            text.transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, t);
            yield return null;
        }
        text.alpha = 1f;
        text.transform.localScale = Vector3.one;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        canvasGroup.alpha = from;
        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = to > 0f;
    }

    void SetVisible(TextMeshProUGUI text, bool visible)
    {
        if (text != null)
            text.alpha = visible ? 1f : 0f;
    }
}
