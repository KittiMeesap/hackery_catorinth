using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIKeyIconUpdater : MonoBehaviour
{
    public LogicalInput logicalInput;
    public Image iconImage;

    [Header("Size Settings")]
    public Vector2 squareSize = new Vector2(64, 64);
    public Vector2 wideSize = new Vector2(128, 64);

    [Header("World Space Settings")]
    [Tooltip("Scale multiplier for World Space Canvas")]
    public float worldScaleMultiplier = 0.01f;

    private RectTransform rect;
    private Canvas parentCanvas;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (!iconImage)
            iconImage = GetComponent<Image>();

        parentCanvas = GetComponentInParent<Canvas>();
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

        // SET SPRITE
        Sprite s = KeyIconDatabase.GetIcon(logicalInput);
        if (s != null)
            iconImage.sprite = s;

        // SET SIZE
        ApplySize();
    }

    private void ApplySize()
    {
        if (parentCanvas == null) return;

        KeyIconSizeType sizeType = KeyIconDatabase.GetSizeType(logicalInput);
        Vector2 targetSize = sizeType == KeyIconSizeType.Wide128
            ? wideSize
            : squareSize;

        if (parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            rect.sizeDelta = targetSize;
            rect.localScale = Vector3.one * worldScaleMultiplier;
        }
        else
        {
            rect.localScale = Vector3.one;
            rect.sizeDelta = targetSize;
        }
    }
}
