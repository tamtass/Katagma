using System.Collections;
using UnityEngine;

// Base class for the "big" item-room items (the ones the player walks over to collect for a
// permanent effect). It handles the shared pickup flow — detecting the player, freezing them,
// playing the fly-into-the-player animation, and then applying the effect. Each concrete item
// only has to say what its effect is (Apply) and, optionally, when it can be picked up (CanPickUp).
[RequireComponent(typeof(Collider2D))]
public abstract class Item : MonoBehaviour
{
    [SerializeField] private float freezeDuration = 1.5f;   // how long the pickup animation/player freeze lasts

    private bool _triggered;   // guards against picking up twice while the animation plays

    // Force the collider to be a trigger so the player walks through it rather than bumping it.
    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // When the player steps on the item: check it's actually the player and that pickup is
    // allowed, then lock it in (disable the collider, mark triggered), play the sound, and
    // start the pickup animation.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerMovement>(out var player)) return;
        if (!CanPickUp(player)) return;

        _triggered = true;
        GetComponent<Collider2D>().enabled = false;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayItemPickup();
        StartCoroutine(PickupRoutine(player));
    }

    // Freezes the player, then animates the item shrinking as it flies into them. Once the
    // animation finishes, applies the effect and destroys the item.
    private IEnumerator PickupRoutine(PlayerMovement player)
    {
        float duration = freezeDuration;
        player.FreezeForPickup(duration);

        Vector3 startPos   = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position   = Vector3.Lerp(startPos, player.transform.position, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Apply(player);
        Destroy(gameObject);
    }

    // Optional gate: subclasses can override to refuse pickup in some state. Default: always allowed.
    protected virtual bool CanPickUp(PlayerMovement player) => true;

    // The actual effect of the item. Each concrete item implements this.
    protected abstract void Apply(PlayerMovement player);
}
