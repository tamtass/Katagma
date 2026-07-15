using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Talks to the Firebase Firestore leaderboard over its plain REST API (no Firebase SDK, which
// keeps the desktop build simple). It can submit a score and fetch the top 100. Both use
// UnityWebRequest and hand results back through callbacks. It's a self-creating singleton, so
// the first access spins it up automatically.
public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager _instance;
    public static LeaderboardManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            // Create one on demand if it doesn't exist yet.
            var go = new GameObject("[LeaderboardManager]");
            _instance = go.AddComponent<LeaderboardManager>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    // Guard against a second instance if one is also placed in the scene.
    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Public entry point to upload a score. Runs the request as a coroutine and reports success
    // through onComplete.
    public void SubmitScore(string playerName, int score, int floorsCleared, float time,
                            Action<bool> onComplete = null)
        => StartCoroutine(SubmitCoroutine(playerName, score, floorsCleared, time, onComplete));

    // Builds a Firestore "create document" request by hand. Firestore's REST format types every
    // field (stringValue, integerValue, etc.), and integers must be sent as strings because
    // Firestore uses 64-bit ints. The name is escaped so it can't break the JSON.
    private IEnumerator SubmitCoroutine(string playerName, int score, int floorsCleared,
                                        float time, Action<bool> onComplete)
    {
        string url     = $"{FirebaseConfig.BaseUrl}/leaderboard?key={FirebaseConfig.ApiKey}";
        string timeStr = time.ToString("F2", CultureInfo.InvariantCulture);   // invariant so it's always a '.'
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

    // Public entry point to fetch the top 100 scores.
    public void FetchTop100(Action<List<LeaderboardEntry>> onComplete)
        => StartCoroutine(FetchCoroutine(onComplete));

    // Runs a Firestore structured query for the 100 highest scores, then parses the typed JSON
    // response into plain LeaderboardEntry objects. On any failure it returns an empty list so
    // callers don't have to special-case errors.
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
                // JsonUtility can't parse a top-level array, so wrap the response in an object
                // with a named field first, then read it into the helper classes below.
                string wrapped = "{\"items\":" + request.downloadHandler.text + "}";
                var wrapper = JsonUtility.FromJson<_QueryWrapper>(wrapped);

                if (wrapper?.items != null)
                    foreach (var item in wrapper.items)
                    {
                        var f = item.document?.fields;
                        if (f == null) continue;   // query rows can include an empty read-time entry
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

    // Minimal JSON escaping for the player name: escape backslashes and quotes, strip newlines.
    private static string EscapeJson(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "").Replace("\r", "");

    // Private classes mirroring the nested shape of Firestore's REST response, purely so
    // JsonUtility has something to deserialize into. The odd names/types (integerValue as a
    // string) match Firestore's format exactly.
    [Serializable] class _QueryWrapper { public _QueryItem[]  items; }
    [Serializable] class _QueryItem    { public _FS_Doc       document; }
    [Serializable] class _FS_Doc       { public _FS_Fields    fields; }
    [Serializable] class _FS_Fields    { public _FS_Str name; public _FS_Int score; public _FS_Int floorsCleared; public _FS_Dbl time; }
    [Serializable] class _FS_Str       { public string stringValue; }
    [Serializable] class _FS_Int       { public string integerValue; }
    [Serializable] class _FS_Dbl       { public double doubleValue; }
}
