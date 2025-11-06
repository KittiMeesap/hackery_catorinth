using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerEmotionUI : MonoBehaviour
{
    // --- Public just for visibility; you don't need to assign these ---
    [Header("UI References (auto)")]
    public Image baseImage;     // EmotionBase image
    public Image iconImage;     // EmotionIcon image

    [Header("Follow (auto)")]
    public Transform followTarget;     // usually Player (root)
    public Camera worldSpaceCamera;    // auto = Camera.main

    // --- Detection ---
    [Header("Detect Enemies")]
    public LayerMask enemyLayers;
    public LayerMask obstacleLayers;
    public float detectRadius = 1.2f;
    [UnityEngine.Range(0f, 180f)] public float frontAngle = 60f;
    public bool useLineOfSight = true;
    public bool countTriggers = true;
    public float losOriginYOffset = 0.4f;
    public float lingerTime = 0.35f;

    [Header("Start State")]
    public bool hideOnStart = true;

    // --- Internals (no setup needed) ---
    RectTransform baseRect, iconRect;
    Vector3 placedBaseLocal;   // right-side layout you placed in editor
    float baseAbsX;
    Vector3 iconDeltaFromBase; // (icon - base) in their common parent
    float iconDeltaAbsX;
    Vector3 iconInitialScale;
    Vector3 canvasLocalScaleMag; // keep positive magnitudes

    ContactFilter2D enemyFilter;
    readonly Collider2D[] hits = new Collider2D[16];
    float lastSeenTime = -999f;

    void Reset()
    {
        // Try to auto-pick sensible default layer masks
        enemyLayers = LayerMask.GetMask("Enemy");
        obstacleLayers = LayerMask.GetMask("Obstacle");
    }

    void Awake()
    {
        // ---- Auto-wire references ----
        if (!followTarget)
        {
            // Prefer the Player root (if Canvas is child of Player)
            var p = transform.root;
            followTarget = p != null ? p : transform;
        }

        // Find base/icon by name first; fallback to first two Images found
        if (!baseImage)
        {
            var tr = transform.Find("EmotionBase");
            baseImage = tr ? tr.GetComponent<Image>() : null;
        }
        if (!iconImage)
        {
            var tr = transform.Find("EmotionIcon");
            iconImage = tr ? tr.GetComponent<Image>() : null;
        }
        if (!baseImage || !iconImage)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (!baseImage && img.name.ToLower().Contains("base")) baseImage = img;
                else if (!iconImage && img.name.ToLower().Contains("icon")) iconImage = img;
            }
            // fallbacks
            if (!baseImage && imgs.Length > 0) baseImage = imgs[0];
            if (!iconImage && imgs.Length > 1) iconImage = imgs[1];
        }

        baseRect = baseImage ? baseImage.rectTransform : null;
        iconRect = iconImage ? iconImage.rectTransform : null;

        // Ensure base & icon are siblings (icon never inherits base's flip)
        if (baseRect && iconRect && iconRect.parent == baseRect)
            iconRect.SetParent(baseRect.parent, true);

        canvasLocalScaleMag = new Vector3(
            Mathf.Abs(transform.localScale.x),
            Mathf.Abs(transform.localScale.y),
            Mathf.Abs(transform.localScale.z)
        );

        if (baseRect)
        {
            // Treat your editor placement as RIGHT side
            placedBaseLocal = baseRect.localPosition;
            baseAbsX = Mathf.Abs(placedBaseLocal.x);
        }

        if (iconRect && baseRect)
        {
            iconInitialScale = new Vector3(
                Mathf.Abs(iconRect.localScale.x),
                Mathf.Abs(iconRect.localScale.y),
                Mathf.Abs(iconRect.localScale.z)
            );
            iconDeltaFromBase = iconRect.localPosition - placedBaseLocal; // right-side delta
            iconDeltaAbsX = Mathf.Abs(iconDeltaFromBase.x);
        }

        // Detection filter
        enemyFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = enemyLayers,
            useTriggers = countTriggers
        };

        if (hideOnStart) Hide(); else Show();
    }

    void Start()
    {
        // Auto-assign world camera for World Space Canvas
        var canvas = GetComponent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && !canvas.worldCamera)
            canvas.worldCamera = worldSpaceCamera ? worldSpaceCamera : Camera.main;
    }

    void LateUpdate()
    {
        if (!followTarget || !baseRect) return;

        // Follow player; keep upright
        transform.position = followTarget.position;
        transform.rotation = Quaternion.identity;

        // True facing from world scale (robust regardless of flip method)
        float parentSignX = Mathf.Sign(followTarget.lossyScale.x);
        if (parentSignX == 0) parentSignX = 1f;

        // Neutralize parent's flip at the canvas so children never get flipped
        transform.localScale = new Vector3(
            canvasLocalScaleMag.x * (parentSignX < 0 ? -1f : 1f),
            canvasLocalScaleMag.y,
            canvasLocalScaleMag.z
        );

        // Swap side based on facing
        int facingSign = (parentSignX >= 0f) ? 1 : -1;

        // Base position/flip (use your RIGHT-side placement as reference)
        float baseX = baseAbsX * facingSign;
        baseRect.localPosition = new Vector3(baseX, placedBaseLocal.y, placedBaseLocal.z);

        var bs = baseRect.localScale;
        float baseMag = Mathf.Abs(bs.x);
        bs.x = baseMag * (facingSign >= 0 ? 1f : -1f);   // flip base only
        baseRect.localScale = bs;

        // Icon moves to the other side (never flipped)
        if (iconRect)
        {
            float ix = iconDeltaAbsX * facingSign;
            iconRect.localPosition = baseRect.localPosition + new Vector3(ix, iconDeltaFromBase.y, iconDeltaFromBase.z);
            iconRect.localScale = iconInitialScale;
            iconRect.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (!baseRect || !iconRect) return;

        if (ScanEnemyAhead())
        {
            Show();
            lastSeenTime = Time.time;
        }
        else if (Time.time - lastSeenTime > lingerTime)
        {
            Hide();
        }
    }

    bool ScanEnemyAhead()
    {
        int count = Physics2D.OverlapCircle((Vector2)followTarget.position, detectRadius, enemyFilter, hits);
        if (count <= 0) return false;

        Vector2 origin = (Vector2)followTarget.position + new Vector2(0f, losOriginYOffset);
        float face = Mathf.Sign(followTarget.lossyScale.x);
        if (face == 0) face = 1f;
        Vector2 facing = new Vector2(face, 0f);

        for (int i = 0; i < count; i++)
        {
            var col = hits[i]; if (!col) continue;
            if (!countTriggers && col.isTrigger) continue;

            Vector2 toTgt = (Vector2)col.bounds.center - origin;
            float dist = toTgt.magnitude;
            if (dist < 0.001f) continue;
            if (Vector2.Angle(facing, toTgt.normalized) > frontAngle * 0.5f) continue;

            if (useLineOfSight)
            {
                var hit = Physics2D.Raycast(origin, toTgt.normalized, dist, obstacleLayers);
                if (hit.collider != null) continue;
            }
            return true;
        }
        return false;
    }

    // Visibility
    void Show()
    {
        if (baseImage) baseImage.gameObject.SetActive(true);
        if (iconImage) iconImage.gameObject.SetActive(true);
    }
    void Hide()
    {
        if (iconImage) iconImage.gameObject.SetActive(false);
        if (baseImage) baseImage.gameObject.SetActive(false);
    }
}
