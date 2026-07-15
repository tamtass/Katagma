using UnityEngine;

// A quick puff of chunky, pixelated smoke played when an enemy dies. The whole particle system
// is configured in code so it needs no art and no hand-tuned ParticleSystem asset. To fit the
// pixel-art look, the particle texture is tiny and point-filtered (so it scales up as hard
// blocks, not a blur) and the particles are opaque, so the puff reads clearly even on a dark
// floor. It destroys itself once the burst finishes.
[RequireComponent(typeof(ParticleSystem))]
public class DeathSmoke : MonoBehaviour
{
    [Header("Look")]
    public Color smokeColor  = new(0.06f, 0.06f, 0.06f);   // near-black
    public int   particleCount = 12;      // particles in the single burst
    public float startSize     = 0.35f;   // base particle size
    public float lifetime      = 0.22f;   // how long the puff lasts
    public float spread        = 1.5f;    // outward launch speed

    [Header("Sorting")]
    public string sortingLayerName = "Player";   // the layer the enemies/player render on
    public int    sortingOrder     = 100;         // high order so it draws on top of them

    // Texture and material are built once and shared across every puff, to avoid re-allocating.
    private static Texture2D _blockCircle;
    private static Material  _material;

    // Configures the whole effect on spawn: a single upward-drifting burst of opaque particles
    // that grow slightly and fade out at the end, using the shared blocky material. The system
    // is set to destroy its GameObject when it finishes.
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
        main.gravityModifier = -0.08f;   // slight upward drift, like rising smoke
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction      = ParticleSystemStopAction.Destroy;

        // Fire everything in one burst instead of a continuous stream.
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        // Spawn from a small disc so the puff has some initial body.
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        // Grow a little as they travel.
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.2f));

        // Hold full opacity, then snap-fade at the end so it vanishes cleanly rather than
        // lingering as a faint haze.
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

    // Builds (once) the shared material. It uses an alpha-blended particle shader rather than the
    // default additive one, because additive black is invisible — dark smoke needs alpha blending.
    private static Material GetMaterial()
    {
        if (_material != null) return _material;
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        _material = new Material(shader) { mainTexture = GetBlockCircle() };
        return _material;
    }

    // Builds (once) a small, hard-edged white disc texture. It's white so the particle colour
    // tints it directly, and point-filtered so it scales up as chunky pixels for the pixel-art look.
    private static Texture2D GetBlockCircle()
    {
        if (_blockCircle != null) return _blockCircle;

        const int size = 10;
        _blockCircle = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,   // no smoothing — keep the pixels crisp
            wrapMode   = TextureWrapMode.Clamp
        };

        Vector2 c    = new((size - 1) / 2f, (size - 1) / 2f);
        float   maxR = size / 2f;

        // Fill a solid opaque circle; everything outside stays transparent.
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = Vector2.Distance(new Vector2(x, y), c) <= maxR - 0.5f;
                _blockCircle.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
            }

        _blockCircle.Apply();
        return _blockCircle;
    }
}
