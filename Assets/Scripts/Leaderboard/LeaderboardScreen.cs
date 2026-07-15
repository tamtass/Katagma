using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The leaderboard screen. When shown, it fetches the top 100 scores and fills a scrollable list
// with one row per entry. Also handles the empty/loading states and the close and refresh buttons.
public class LeaderboardScreen : MonoBehaviour
{
    public Transform          rowContainer;   // the ScrollView's Content, where rows are added
    public GameObject         rowPrefab;      // a row prefab carrying a LeaderboardRowUI
    public TextMeshProUGUI    statusText;     // shows "Loading…" or an empty/error message

    // Refresh automatically each time the screen is opened.
    void OnEnable() => Refresh();

    // Clears the list, shows a loading message, and kicks off a fresh fetch. Also wired to the
    // refresh button.
    public void Refresh()
    {
        ClearRows();
        SetStatus("Loading…");
        LeaderboardManager.Instance.FetchTop100(OnFetched);
    }

    // Callback once scores come back: rebuild the list, or show an empty-state message if there
    // are none.
    private void OnFetched(List<LeaderboardEntry> entries)
    {
        ClearRows();

        if (entries.Count == 0)
        {
            SetStatus("No scores yet — be the first!");
            return;
        }

        SetStatus("");
        for (int i = 0; i < entries.Count; i++)
        {
            var row = Instantiate(rowPrefab, rowContainer);
            row.GetComponent<LeaderboardRowUI>()?.SetData(i + 1, entries[i]);
        }

        // Force the layout to recompute the content height right now, otherwise the ScrollRect
        // sees a stale size and snaps back to the top after the first scroll.
        LayoutRebuilder.ForceRebuildLayoutImmediate(rowContainer as RectTransform);
    }

    // Removes all current rows.
    private void ClearRows()
    {
        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);
    }

    // Sets the status label, hiding it entirely when the message is empty.
    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }
    }

    // Close button: hand back to the GameManager to return to the menu.
    public void OnCloseClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.CloseLeaderboard();
    }
}
