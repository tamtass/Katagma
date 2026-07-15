using UnityEngine;

// An item-room item that permanently raises the player's maximum health (and heals by the
// same amount, so the new capacity is filled immediately).
public class IncreaseMaxHealthItem : Item
{
    public float amount = 20f;   // how much max HP this grants

    // The effect, run when the pickup animation finishes.
    protected override void Apply(PlayerMovement player)
    {
        player.UpgradeMaxHealth(amount);
        player.Heal(amount);
    }
}
