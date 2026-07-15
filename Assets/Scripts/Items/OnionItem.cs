using UnityEngine;

// The Onion item. Grants the player a passive auto-shooter that fires at the nearest enemy.
// Like the max-health item, it lives in the item-room pool and inherits the whole pickup
// flow from Item; it only defines what collecting it does.
public class OnionItem : Item
{
    public GameObject projectilePrefab;   // the PlayerProjectile the auto-shooter will fire

    // Attaches an AutoShooter to the player (or reuses the existing one if they already have
    // it, so a second Onion just re-points it rather than stacking a second shooter) and
    // hands it the projectile to fire.
    protected override void Apply(PlayerMovement player)
    {
        AutoShooter shooter = player.GetComponent<AutoShooter>();
        if (shooter == null)
            shooter = player.gameObject.AddComponent<AutoShooter>();

        shooter.Configure(projectilePrefab);
    }
}
