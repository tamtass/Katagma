using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The popup on the game-over screen that lets the player upload their score. It asks for a name
// (pre-filled with their last one), submits the run's stats to the leaderboard, and gives
// feedback while it works. Controls are locked during submission and re-enabled on failure so
// the player can retry.
public class SubmitScorePopup : MonoBehaviour
{
    public TMP_InputField      nameInput;
    public Button              submitButton;
    public TextMeshProUGUI     statusText;

    private const string LastNameKey = "LastSubmittedName";   // PlayerPrefs key for the remembered name
    private const int    MaxNameLen  = 20;

    // Opens the popup: pre-fill the name from last time, cap its length, and reset the controls.
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

    // Submit button: validate the name, lock the controls, remember the name, and send the run's
    // score and stats to the leaderboard.
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

    // Cancel button: just close the popup.
    public void OnCancelClicked()
    {
        gameObject.SetActive(false);
    }

    // Result callback: on success, confirm then auto-close; on failure, show an error and unlock
    // the controls so the player can try again.
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

    // Shows a message, waits briefly (real time, since the game is frozen here), then closes.
    private IEnumerator CloseAfterDelay(string message)
    {
        SetStatus(message);
        yield return new WaitForSecondsRealtime(1.5f);
        gameObject.SetActive(false);
    }

    // Enables/disables the name field and submit button together, to lock input during a request.
    private void SetBusy(bool busy)
    {
        if (nameInput    != null) nameInput.interactable    = !busy;
        if (submitButton != null) submitButton.interactable = !busy;
    }

    // Sets the status label text.
    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
