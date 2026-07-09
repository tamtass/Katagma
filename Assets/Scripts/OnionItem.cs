using UnityEngine;

// Onion — "The power to weep arrows." Grants the player a passive auto-shooter that
// fires at the nearest enemy. Lives in the item-room pool, like IncreaseMaxHealthItem.
public class OnionItem : Item
{
    public GameObject projectilePrefab;

    protected override void Apply(PlayerMovement player)
    {
        // Reuse an existing shooter if the player already has one (a second Onion just
        // re-points it), otherwise attach a fresh one.
        AutoShooter shooter = player.GetComponent<AutoShooter>();
        if (shooter == null)
            shooter = player.gameObject.AddComponent<AutoShooter>();

        shooter.Configure(projectilePrefab);
    }
}
