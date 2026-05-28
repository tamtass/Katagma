using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public PlayerMovement player;
    public RectTransform healthBarBase;
    public Image healthBarActual;
    public TextMeshProUGUI healthText;

    private float baseWidth;
    private float baseMaxHealth;
    private float leftEdgeX;
    private float leftBorder;   // Left offset  (offsetMin.x)
    private float rightBorder;  // Right offset (-offsetMax.x)

    void Start()
    {
        baseWidth     = healthBarBase.sizeDelta.x;
        baseMaxHealth = player.maxHealth;
        leftEdgeX     = healthBarBase.anchoredPosition.x - baseWidth * healthBarBase.pivot.x;

        RectTransform actualRT = healthBarActual.rectTransform;
        leftBorder  =  actualRT.offsetMin.x;
        rightBorder = -actualRT.offsetMax.x;
    }

    void Update()
    {
        if (player == null || player.maxHealth <= 0f) return;

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
