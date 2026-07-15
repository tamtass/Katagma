using UnityEngine;
using UnityEngine.SceneManagement;

// The "are you sure?" confirmation for wiping save data (the unlocked story images). Yes clears
// PlayerPrefs and reloads the scene from scratch so everything starts fresh; No just closes it.
public class DeleteSavePopup : MonoBehaviour
{
    public void Show()  => gameObject.SetActive(true);
    public void Hide()  => gameObject.SetActive(false);

    // Confirmed: erase all saved data, then reload the scene. The persistent singletons are
    // destroyed first so they re-create themselves cleanly in the reloaded scene rather than
    // surviving as stale duplicates.
    public void OnYesClicked()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (TransitionScreen.Instance != null) Destroy(TransitionScreen.Instance.gameObject);
        if (GameManager.Instance      != null) Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Cancelled: just close the popup.
    public void OnNoClicked() => Hide();
}
