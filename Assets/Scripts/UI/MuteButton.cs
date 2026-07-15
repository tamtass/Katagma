using UnityEngine;

// The options-screen mute toggle. Flips the global mute (via the GameManager) and swaps the
// button's icon to match. Hook ToggleMute up to the button's OnClick in the inspector.
public class MuteButton : MonoBehaviour
{
    public Sprite mutedSprite;     // icon shown when muted
    public Sprite unmutedSprite;   // icon shown when not muted

    // Set the icon to match the current mute state when the button first appears.
    void Start()
    {
        UpdateButtonSprite();
    }

    // Picks the icon based on whether the game is currently muted.
    private void UpdateButtonSprite()
    {
        GetComponent<UnityEngine.UI.Image>().sprite = GameManager.Instance.IsGameMuted ? mutedSprite : unmutedSprite;
    }

    // Called by the button click: flip mute and refresh the icon.
    public void ToggleMute()
    {
        GameManager.Instance.IsGameMuted = !GameManager.Instance.IsGameMuted;
        UpdateButtonSprite();
    }
}
