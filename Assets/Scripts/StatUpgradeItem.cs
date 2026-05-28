using UnityEngine;

public class StatUpgradeItem : Item
{
    public enum Stat
    {
        CurrentHealth,
        MaxHealth,
        MovementSpeed,
        AttackSpeed,
        Damage,
        AttackRange,
        ProjectileCount
    }

    public Stat stat;
    public float amount = 1f;

    protected override bool CanPickUp(PlayerMovement player)
    {
        if (stat == Stat.CurrentHealth)
            return player.health < player.maxHealth;
        return true;
    }

    protected override void Apply(PlayerMovement player)
    {
        switch (stat)
        {
            case Stat.CurrentHealth:   player.Heal(amount);                      break;
            case Stat.MaxHealth:       player.UpgradeMaxHealth(amount);          break;
            case Stat.MovementSpeed:   player.UpgradeMovementSpeed(amount);      break;
            case Stat.AttackSpeed:     player.UpgradeAttackSpeed(amount);        break;
            case Stat.Damage:          player.UpgradeDamage(amount);             break;
            case Stat.AttackRange:     player.UpgradeAttackRange(amount);        break;
            case Stat.ProjectileCount: player.UpgradeProjectileCount((int)amount); break;
        }
    }
}
