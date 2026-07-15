// A single leaderboard row in plain form, used both for submitting a score and for holding one
// fetched back from the server. Only name and score are shown in the UI; floors and time are
// stored too, in case they're wanted later.
public class LeaderboardEntry
{
    public string name;
    public int    score;
    public int    floorsCleared;
    public float  time;
}
