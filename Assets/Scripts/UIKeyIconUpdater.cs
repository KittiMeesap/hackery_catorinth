using UnityEngine;
using UnityEngine.UI;

public class UIKeyIconUpdater : MonoBehaviour
{
    public LogicalInput logicalInput;
    public Image iconImage;

    private void Awake()
    {
        if (!iconImage)
            iconImage = GetComponent<Image>();
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

        Sprite s = KeyIconDatabase.GetIcon(logicalInput);
        if (s != null)
            iconImage.sprite = s;
    }
}