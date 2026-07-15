using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The player's HUD health bar. The fill slides smoothly toward the true health (rather than
// snapping) so damage and healing read clearly, and the whole bar physically grows wider when
// the player's max health is upgraded, keeping the fill proportion honest.
public class HealthBar : MonoBehaviour
{
    public RectTransform healthBarBase;    // the bar's background/frame, resized as max HP grows
    public Image healthBarFill;            // the coloured fill (a Filled image driven by fillAmount)
    public TextMeshProUGUI healthText;     // numeric HP readout

    public float slideSpeed = 80f;   // how fast the displayed value chases the real one

    private PlayerMovement player;
    private float baseWidth;         // the bar's authored width at the starting max HP
    private float baseMaxHealth;     // the player's max HP when first seen, the reference for scaling
    private float leftEdgeX;         // the bar's fixed left edge, so it grows rightward only
    private float displayedHealth;   // the smoothed value actually shown
    private bool initialized;

    // Record the bar's starting width and left edge so it can be resized while keeping its left
    // edge anchored.
    void Awake()
    {
        baseWidth = healthBarBase.sizeDelta.x;
        leftEdgeX = healthBarBase.anchoredPosition.x - baseWidth * healthBarBase.pivot.x;
    }

    // First-time setup once the player exists: capture the reference max HP and seed the display.
    void Initialize()
    {
        baseMaxHealth   = player.maxHealth;
        displayedHealth = player.health;
        initialized     = true;
    }

    // Each frame: find the player if needed, resize the bar to match current max HP (anchored on
    // the left), then slide the fill toward the real health and update the number. Uses unscaled
    // time so the bar still drains to zero on death, when the game is frozen.
    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement>();
            initialized = false;
        }
        if (player == null || player.maxHealth <= 0f) return;
        if (!initialized) Initialize();

        // Widen the bar in proportion to how much max HP has grown, keeping the left edge fixed.
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
