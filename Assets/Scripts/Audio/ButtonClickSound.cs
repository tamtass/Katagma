using UnityEngine;
using UnityEngine.UI;

// Plays the UI click sound when a button is pressed. It's flexible about where it's attached:
// put it directly on a Button to wire just that one, or on a parent (like a menu Canvas) to
// wire every child Button at once — including inactive ones — so a whole menu is covered by a
// single component.
public class ButtonClickSound : MonoBehaviour
{
    // Wire up the click sound. If this object is a Button, hook that one; otherwise hook every
    // Button beneath it.
    void Start()
    {
        if (TryGetComponent<Button>(out var self))
            self.onClick.AddListener(PlayClick);
        else
            foreach (var b in GetComponentsInChildren<Button>(true))
                b.onClick.AddListener(PlayClick);
    }

    // Fires the click sound through the SoundManager.
    private void PlayClick()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
    }
}
