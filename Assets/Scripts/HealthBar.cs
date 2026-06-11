using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public RectTransform healthBarBase;
    public Image healthBarActual;
    public TextMeshProUGUI healthText;

    public float slideSpeed = 80f;

    private PlayerMovement player;
    private float baseWidth;
    private float baseMaxHealth;
    private float leftEdgeX;
    private float leftBorder;
    private float rightBorder;
    private float displayedHealth;
    private bool initialized;

    void Awake()
    {
        baseWidth = healthBarBase.sizeDelta.x;
        leftEdgeX = healthBarBase.anchoredPosition.x - baseWidth * healthBarBase.pivot.x;

        RectTransform actualRT = healthBarActual.rectTransform;
        leftBorder  =  actualRT.offsetMin.x;
        rightBorder = -actualRT.offsetMax.x;
    }

    void Initialize()
    {
        baseMaxHealth   = player.maxHealth;
        displayedHealth = player.health;

        healthBarBase.sizeDelta = new Vector2(baseWidth, healthBarBase.sizeDelta.y);
        healthBarBase.anchoredPosition = new Vector2(
            leftEdgeX + baseWidth * healthBarBase.pivot.x,
            healthBarBase.anchoredPosition.y);

        initialized = true;
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

        // Resize bar base, keeping the left edge fixed
        float newWidth = baseWidth * (player.maxHealth / baseMaxHealth);
        healthBarBase.sizeDelta = new Vector2(newWidth, healthBarBase.sizeDelta.y);
        healthBarBase.anchoredPosition = new Vector2(
            leftEdgeX + newWidth * healthBarBase.pivot.x,
            healthBarBase.anchoredPosition.y);

        // Move the right edge of the fill, preserving the border offsets
        // rightEdge = leftBorder + fillableWidth * ratio
        // offsetMax.x = rightEdge - newWidth
        displayedHealth = Mathf.MoveTowards(displayedHealth, player.health, slideSpeed * Time.deltaTime);

        float ratio           = displayedHealth / player.maxHealth;
        float fillableWidth   = newWidth - leftBorder - rightBorder;
        RectTransform actualRT = healthBarActual.rectTransform;
        actualRT.offsetMax = new Vector2(
            leftBorder + fillableWidth * ratio - newWidth,
            actualRT.offsetMax.y);

        healthText.text = $"{Mathf.CeilToInt(displayedHealth)}";
    }
}
