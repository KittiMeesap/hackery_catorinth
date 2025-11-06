// EnemyEmotionUI.cs
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyEmotionUI : MonoBehaviour
{
    [Header("UI References (auto)")]
    public Image baseImage;          // "EmotionBase"
    public Image iconImage;          // "EmotionIcon"

    [Header("Follow (auto)")]
    public Transform followTarget;
    public Camera worldSpaceCamera;

    [Header("Icon Sprites")]
    public Sprite alertSprite;
    public Sprite questionSprite;

    [Header("Drive Mode")]
    public bool driveFromController = false;

    [Header("Detect Player (self scan)")]
    public LayerMask playerLayers = default;
    public LayerMask obstacleLayers = default;
    public float detectRadius = 1.6f;
    [Range(0f, 180f)] public float frontAngle = 70f;
    public bool useLineOfSight = true;
    public bool countTriggers = true;
    public float losOriginYOffset = 0.4f;
    public float lingerTime = 0.25f;

    [Header("Layout / Facing")]
    public SpriteRenderer facingSprite;

    [Header("Start State")]
    public bool hideOnStart = true;

    [Header("Debug")]
    public bool drawGizmos = false;
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.4f);

    // ----- runtime -----
    RectTransform baseRect, iconRect;
    Vector3 placedBaseLocal;
    float baseAbsX;
    Vector3 iconDeltaFromBase;
    float iconDeltaAbsX;
    Vector3 iconInitialScale;
    Vector3 canvasLocalScaleMag;

    ContactFilter2D playerFilter;
    readonly Collider2D[] hits = new Collider2D[16];

    enum UIState { Hidden, Alert }
    UIState state = UIState.Hidden;

    float lastSeenTime = -999f;

    void Reset()
    {
        playerLayers = LayerMask.GetMask("Player");
        obstacleLayers = LayerMask.GetMask("Obstacle");
    }

    void Awake()
    {
        if (!followTarget) followTarget = transform.root;

        if (!baseImage) baseImage = transform.Find("EmotionBase")?.GetComponent<Image>();
        if (!iconImage) iconImage = transform.Find("EmotionIcon")?.GetComponent<Image>();

        if (!baseImage || !iconImage)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            if (!baseImage && imgs.Length > 0) baseImage = imgs[0];
            if (!iconImage && imgs.Length > 1) iconImage = imgs[1];
        }

        baseRect = baseImage ? baseImage.rectTransform : null;
        iconRect = iconImage ? iconImage.rectTransform : null;

        if (baseRect && iconRect && iconRect.parent == baseRect)
            iconRect.SetParent(baseRect.parent, true);

        canvasLocalScaleMag = new Vector3(
            Mathf.Abs(transform.localScale.x),
            Mathf.Abs(transform.localScale.y),
            Mathf.Abs(transform.localScale.z)
        );

        if (baseRect)
        {
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
            iconDeltaFromBase = iconRect.localPosition - placedBaseLocal;
            iconDeltaAbsX = Mathf.Abs(iconDeltaFromBase.x);
        }

        if (!facingSprite) facingSprite = followTarget ? followTarget.GetComponentInChildren<SpriteRenderer>() : null;

        playerFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = playerLayers,
            useTriggers = countTriggers
        };

        if (hideOnStart) SetState(UIState.Hidden, true); else SetState(UIState.Alert, true);
    }

    void Start()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && !canvas.worldCamera)
            canvas.worldCamera = worldSpaceCamera ? worldSpaceCamera : Camera.main;
    }

    int GetFacingSign()
    {
        if (facingSprite != null) return facingSprite.flipX ? -1 : 1;
        float s = Mathf.Sign(followTarget ? followTarget.lossyScale.x : 1f);
        return s == 0 ? 1 : (int)s;
    }

    void LateUpdate()
    {
        if (!followTarget || !baseRect) return;

        transform.position = followTarget.position;
        transform.rotation = Quaternion.identity;

        int facingSign = GetFacingSign();

        transform.localScale = new Vector3(
            canvasLocalScaleMag.x * (facingSign < 0 ? -1f : 1f),
            canvasLocalScaleMag.y,
            canvasLocalScaleMag.z
        );

        float baseX = baseAbsX * facingSign;
        baseRect.localPosition = new Vector3(baseX, placedBaseLocal.y, placedBaseLocal.z);

        var bs = baseRect.localScale;
        float mag = Mathf.Abs(bs.x);
        bs.x = mag * (facingSign >= 0 ? 1f : -1f);
        baseRect.localScale = bs;

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
        if (!driveFromController)
        {
            if (SelfScanSawPlayer())
            {
                SetState(UIState.Alert);
                lastSeenTime = Time.time;
            }
            else if (Time.time - lastSeenTime > lingerTime)
            {
                SetState(UIState.Hidden);
            }
        }
    }

    bool SelfScanSawPlayer()
    {
        if (!followTarget) return false;

        int count = Physics2D.OverlapCircle((Vector2)followTarget.position, detectRadius, playerFilter, hits);
        if (count <= 0) return false;

        Vector2 origin = (Vector2)followTarget.position + new Vector2(0f, losOriginYOffset);
        Vector2 facing = new Vector2(GetFacingSign(), 0f);

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

    // -------- Public API for controller driving --------
    public void ForceAlert()
    {
        SetState(UIState.Alert);
        lastSeenTime = Time.time;
    }

    public void ForceHidden()
    {
        SetState(UIState.Hidden);
    }

    /// <summary>Show alert now and auto-hide after at least 'seconds' (respects lingerTime if seconds is smaller).</summary>
    public void PulseAlert(float seconds)
    {
        SetState(UIState.Alert);
        float hold = Mathf.Max(lingerTime, seconds);
        lastSeenTime = Time.time - (lingerTime - hold);
    }
    // ---------------------------------------------------

    void SetState(UIState next, bool force = false)
    {
        if (!force && state == next) return;
        state = next;

        switch (state)
        {
            case UIState.Hidden:
                if (iconImage) iconImage.gameObject.SetActive(false);
                if (baseImage) baseImage.gameObject.SetActive(false);
                break;

            case UIState.Alert:
                if (baseImage) baseImage.gameObject.SetActive(true);
                if (iconImage)
                {
                    if (alertSprite) iconImage.sprite = alertSprite;
                    iconImage.gameObject.SetActive(true);
                }
                break;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !followTarget) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(followTarget.position, detectRadius);

        Vector3 origin = followTarget.position + new Vector3(0f, losOriginYOffset, 0f);
        Vector3 dir = new Vector3(GetFacingSign(), 0f, 0f);
        Gizmos.DrawLine(origin, origin + dir * detectRadius);
    }
#endif
}
