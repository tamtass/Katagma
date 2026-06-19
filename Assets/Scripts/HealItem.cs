using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealItem : MonoBehaviour
{
    public float healAmount = 20f;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerMovement>(out var player)) return;
        if (player.health >= player.maxHealth) return;
        player.Heal(healAmount);
        Destroy(gameObject);
    }
}
