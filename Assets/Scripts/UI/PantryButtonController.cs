using UnityEngine;
using UnityEngine.UI;

public class PantryButtonController : MonoBehaviour
{
    [Header("UI")]
    public Image background;
    public Image ingredientIconImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.7f, 0.85f, 1f);
    public Color selectedColor = new Color(0.6f, 1f, 0.6f);
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);

    private IngredientItemSO ingredient;

    private bool isLocked = false;
    private bool isSelected = false;

    public IngredientItemSO Ingredient => ingredient;
    public bool IsLocked() => isLocked;

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
    }

    public void SetHighlight(bool active)
    {
        if (isLocked || isSelected) return;
        background.color = active ? highlightColor : normalColor;
    }

    public void SetSelected(bool active)
    {
        isSelected = active;
        background.color = active ? selectedColor : normalColor;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        background.color = locked
            ? lockedColor
            : (isSelected ? selectedColor : normalColor);
    }
}
