using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardScreen : MonoBehaviour
{
    public Transform          rowContainer;   // Scroll View → Viewport → Content
    public GameObject         rowPrefab;      // prefab with LeaderboardRowUI
    public TextMeshProUGUI    statusText;     // "Loading…" / error message

    void OnEnable() => Refresh();

    public void Refresh()
    {
        ClearRows();
        SetStatus("Loading…");
        LeaderboardManager.Instance.FetchTop100(OnFetched);
    }

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

        // Force the ContentSizeFitter to compute the correct height immediately
        // so the ScrollRect doesn't clamp and snap back to position 0
        LayoutRebuilder.ForceRebuildLayoutImmediate(rowContainer as RectTransform);
    }

    private void ClearRows()
    {
        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }
    }

    public void OnCloseClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.CloseLeaderboard();
    }
}
