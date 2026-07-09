using UnityEngine;

// A one-shot puff of chunky, pixelated smoke, configured entirely in code so it
// needs no art or hand-tuned ParticleSystem. Emits a dense burst of solid blocky
// particles that expand and fade, then destroys itself.
//
// Pixel-art friendly: the particle texture is tiny and point-filtered, so it scales
// up as hard blocks rather than a smooth blur. Opaque particles (not soft alpha) so
// the puff reads clearly even on a dark floor.
[RequireComponent(typeof(ParticleSystem))]
public class DeathSmoke : MonoBehaviour
{
    [Header("Look")]
    public Color smokeColor  = new(0.06f, 0.06f, 0.06f);   // near-black
    public int   particleCount = 12;
    public float startSize     = 0.35f;
    public float lifetime      = 0.22f;
    public float spread        = 1.5f;                      // outward launch speed

    [Header("Sorting")]
    public string sortingLayerName = "Player";              // same layer the enemies/player render on
    public int    sortingOrder     = 100;                   // high order → draws on top of them

    private static Texture2D _blockCircle;
    private static Material  _material;

    void Awake()
    {
        var ps = GetComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.duration        = lifetime;
        main.startLifetime   = lifetime;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(spread * 0.4f, spread);
        main.startSize       = new ParticleSystem.MinMaxCurve(startSize * 0.6f, startSize);
        main.startColor      = smokeColor;
        main.gravityModifier = -0.08f;                      // drift up like rising smoke
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction      = ParticleSystemStopAction.Destroy;

        // One dense burst rather than a stream.
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        // Emit from a small disc so the puff starts with some body.
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        // Slight grow as they travel.
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.2f));

        // Stay opaque, then snap-fade near the end so it doesn't wash out to a faint haze.
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(smokeColor, 0f), new GradientColorKey(smokeColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = grad;

        var psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.material         = GetMaterial();
        psRenderer.sortingLayerName = sortingLayerName;
        psRenderer.sortingOrder     = sortingOrder;

        ps.Play();
    }

    // Alpha-blended (not additive — additive black is invisible), shared across puffs.
    private static Material GetMaterial()
    {
        if (_material != null) return _material;
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        _material = new Material(shader) { mainTexture = GetBlockCircle() };
        return _material;
    }

    // A tiny, hard-edged white disc. Point filtering keeps it blocky when scaled up,
    // matching the pixel-art style. White so the particle colour tints it directly.
    private static Texture2D GetBlockCircle()
    {
        if (_blockCircle != null) return _blockCircle;

        const int size = 10;
        _blockCircle = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,   // no smoothing — hard pixel blocks
            wrapMode   = TextureWrapMode.Clamp
        };

        Vector2 c    = new((size - 1) / 2f, (size - 1) / 2f);
        float   maxR = size / 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Hard cutoff = solid opaque disc, no soft gradient.
                bool inside = Vector2.Distance(new Vector2(x, y), c) <= maxR - 0.5f;
                _blockCircle.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
            }

        _blockCircle.Apply();
        return _blockCircle;
    }
}
