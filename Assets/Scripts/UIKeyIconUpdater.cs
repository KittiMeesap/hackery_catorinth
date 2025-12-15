using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class UIKeyIconUpdater : MonoBehaviour
{
    public LogicalInput logicalInput;

    [Header("Height Based Sizing")]
    public float squareHeight = 64f;
    public float wideHeight = 48f;

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

        Sprite sprite = KeyIconDatabase.GetIcon(logicalInput);
        if (sprite == null) return;

        iconImage.sprite = sprite;
        ApplySize(sprite);
    }

    private void ApplySize(Sprite sprite)
    {
        if (parentCanvas == null) return;

        KeyIconSizeType sizeType = KeyIconDatabase.GetSizeType(logicalInput);

        // LOCK HEIGHT
        float targetHeight =
            sizeType == KeyIconSizeType.Wide128
                ? wideHeight
                : squareHeight;

        // CALCULATE WIDTH FROM ASPECT
        float aspect = sprite.rect.width / sprite.rect.height;
        float targetWidth = targetHeight * aspect;

        rect.sizeDelta = new Vector2(targetWidth, targetHeight);

        // World space scaling
        if (parentCanvas.renderMode == RenderMode.WorldSpace)
            rect.localScale = Vector3.one * worldScaleMultiplier;
        else
            rect.localScale = Vector3.one;
    }
}
