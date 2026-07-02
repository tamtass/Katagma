using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public RectTransform healthBarBase;
    public Image healthBarFill;
    public TextMeshProUGUI healthText;

    public float slideSpeed = 80f;

    private PlayerMovement player;
    private float baseWidth;
    private float baseMaxHealth;
    private float leftEdgeX;
    private float displayedHealth;
    private bool initialized;

    void Awake()
    {
        baseWidth = healthBarBase.sizeDelta.x;
        leftEdgeX = healthBarBase.anchoredPosition.x - baseWidth * healthBarBase.pivot.x;
    }

    void Initialize()
    {
        baseMaxHealth   = player.maxHealth;
        displayedHealth = player.health;
        initialized     = true;
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement>();
            initialized = false;
        }
        if (player == null || player.maxHealth <= 0f) return;
        if (!initialized) Initialize();

        // Resize base bar width as max health grows, keeping the left edge fixed
        float newWidth = baseWidth * (player.maxHealth / baseMaxHealth);
        healthBarBase.sizeDelta = new Vector2(newWidth, healthBarBase.sizeDelta.y);
        healthBarBase.anchoredPosition = new Vector2(
            leftEdgeX + newWidth * healthBarBase.pivot.x,
            healthBarBase.anchoredPosition.y);

        displayedHealth = Mathf.MoveTowards(displayedHealth, player.health, slideSpeed * Time.unscaledDeltaTime);
        healthBarFill.fillAmount = displayedHealth / player.maxHealth;
        healthText.text = $"{Mathf.CeilToInt(displayedHealth)}";
    }
}
