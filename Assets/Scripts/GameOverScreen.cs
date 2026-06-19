using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class GameOverScreen : MonoBehaviour
{
    [Header("Win / Lose")]
    public GameObject winTitle;
    public GameObject loseTitle;
    public GameObject winImage;
    public GameObject loseImage;

    [Header("Stats")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI enemiesKilledText;
    public TextMeshProUGUI roomsClearedText;
    public TextMeshProUGUI floorsText;

    [Header("Settings")]
    public float fadeDuration = 1f;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(bool isWin)
    {
        gameObject.SetActive(true);

        if (winTitle != null)  winTitle.SetActive(isWin);
        if (loseTitle != null) loseTitle.SetActive(!isWin);
        if (winImage != null)  winImage.SetActive(isWin);
        if (loseImage != null) loseImage.SetActive(!isWin);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            scoreText.text        = gm.Score.ToString();
            enemiesKilledText.text = gm.EnemiesKilled.ToString();
            roomsClearedText.text  = gm.RoomsCleared.ToString();
            floorsText.text        = gm.FloorsCleared.ToString();

            int total   = (int)gm.ElapsedTime;
            int hours   = total / 3600;
            int minutes = total % 3600 / 60;
            int seconds = total % 60;
            timeText.text = hours > 0
                ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
                : $"{minutes:D2}:{seconds:D2}";
        }

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }
}
