using UnityEngine;
using UnityEngine.UI;

// The boss room's health bar. Like the player bar, its fill slides smoothly toward the boss's
// real health. The boss is handed to it directly (rather than searched for) so it can't latch
// onto the wrong enemy, and the whole bar hides itself once the boss is gone.
public class BossHealthBar : MonoBehaviour
{
    public Image healthBarFill;      // the Filled image driven by fillAmount
    public float slideSpeed = 80f;   // how fast the fill chases the real health

    private Enemy boss;              // the boss this bar tracks
    private float maxHealth;         // boss HP at the start, used as the fill denominator
    private float displayedHealth;   // the smoothed value shown
    private bool initialized;

    // Called by the room when it spawns the boss, to bind this bar to it and seed the display.
    public void SetBoss(Enemy enemy)
    {
        boss            = enemy;
        maxHealth       = enemy.health;
        displayedHealth = enemy.health;
        initialized     = true;
    }

    // Slides the fill toward the boss's health each frame. Once the boss is destroyed the bar
    // hides itself. The target is clamped at zero so an overkill hit (negative HP) can't drag
    // the bar below empty while the boss object still briefly exists.
    void Update()
    {
        if (!initialized) return;

        if (boss == null)
        {
            gameObject.SetActive(false);
            return;
        }

        float target = Mathf.Max(boss.health, 0f);
        displayedHealth = Mathf.MoveTowards(displayedHealth, target, slideSpeed * Time.deltaTime);
        healthBarFill.fillAmount = displayedHealth / maxHealth;
    }
}
