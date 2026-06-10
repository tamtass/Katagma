using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public RectTransform healthBarBase;
    public Image healthBarActual;
    public TextMeshProUGUI healthText;

    private PlayerMovement player;
    private float baseWidth;
    private float baseMaxHealth;
    private float leftEdgeX;
    private float leftBorder;
    private float rightBorder;
    private bool initialized;

    void Initialize()
    {
        baseWidth     = healthBarBase.sizeDelta.x;
        baseMaxHealth = player.maxHealth;
        leftEdgeX     = healthBarBase.anchoredPosition.x - baseWidth * healthBarBase.pivot.x;

        RectTransform actualRT = healthBarActual.rectTransform;
        leftBorder  =  actualRT.offsetMin.x;
        rightBorder = -actualRT.offsetMax.x;
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
        float ratio           = player.health / player.maxHealth;
        float fillableWidth   = newWidth - leftBorder - rightBorder;
        RectTransform actualRT = healthBarActual.rectTransform;
        actualRT.offsetMax = new Vector2(
            leftBorder + fillableWidth * ratio - newWidth,
            actualRT.offsetMax.y);

        healthText.text = $"{Mathf.CeilToInt(player.health)}/{Mathf.CeilToInt(player.maxHealth)}";
    }
}
