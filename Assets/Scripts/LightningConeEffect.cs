using UnityEngine;

public class LightningConeEffect : MonoBehaviour
{
    [Header("Cone")]
    public float coneAngle = 45f;

    [Header("Bolt Shape")]
    public int segments = 12;
    public float displacement = 0.25f;

    [Header("Timing")]
    public float flickerRate = 0.04f;
    public float duration = 0.12f;

    [Header("Visuals")]
    public float startWidth = 0.06f;
    public float endWidth = 0.01f;
    public Color color = Color.cyan;
    public Material material;
    public string sortingLayerName = "Default";
    public int sortingOrder = 10;

    [Header("Outline")]
    public bool  outline      = true;
    public float outlineWidth = 0.03f;         // extra width added to each side
    public Color outlineColor = Color.black;

    private LineRenderer[] bolts;
    private LineRenderer[] boltOutlines;        // thicker black lines drawn under each bolt
    private Vector3[] directions;
    private Vector3 origin;
    private float range;
    private float flickerTimer;
    private float durationTimer;
    private bool active;
    private int activeBoltCount;

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
            // Outline first, one order below, thicker and black — sits under the bolt.
            boltOutlines[i] = CreateLine("BoltOutline",
                outlineColor, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0f),
                startWidth + outlineWidth * 2f, endWidth + outlineWidth * 2f,
                sortingOrder - 1);

            bolts[i] = CreateLine("Bolt",
                color, new Color(color.r, color.g, color.b, 0f),
                startWidth, endWidth, sortingOrder);
        }
    }

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
        lr.enabled          = false;

        return lr;
    }

    // count = projectileCount from PlayerMovement
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
            float angle = facingDegrees + Mathf.Lerp(-half, half, t);
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

    void Regenerate()
    {
        for (int i = 0; i < activeBoltCount; i++)
        {
            Vector3 dir  = directions[i];
            Vector3 end  = origin + dir * range;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

            SetPoint(i, 0, origin);
            SetPoint(i, segments, end);

            for (int j = 1; j < segments; j++)
            {
                float   t           = (float)j / segments;
                Vector3 point       = Vector3.Lerp(origin, end, t);
                float   maxDisplace = displacement * (1f - Mathf.Abs(t - 0.5f) * 2f);
                point += perp * Random.Range(-maxDisplace, maxDisplace);
                SetPoint(i, j, point);
            }
        }
    }

    // Writes a point to both the bolt and its outline so they trace the same jagged path.
    private void SetPoint(int i, int index, Vector3 p)
    {
        bolts[i].SetPosition(index, p);
        if (outline && boltOutlines[i] != null) boltOutlines[i].SetPosition(index, p);
    }
}
