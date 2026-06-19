using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Item : MonoBehaviour
{
    [SerializeField] private float freezeDuration = 1.5f;

    private bool _triggered;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerMovement>(out var player)) return;
        if (!CanPickUp(player)) return;

        _triggered = true;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(PickupRoutine(player));
    }

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

    protected virtual bool CanPickUp(PlayerMovement player) => true;
    protected abstract void Apply(PlayerMovement player);
}
