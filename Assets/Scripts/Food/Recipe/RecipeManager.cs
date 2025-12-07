using System.Collections.Generic;
using UnityEngine;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager Instance;

    [Header("All possible recipes")]
    public List<RecipeSO> recipes;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Returns all ingredients still possible based on selected ones.
    /// </summary>
    public List<FoodItemSO> GetAllowedIngredients(List<FoodItemSO> selected)
    {
        HashSet<FoodItemSO> allowed = new HashSet<FoodItemSO>();

        foreach (var recipe in recipes)
        {
            if (!recipe.MatchesSelected(selected))
                continue;

            foreach (var ing in recipe.ingredients)
                allowed.Add(ing);
        }

        return new List<FoodItemSO>(allowed);
    }
}
