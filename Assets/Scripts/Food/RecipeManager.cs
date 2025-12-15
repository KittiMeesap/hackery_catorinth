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

    // ===== ADD RECIPE =====
    public void AddRecipe(RecipeSO recipe)
    {
        if (!recipes.Contains(recipe))
            recipes.Add(recipe);
    }

    public void ResetRecipes()
    {
        recipes.Clear();
    }

    // ===== FIND RECIPE FROM INGREDIENTS (PREFER BASE/SLICEABLE) =====
    public RecipeSO GetRecipeFromIngredients(List<IngredientItemSO> selected)
    {
        RecipeSO best = null;

        foreach (var recipe in recipes)
        {
            if (!recipe.MatchesSelected(selected))
                continue;

            if (best == null)
            {
                best = recipe;
            }
        }

        return best;
    }

    // ===== ALLOWED INGREDIENTS HELPER =====
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

    // ===== EXACT MATCH CHECK =====
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
