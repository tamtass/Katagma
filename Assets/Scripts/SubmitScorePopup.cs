using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubmitScorePopup : MonoBehaviour
{
    public TMP_InputField      nameInput;
    public Button              submitButton;
    public TextMeshProUGUI     statusText;

    private const string LastNameKey = "LastSubmittedName";
    private const int    MaxNameLen  = 20;

    public void Show()
    {
        gameObject.SetActive(true);
        if (nameInput != null)
        {
            nameInput.text          = PlayerPrefs.GetString(LastNameKey, "");
            nameInput.characterLimit = MaxNameLen;
            nameInput.interactable  = true;
        }
        if (submitButton != null) submitButton.interactable = true;
        SetStatus("");
    }

    public void OnSubmitClicked()
    {
        string playerName = nameInput != null ? nameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(playerName))
        {
            SetStatus("Please enter a name.");
            return;
        }

        SetBusy(true);
        SetStatus("Submitting…");

        int   score   = GameManager.Instance?.Score         ?? 0;
        int   floors  = GameManager.Instance?.FloorsCleared ?? 0;
        float time    = GameManager.Instance?.ElapsedTime   ?? 0f;

        PlayerPrefs.SetString(LastNameKey, playerName);
        PlayerPrefs.Save();

        LeaderboardManager.Instance.SubmitScore(playerName, score, floors, time, OnSubmitResult);
    }

    public void OnCancelClicked()
    {
        gameObject.SetActive(false);
    }

    private void OnSubmitResult(bool success)
    {
        if (success)
            StartCoroutine(CloseAfterDelay("Score submitted!"));
        else
        {
            SetStatus("Failed — check your connection.");
            SetBusy(false);
        }
    }

    private IEnumerator CloseAfterDelay(string message)
    {
        SetStatus(message);
        yield return new WaitForSecondsRealtime(1.5f);
        gameObject.SetActive(false);
    }

    private void SetBusy(bool busy)
    {
        if (nameInput    != null) nameInput.interactable    = !busy;
        if (submitButton != null) submitButton.interactable = !busy;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
