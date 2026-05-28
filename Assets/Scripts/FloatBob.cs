using UnityEngine;

public class FloatBob : MonoBehaviour
{
    public float amplitude = 0.08f;  // units up and down
    public float frequency = 1.8f;   // full cycles per second

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        Vector3 pos = startLocalPos;
        pos.y += Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        transform.localPosition = pos;
    }
}
