using System.Collections.Generic;
using UnityEngine;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager Instance;

    [Header("Unlocked recipes")]
    public List<RecipeSO> recipes = new();

    private void Awake()
    {
        Instance = this;
    }

    public void AddRecipe(RecipeSO recipe)
    {
        if (!recipes.Contains(recipe))
            recipes.Add(recipe);
    }

    public RecipeSO GetRecipeFromIngredients(List<IngredientItemSO> selected)
    {
        foreach (var recipe in recipes)
        {
            if (recipe.MatchesSelected(selected))
                return recipe;
        }
        return null;
    }

    public List<IngredientItemSO> GetAllowedIngredients(List<IngredientItemSO> selected)
    {
        List<IngredientItemSO> allowed = new();

        foreach (var recipe in recipes)
        {
            if (recipe.MatchesSelected(selected))
            {
                foreach (var ing in recipe.ingredients)
                {
                    if (!allowed.Contains(ing))
                        allowed.Add(ing);
                }
            }
        }

        return allowed;
    }

    public bool CanMix(List<IngredientItemSO> contents)
    {
        foreach (var recipe in recipes)
        {
            if (recipe.MatchIngredients(contents))
                return true;
        }
        return false;
    }
}
