// Connection details for the Firebase Firestore database that backs the leaderboard. These are
// used to build the REST API URLs. The API key is a public client key (Firestore access is
// governed by the database's security rules, not by keeping this secret).
public static class FirebaseConfig
{
    public const string ApiKey    = "AIzaSyDkUUopC4l60AROFdalgsZCcbLBm7saoVg";
    public const string ProjectId = "katagma-eea2e";
    public const string BaseUrl   =
        "https://firestore.googleapis.com/v1/projects/katagma-eea2e/databases/(default)/documents";
}
