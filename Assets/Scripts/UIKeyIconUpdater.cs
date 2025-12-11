using UnityEngine;
using UnityEngine.UI;

public class UIKeyIconUpdater : MonoBehaviour
{
    public string logicalKey = "confirm";
    public Image iconImage;

    private void OnEnable()
    {
        if (!iconImage)
            iconImage = GetComponent<Image>();

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

        Sprite icon = KeyIconDatabase.GetIcon(logicalKey);
        if (icon != null)
            iconImage.sprite = icon;
    }
}
