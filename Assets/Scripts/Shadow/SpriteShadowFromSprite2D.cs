using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteShadowFromSprite2D : MonoBehaviour
{
    [Header("Shadow Source")]
    [SerializeField] private Sprite customSprite;

    [Header("Shadow Settings")]
    [Range(-2f, 2f)][SerializeField] private float offsetX = 0.15f;
    [Range(-2f, 2f)][SerializeField] private float offsetY = -0.15f;
    [Range(0f, 1f)][SerializeField] private float opacity = 0.5f;
    [Range(0.1f, 3f)][SerializeField] private float scale = 1f;
    [SerializeField] private Color color = Color.black;
    [SerializeField] private int orderOffset = -1;

    [Header("Size Controls")]
    [Range(0.1f, 3f)][SerializeField] private float shapeWidth = 1f;
    [Range(0.1f, 3f)][SerializeField] private float shapeHeight = 1f;

    [Header("Flip Settings")]
    [SerializeField] private bool flipX = false;
    [SerializeField] private bool flipY = false;

    [Header("Light Direction Settings")]
    [Range(-1f, 1f)][SerializeField] private float lightDirX = 1f;
    [Range(-1f, 1f)][SerializeField] private float lightDirY = -1f;
    [Range(0f, 1f)][SerializeField] private float lightRotationInfluence = 1f;
    [Range(0f, 1f)][SerializeField] private float skewAmount = 0.3f;

    [Header("Fade Shadow Settings")]
    [Range(0f, 1f)][SerializeField] private float fadeStrength = 0.3f;
    [Range(0.1f, 10f)][SerializeField] private float fadeDistance = 2f;
    [SerializeField] private Transform fadeTarget;

    private SpriteRenderer source;
    private SpriteRenderer shadow;
    private PlayerHiding playerHiding;

    private void OnEnable()
    {
        source = GetComponent<SpriteRenderer>();
        playerHiding = GetComponentInParent<PlayerHiding>();
        FindOrCreateShadow();
        UpdateShadow();
    }

    private void LateUpdate()
    {
        if (source && shadow)
        {
            UpdateShadow();

            if (playerHiding != null)
            {
                bool shouldHide = playerHiding.IsHidingInContainer;
                if (shadow.enabled == shouldHide)
                    shadow.enabled = !shouldHide;
            }
        }
    }

    private void FindOrCreateShadow()
    {
        if (shadow != null && shadow.gameObject != null)
            return;

        foreach (Transform child in transform)
        {
            if (child.name == "SpriteShadow")
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    shadow = sr;
                    return;
                }
            }
        }

        GameObject go = new GameObject("SpriteShadow");
        go.transform.SetParent(transform);
        shadow = go.AddComponent<SpriteRenderer>();
        shadow.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void UpdateShadow()
    {
        if (source == null || shadow == null) return;

        shadow.sprite = customSprite ? customSprite : source.sprite;
        shadow.flipX = source.flipX ^ flipX;
        shadow.flipY = source.flipY ^ flipY;

        Vector2 lightDir = new Vector2(lightDirX, lightDirY).normalized;
        float lightAngle = Mathf.Atan2(lightDir.y, lightDir.x) * Mathf.Rad2Deg;
        shadow.transform.localRotation = Quaternion.Euler(0, 0, -lightAngle * lightRotationInfluence);

        Vector3 skewScale = new Vector3(
            shapeWidth * (1 + Mathf.Abs(lightDir.x) * skewAmount),
            shapeHeight * (1 + Mathf.Abs(lightDir.y) * skewAmount),
            1f
        );
        shadow.transform.localScale = skewScale * scale;

        if (source.sprite != null)
        {
            Bounds b = source.sprite.bounds;
            Vector3 basePos = new Vector3(0, -b.extents.y * 0f, 0);
            shadow.transform.localPosition = basePos + new Vector3(offsetX, offsetY, 0);
        }
        else
        {
            shadow.transform.localPosition = new Vector3(offsetX, offsetY, 0);
        }

        float finalOpacity = opacity;
        if (fadeTarget != null)
        {
            float dist = Vector2.Distance(transform.position, fadeTarget.position);
            float fadeFactor = Mathf.Clamp01(1f - dist / fadeDistance);
            finalOpacity = opacity * Mathf.Lerp(1f, 0f, (1f - fadeFactor) * fadeStrength);
        }

        shadow.color = new Color(color.r, color.g, color.b, finalOpacity);
        shadow.sortingLayerID = source.sortingLayerID;
        shadow.sortingOrder = source.sortingOrder + orderOffset;
    }

    public void ClearShadow()
    {
        if (shadow != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(shadow.gameObject);
            else
                Destroy(shadow.gameObject);
#else
            Destroy(shadow.gameObject);
#endif
            shadow = null;
        }

        foreach (Transform child in transform)
        {
            if (child.name == "SpriteShadow")
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    public void RebuildShadow()
    {
        ClearShadow();
        FindOrCreateShadow();
        UpdateShadow();
    }

    private void OnDestroy()
    {
        ClearShadow();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpriteShadowFromSprite2D))]
    public class SpriteShadowFromSprite2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpriteShadowFromSprite2D shadowScript = (SpriteShadowFromSprite2D)target;
            EditorGUILayout.Space();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Shadow Now", GUILayout.Height(28)))
            {
                shadowScript.ClearShadow();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Rebuild Shadow", GUILayout.Height(28)))
            {
                shadowScript.RebuildShadow();
            }

            GUI.backgroundColor = Color.white;
        }
    }
#endif
}
