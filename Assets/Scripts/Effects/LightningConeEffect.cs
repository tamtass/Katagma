using UnityEngine;

// The visual for the player's melee attack: a fan of jagged lightning bolts drawn across the
// attack cone. Each bolt is a LineRenderer whose points are randomly displaced to look electric,
// and re-randomised a few times over the effect's short lifetime so it flickers. Each bolt also
// gets a thicker black outline drawn just beneath it. This is purely cosmetic — the actual hit
// detection lives in PlayerMovement.
public class LightningConeEffect : MonoBehaviour
{
    [Header("Cone")]
    public float coneAngle = 45f;   // total spread of the fan, in degrees (set by the player each attack)

    [Header("Bolt Shape")]
    public int segments = 12;          // line segments per bolt; more = more jagged detail
    public float displacement = 0.25f; // how far the mid-points can wander sideways

    [Header("Timing")]
    public float flickerRate = 0.04f;  // how often the bolts re-randomise
    public float duration = 0.12f;     // how long the whole effect stays on screen

    [Header("Visuals")]
    public float startWidth = 0.06f;   // bolt width at the player end
    public float endWidth = 0.01f;     // bolt width at the tip (tapers to a point)
    public Color color = Color.cyan;
    public Material material;           // optional; falls back to a default sprite material
    public string sortingLayerName = "Default";
    public int sortingOrder = 10;

    [Header("Outline")]
    public bool  outline      = true;
    public float outlineWidth = 0.03f;         // extra width added to each side of the bolt
    public Color outlineColor = Color.black;

    private LineRenderer[] bolts;         // one line per bolt
    private LineRenderer[] boltOutlines;  // matching thicker black line under each bolt
    private Vector3[] directions;         // direction each bolt points
    private Vector3 origin;               // where the bolts start (the player)
    private float range;                  // bolt length
    private float flickerTimer;           // counts down to the next re-randomise
    private float durationTimer;          // counts down to the effect ending
    private bool active;
    private int activeBoltCount;          // how many bolts currently exist

    // Rebuilds the pool of line renderers for a given bolt count (only when the count changes,
    // e.g. the player's projectile count went up). Destroys the old ones and creates a bolt plus
    // its outline for each.
    void RebuildBolts(int count)
    {
        if (bolts != null)
            foreach (var b in bolts)
                if (b != null) Destroy(b.gameObject);
        if (boltOutlines != null)
            foreach (var b in boltOutlines)
                if (b != null) Destroy(b.gameObject);

        activeBoltCount = count;
        bolts        = new LineRenderer[count];
        boltOutlines = new LineRenderer[count];
        directions   = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            // Outline first, thicker, black, one sorting order below so it sits under the bolt.
            boltOutlines[i] = CreateLine("BoltOutline",
                outlineColor, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0f),
                startWidth + outlineWidth * 2f, endWidth + outlineWidth * 2f,
                sortingOrder - 1);

            bolts[i] = CreateLine("Bolt",
                color, new Color(color.r, color.g, color.b, 0f),
                startWidth, endWidth, sortingOrder);
        }
    }

    // Creates one configured LineRenderer (used for both bolts and their outlines). The end colour
    // fades to transparent so the tip tapers out cleanly rather than ending in a hard blob.
    private LineRenderer CreateLine(string name, Color start, Color end, float wStart, float wEnd, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount    = segments + 1;
        lr.startWidth       = wStart;
        lr.endWidth         = wEnd;
        lr.startColor       = start;
        lr.endColor         = end;
        lr.useWorldSpace    = true;
        lr.material         = material != null ? material : new Material(Shader.Find("Sprites/Default"));
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder     = order;
        lr.enabled          = false;   // hidden until Play turns it on

        return lr;
    }

    // Fires the effect. Called by PlayerMovement each attack with where it starts, which way it
    // faces, how long the bolts are, and how many there are. Spreads the bolts evenly across the
    // cone, enables them, and generates their first shape. Also starts the lifetime/flicker timers.
    public void Play(Vector3 worldOrigin, float facingDegrees, float attackRange, int count)
    {
        if (count != activeBoltCount)
            RebuildBolts(count);

        origin = worldOrigin;
        range  = attackRange;

        float half = coneAngle / 2f;
        for (int i = 0; i < activeBoltCount; i++)
        {
            float t     = activeBoltCount > 1 ? (float)i / (activeBoltCount - 1) : 0.5f;
            float angle = facingDegrees + Mathf.Lerp(-half, half, t);   // spread across the cone
            float rad   = angle * Mathf.Deg2Rad;
            directions[i] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            bolts[i].enabled = true;
            if (boltOutlines[i] != null) boltOutlines[i].enabled = outline;
        }

        Regenerate();
        active        = true;
        durationTimer = duration;
        flickerTimer  = flickerRate;
    }

    // Counts the effect down. When the lifetime ends it hides every line; otherwise it re-randomises
    // the bolt shapes on the flicker interval (following the player's position) for a lively look.
    void Update()
    {
        if (!active) return;

        durationTimer -= Time.deltaTime;
        if (durationTimer <= 0f)
        {
            foreach (var b in bolts) b.enabled = false;
            foreach (var b in boltOutlines) b.enabled = false;
            active = false;
            return;
        }

        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            origin = transform.position;
            Regenerate();
            flickerTimer = flickerRate;
        }
    }

    // Builds the jagged shape of every bolt: a straight line from origin to tip, with each
    // in-between point pushed sideways by a random amount. The displacement is largest in the
    // middle and tapers to zero at both ends, so the bolts stay pinned at the player and the tip.
    void Regenerate()
    {
        for (int i = 0; i < activeBoltCount; i++)
        {
            Vector3 dir  = directions[i];
            Vector3 end  = origin + dir * range;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);   // sideways direction for the displacement

            SetPoint(i, 0, origin);
            SetPoint(i, segments, end);

            for (int j = 1; j < segments; j++)
            {
                float   t           = (float)j / segments;
                Vector3 point       = Vector3.Lerp(origin, end, t);
                float   maxDisplace = displacement * (1f - Mathf.Abs(t - 0.5f) * 2f);   // 0 at ends, max at middle
                point += perp * Random.Range(-maxDisplace, maxDisplace);
                SetPoint(i, j, point);
            }
        }
    }

    // Writes a point to both a bolt and its outline, so the outline always traces the identical
    // jagged path underneath.
    private void SetPoint(int i, int index, Vector3 p)
    {
        bolts[i].SetPosition(index, p);
        if (outline && boltOutlines[i] != null) boltOutlines[i].SetPosition(index, p);
    }
}
