using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The lore/reward screen shown after a win. Each victory reveals one previously-hidden story
// image (fading it from black to visible), and which images have been revealed is saved to
// PlayerPrefs so progress carries across sessions. Once nothing new is left to reveal it just
// shows the images already unlocked.
public class StoryProgressionScreen : MonoBehaviour
{
    [Header("Images")]
    public Image[] storyImages;   // the full set of story images, revealed one per win

    [Header("Continue Button")]
    public CanvasGroup continueButton;   // faded in only after the reveal finishes

    [Header("Hide During Story")]
    public GameObject[] hideOnShow;   // things to hide while the story plays

    [Header("Timing")]
    public float imageShowDelay     = 1f;   // pause before the reveal starts
    public float imageFadeDuration  = 2f;   // how long an image fades in
    public float buttonFadeDuration = 0.5f;

    private const string PrefsKey = "StoryUnlockedImages";   // PlayerPrefs key for saved progress

    // Sets up the screen: show already-unlocked images bright and the rest black, hide the
    // continue button, hide anything flagged, then start the reveal sequence.
    public void Show()
    {
        var unlocked = LoadUnlocked();

        if (storyImages != null)
            for (int i = 0; i < storyImages.Length; i++)
                if (storyImages[i] != null)
                    storyImages[i].color = unlocked.Contains(i) ? Color.white : Color.black;

        if (continueButton != null)
        {
            continueButton.alpha          = 0f;
            continueButton.interactable   = false;
            continueButton.blocksRaycasts = false;
        }

        if (hideOnShow != null)
            foreach (var go in hideOnShow)
                if (go != null) go.SetActive(false);

        StartCoroutine(PlaySequence(unlocked));
    }

    // Picks one still-locked image at random, saves it as unlocked, and fades it in; if
    // everything's already unlocked it skips straight ahead. Then fades in the continue button.
    // Uses unscaled time because the game is frozen on this screen.
    private IEnumerator PlaySequence(HashSet<int> unlocked)
    {
        if (storyImages == null || storyImages.Length == 0) yield break;

        // Gather the images not yet revealed.
        var locked = new List<int>();
        for (int i = 0; i < storyImages.Length; i++)
            if (!unlocked.Contains(i)) locked.Add(i);

        yield return new WaitForSecondsRealtime(imageShowDelay);

        if (locked.Count > 0)
        {
            int index = locked[Random.Range(0, locked.Count)];
            Image target = storyImages[index];

            unlocked.Add(index);
            SaveUnlocked(unlocked);

            float elapsed = 0f;
            while (elapsed < imageFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.color = Color.Lerp(Color.black, Color.white, elapsed / imageFadeDuration);
                yield return null;
            }
            target.color = Color.white;
        }
        // If everything was already unlocked, the images are already white — nothing to reveal.

        if (continueButton == null) yield break;

        continueButton.interactable   = true;
        continueButton.blocksRaycasts = true;
        float e = 0f;
        while (e < buttonFadeDuration)
        {
            e += Time.unscaledDeltaTime;
            continueButton.alpha = e / buttonFadeDuration;
            yield return null;
        }
        continueButton.alpha = 1f;
    }

    // Continue button: back to the main menu.
    public void OnContinueClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
    }

    // Reads the set of unlocked image indices from PlayerPrefs (stored as a comma-separated list).
    private HashSet<int> LoadUnlocked()
    {
        var result = new HashSet<int>();
        string data = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(data)) return result;
        foreach (var part in data.Split(','))
            if (int.TryParse(part, out int i)) result.Add(i);
        return result;
    }

    // Writes the unlocked set back to PlayerPrefs.
    private void SaveUnlocked(HashSet<int> unlocked)
    {
        PlayerPrefs.SetString(PrefsKey, string.Join(",", unlocked));
        PlayerPrefs.Save();
    }
}
