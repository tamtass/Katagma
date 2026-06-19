using UnityEngine;

public class IncreaseMaxHealthItem : Item
{
    public float amount = 20f;

    protected override void Apply(PlayerMovement player)
    {
        player.UpgradeMaxHealth(amount);
        player.Heal(amount);
    }
}
