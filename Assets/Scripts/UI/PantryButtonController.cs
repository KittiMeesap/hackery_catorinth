using UnityEngine;
using UnityEngine.UI;

public class PantryButtonController : MonoBehaviour
{
    [Header("UI")]
    public Image background;
    public Image ingredientIconImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.75f, 0.9f, 1f);
    public Color selectedColor = new Color(0.65f, 1f, 0.65f);
    public Color lockedColor = new Color(0.45f, 0.45f, 0.45f);
    public Color selectedHighlightColor = new Color(0.35f, 0.85f, 0.35f);

    private IngredientItemSO ingredient;

    private bool isLocked = false;
    private bool isSelected = false;
    private bool isHighlighted = false;

    public IngredientItemSO Ingredient => ingredient;
    public bool IsLocked() => isLocked;
    public bool IsSelected() => isSelected;

    private void Awake()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (ingredientIconImage == null)
            ingredientIconImage = transform.Find("Icon")?.GetComponent<Image>();
    }

    public void SetIngredient(IngredientItemSO ing)
    {
        ingredient = ing;
        if (ingredientIconImage)
            ingredientIconImage.sprite = ing.icon;

        RefreshColor();
    }

    // Highlight (cursor focus)
    public void SetHighlight(bool active)
    {
        isHighlighted = active;
        RefreshColor();
    }

    // Selected (already chosen)
    public void SetSelected(bool active)
    {
        isSelected = active;
        RefreshColor();
    }

    // Locked (cannot pick)
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (isLocked)
        {
            background.color = lockedColor;
            return;
        }

        if (isSelected && isHighlighted)
        {
            background.color = selectedHighlightColor;
            return;
        }

        if (isSelected)
        {
            background.color = selectedColor;
            return;
        }

        if (isHighlighted)
        {
            background.color = highlightColor;
            return;
        }

        background.color = normalColor;
    }
}
