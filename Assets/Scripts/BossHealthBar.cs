using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public Image healthBarFill;
    public float slideSpeed = 80f;

    private Enemy boss;
    private float maxHealth;
    private float displayedHealth;
    private bool initialized;

    public void SetBoss(Enemy enemy)
    {
        boss            = enemy;
        maxHealth       = enemy.health;
        displayedHealth = enemy.health;
        initialized     = true;
    }

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
