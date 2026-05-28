using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Item : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerMovement>(out var player)) return;
        if (!CanPickUp(player)) return;

        Apply(player);
        Destroy(gameObject);
    }

    protected virtual bool CanPickUp(PlayerMovement player) => true;
    protected abstract void Apply(PlayerMovement player);
}
