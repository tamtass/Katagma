using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager _instance;
    public static LeaderboardManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[LeaderboardManager]");
            _instance = go.AddComponent<LeaderboardManager>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    public void SubmitScore(string playerName, int score, int floorsCleared, float time,
                            Action<bool> onComplete = null)
        => StartCoroutine(SubmitCoroutine(playerName, score, floorsCleared, time, onComplete));

    private IEnumerator SubmitCoroutine(string playerName, int score, int floorsCleared,
                                        float time, Action<bool> onComplete)
    {
        string url     = $"{FirebaseConfig.BaseUrl}/leaderboard?key={FirebaseConfig.ApiKey}";
        string timeStr = time.ToString("F2", CultureInfo.InvariantCulture);
        string body    = "{\"fields\":{"
            + $"\"name\":{{\"stringValue\":\"{EscapeJson(playerName)}\"}}"
            + $",\"score\":{{\"integerValue\":\"{score}\"}}"
            + $",\"floorsCleared\":{{\"integerValue\":\"{floorsCleared}\"}}"
            + $",\"time\":{{\"doubleValue\":{timeStr}}}"
            + "}}";

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool ok = request.result == UnityWebRequest.Result.Success;
        if (!ok) Debug.LogWarning($"[Leaderboard] Submit failed: {request.error}");
        request.Dispose();
        onComplete?.Invoke(ok);
    }

    // ── Fetch ─────────────────────────────────────────────────────────────────

    public void FetchTop100(Action<List<LeaderboardEntry>> onComplete)
        => StartCoroutine(FetchCoroutine(onComplete));

    private IEnumerator FetchCoroutine(Action<List<LeaderboardEntry>> onComplete)
    {
        string url  = $"{FirebaseConfig.BaseUrl}:runQuery?key={FirebaseConfig.ApiKey}";
        string body = "{\"structuredQuery\":{"
            + "\"from\":[{\"collectionId\":\"leaderboard\"}],"
            + "\"orderBy\":[{\"field\":{\"fieldPath\":\"score\"},\"direction\":\"DESCENDING\"}],"
            + "\"limit\":100}}";

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        var entries = new List<LeaderboardEntry>();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                // JsonUtility can't parse arrays directly — wrap it first
                string wrapped = "{\"items\":" + request.downloadHandler.text + "}";
                var wrapper = JsonUtility.FromJson<_QueryWrapper>(wrapped);

                if (wrapper?.items != null)
                    foreach (var item in wrapper.items)
                    {
                        var f = item.document?.fields;
                        if (f == null) continue;
                        entries.Add(new LeaderboardEntry
                        {
                            name          = f.name?.stringValue ?? "Unknown",
                            score         = int.TryParse(f.score?.integerValue, out int s)  ? s  : 0,
                            floorsCleared = int.TryParse(f.floorsCleared?.integerValue, out int fc) ? fc : 0,
                            time          = f.time != null ? (float)f.time.doubleValue : 0f
                        });
                    }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[Leaderboard] Fetch failed: {request.error}");
        }

        request.Dispose();
        onComplete?.Invoke(entries);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string EscapeJson(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "").Replace("\r", "");

    // Firestore REST response shape — private, only used for deserialization
    [Serializable] class _QueryWrapper { public _QueryItem[]  items; }
    [Serializable] class _QueryItem    { public _FS_Doc       document; }
    [Serializable] class _FS_Doc       { public _FS_Fields    fields; }
    [Serializable] class _FS_Fields    { public _FS_Str name; public _FS_Int score; public _FS_Int floorsCleared; public _FS_Dbl time; }
    [Serializable] class _FS_Str       { public string stringValue; }
    [Serializable] class _FS_Int       { public string integerValue; }
    [Serializable] class _FS_Dbl       { public double doubleValue; }
}
