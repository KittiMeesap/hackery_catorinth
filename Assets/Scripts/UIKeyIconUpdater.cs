using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class UIKeyIconUpdater : MonoBehaviour
{
    public LogicalInput logicalInput;

    [Header("Size Settings")]
    public Vector2 squareSize = new Vector2(64, 64);
    public Vector2 wideSize = new Vector2(128, 64);

    [Header("World Space Settings")]
    public float worldScaleMultiplier = 0.01f;

    private RectTransform rect;
    private Image iconImage;
    private Canvas parentCanvas;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();
        parentCanvas = GetComponentInParent<Canvas>();

        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
    }

    private void OnEnable()
    {
        UpdateIcon();

        if (GameInput.Instance != null)
            GameInput.Instance.ControlSchemeChanged += UpdateIcon;
    }

    private void OnDisable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.ControlSchemeChanged -= UpdateIcon;
    }

    private void UpdateIcon()
    {
        if (!iconImage) return;

        // ===== SET SPRITE =====
        Sprite sprite = KeyIconDatabase.GetIcon(logicalInput);
        if (sprite != null)
            iconImage.sprite = sprite;

        ApplySize();
    }

    private void ApplySize()
    {
        if (parentCanvas == null) return;

        KeyIconSizeType sizeType = KeyIconDatabase.GetSizeType(logicalInput);

        Vector2 targetSize =
            sizeType == KeyIconSizeType.Wide128
                ? wideSize
                : squareSize;

        rect.sizeDelta = targetSize;

        // World space scaling
        if (parentCanvas.renderMode == RenderMode.WorldSpace)
            rect.localScale = Vector3.one * worldScaleMultiplier;
        else
            rect.localScale = Vector3.one;
    }
}
