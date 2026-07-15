using UnityEngine;

// A small heal pickup (the kind enemies can drop), separate from the item-room items. It's
// simpler: no pickup animation or freeze, it just heals on contact and vanishes. It refuses
// to be collected at full health so the player doesn't waste it.
[RequireComponent(typeof(Collider2D))]
public class HealItem : MonoBehaviour
{
    public float healAmount = 20f;

    // Make the collider a trigger so the player walks over it.
    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    // Heal the player on contact (unless already full), play the sound, and remove the pickup.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerMovement>(out var player)) return;
        if (player.health >= player.maxHealth) return;
        player.Heal(healAmount);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayHealPickup();
        Destroy(gameObject);
    }
}
