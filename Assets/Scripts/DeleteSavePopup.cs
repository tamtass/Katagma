using UnityEngine;
using UnityEngine.SceneManagement;

public class DeleteSavePopup : MonoBehaviour
{
    public void Show()  => gameObject.SetActive(true);
    public void Hide()  => gameObject.SetActive(false);

    public void OnYesClicked()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Destroy persistent singletons so they reinitialise cleanly from the reloaded scene
        if (TransitionScreen.Instance != null) Destroy(TransitionScreen.Instance.gameObject);
        if (GameManager.Instance      != null) Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnNoClicked() => Hide();
}
