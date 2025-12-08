using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Food/Recipe")]
public class RecipeSO : ScriptableObject
{
    public string recipeId;

    [Header("Required ingredients")]
    public List<IngredientItemSO> ingredients;

    [Header("Icons")]
    public Sprite mixedIcon;
    public Sprite outputIcon;
    public Sprite cooledIcon;

    [Header("Extra Settings")]
    public bool requiresCooling;

    public bool MatchesSelected(List<IngredientItemSO> selected)
    {
        if (selected.Count > ingredients.Count)
            return false;

        List<IngredientItemSO> temp = new List<IngredientItemSO>(ingredients);

        foreach (var sel in selected)
        {
            if (!temp.Contains(sel))
                return false;

            temp.Remove(sel);
        }

        return true;
    }
}
