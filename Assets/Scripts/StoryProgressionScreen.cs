using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryProgressionScreen : MonoBehaviour
{
    [Header("Images")]
    public Image[] storyImages;

    [Header("Continue Button")]
    public CanvasGroup continueButton;

    [Header("Hide During Story")]
    public GameObject[] hideOnShow;

    [Header("Timing")]
    public float imageShowDelay     = 1f;
    public float imageFadeDuration  = 2f;
    public float buttonFadeDuration = 0.5f;

    private const string PrefsKey = "StoryUnlockedImages";

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

    private IEnumerator PlaySequence(HashSet<int> unlocked)
    {
        if (storyImages == null || storyImages.Length == 0) yield break;

        // Pick one image that hasn't been unlocked yet
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
        // if all images already unlocked, they're already white — just show the button

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

    public void OnContinueClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
    }

    private HashSet<int> LoadUnlocked()
    {
        var result = new HashSet<int>();
        string data = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(data)) return result;
        foreach (var part in data.Split(','))
            if (int.TryParse(part, out int i)) result.Add(i);
        return result;
    }

    private void SaveUnlocked(HashSet<int> unlocked)
    {
        PlayerPrefs.SetString(PrefsKey, string.Join(",", unlocked));
        PlayerPrefs.Save();
    }
}
