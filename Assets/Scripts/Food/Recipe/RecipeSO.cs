using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Food/Recipe")]
public class RecipeSO : ScriptableObject
{
    public string recipeId;

    [Header("Required ingredients (exact count)")]
    public List<FoodItemSO> ingredients;

    /// <summary>
    /// Checks whether the selected ingredients can still form this recipe.
    /// </summary>
    public bool MatchesSelected(List<FoodItemSO> selected)
    {
        // If selected exceeds recipe count impossible
        if (selected.Count > ingredients.Count)
            return false;

        // Make a local copy so we can remove ingredients as we match them
        List<FoodItemSO> temp = new List<FoodItemSO>(ingredients);

        foreach (var sel in selected)
        {
            if (!temp.Contains(sel))
                return false;

            temp.Remove(sel); // remove matched ingredient
        }

        return true; // all selected ingredients match the recipe
    }
}
