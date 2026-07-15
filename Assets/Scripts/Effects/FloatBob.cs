using UnityEngine;

// A tiny idle animation: gently bobs an object up and down on a sine wave. Used to make pickups
// and similar objects feel alive without needing an animation. Purely cosmetic.
public class FloatBob : MonoBehaviour
{
    public float amplitude = 0.08f;  // how far it moves up and down, in units
    public float frequency = 1.8f;   // how many full bobs per second

    private Vector3 startLocalPos;   // the rest position the bob oscillates around

    // Remember where the object starts so the bob is relative to it.
    void Start()
    {
        startLocalPos = transform.localPosition;
    }

    // Offset the Y position by a sine wave each frame.
    void Update()
    {
        Vector3 pos = startLocalPos;
        pos.y += Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        transform.localPosition = pos;
    }
}
