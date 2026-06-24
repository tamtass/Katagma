using UnityEngine;

public class MuteButton : MonoBehaviour
{
    public Sprite mutedSprite;
    public Sprite unmutedSprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateButtonSprite();
    }

    private void UpdateButtonSprite()
    {
        GetComponent<UnityEngine.UI.Image>().sprite = GameManager.Instance.IsGameMuted ? mutedSprite : unmutedSprite;
    }

    public void ToggleMute()
    {
        GameManager.Instance.IsGameMuted = !GameManager.Instance.IsGameMuted;
        Debug.Log(AudioListener.volume);
        UpdateButtonSprite();
    }
}
