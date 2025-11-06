using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLightFromSprite2D : MonoBehaviour
{
    [Header("Light Source")]
    [SerializeField] private Sprite customSprite;

    [Header("Light Settings")]
    [Range(-2f, 2f)][SerializeField] private float offsetX = 0f;
    [Range(-2f, 2f)][SerializeField] private float offsetY = 0f;
    [Range(0f, 2f)][SerializeField] private float intensity = 1f;
    [Range(0.1f, 3f)][SerializeField] private float scale = 1f;
    [SerializeField] private Color lightColor = Color.yellow;

    [Header("Sorting Settings")]
    [SerializeField] private bool useSameSortingAsSource = true;
    [SerializeField] private string customSortingLayer = "Default";
    [SerializeField] private int customSortingOrder = 1;
    [SerializeField] private int orderOffset = 1;

    [Header("Size Controls")]
    [Range(0.1f, 3f)][SerializeField] private float shapeWidth = 1f;
    [Range(0.1f, 3f)][SerializeField] private float shapeHeight = 1f;

    [Header("Pulse Settings")]
    [SerializeField] private bool pulse = false;
    [Range(0.1f, 5f)][SerializeField] private float pulseSpeed = 2f;
    [Range(0.5f, 2f)][SerializeField] private float pulseAmount = 1.2f;

    [Header("Fade Light Settings")]
    [Range(0f, 1f)][SerializeField] private float fadeStrength = 0.3f;
    [Range(0.1f, 10f)][SerializeField] private float fadeDistance = 2f;
    [SerializeField] private Transform fadeTarget;

    private SpriteRenderer source;
    private SpriteRenderer glow;
    private PlayerHiding playerHiding;

    private void OnEnable()
    {
        source = GetComponent<SpriteRenderer>();
        playerHiding = GetComponentInParent<PlayerHiding>();
        FindOrCreateLight();
        UpdateLight();
    }

    private void LateUpdate()
    {
        if (source && glow)
        {
            UpdateLight();

            if (playerHiding != null)
            {
                bool shouldHide = playerHiding.IsHidingInContainer;
                if (glow.enabled == shouldHide)
                    glow.enabled = !shouldHide;
            }
        }
    }

    private void FindOrCreateLight()
    {
        if (glow != null && glow.gameObject != null)
            return;

        foreach (Transform child in transform)
        {
            if (child.name == "SpriteLight")
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    glow = sr;
                    return;
                }
            }
        }

        GameObject go = new GameObject("SpriteLight");
        go.transform.SetParent(transform);
        glow = go.AddComponent<SpriteRenderer>();
        glow.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void UpdateLight()
    {
        if (source == null || glow == null) return;

        glow.sprite = customSprite ? customSprite : source.sprite;
        glow.flipX = source.flipX;
        glow.flipY = source.flipY;

        float pulseFactor = 1f;
        if (pulse && Application.isPlaying)
            pulseFactor = Mathf.Lerp(1f, pulseAmount, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);

        glow.transform.localScale = new Vector3(shapeWidth, shapeHeight, 1f) * scale * pulseFactor;
        glow.transform.localPosition = new Vector3(offsetX, offsetY, 0);

        float finalIntensity = intensity;
        if (fadeTarget != null)
        {
            float dist = Vector2.Distance(transform.position, fadeTarget.position);
            float fadeFactor = Mathf.Clamp01(1f - dist / fadeDistance);
            finalIntensity = intensity * Mathf.Lerp(1f, 0f, (1f - fadeFactor) * fadeStrength);
        }

        Color finalColor = lightColor;
        finalColor.a = finalIntensity;
        glow.color = finalColor;

        if (useSameSortingAsSource)
        {
            glow.sortingLayerID = source.sortingLayerID;
            glow.sortingOrder = source.sortingOrder + orderOffset;
        }
        else
        {
            glow.sortingLayerName = customSortingLayer;
            glow.sortingOrder = customSortingOrder;
        }
    }

    public void ClearLight()
    {
        if (glow != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(glow.gameObject);
            else
                Destroy(glow.gameObject);
#else
            Destroy(glow.gameObject);
#endif
            glow = null;
        }

        foreach (Transform child in transform)
        {
            if (child.name == "SpriteLight")
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

    public void RebuildLight()
    {
        ClearLight();
        FindOrCreateLight();
        UpdateLight();
    }

    private void OnDestroy()
    {
        ClearLight();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpriteLightFromSprite2D))]
    public class SpriteLightFromSprite2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpriteLightFromSprite2D lightScript = (SpriteLightFromSprite2D)target;
            EditorGUILayout.Space();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Light Now", GUILayout.Height(28)))
            {
                lightScript.ClearLight();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Rebuild Light", GUILayout.Height(28)))
            {
                lightScript.RebuildLight();
            }

            GUI.backgroundColor = Color.white;
        }
    }
#endif
}
