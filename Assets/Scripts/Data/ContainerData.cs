using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ContainerData
{
    public List<IngredientItemSO> contents = new();
    public RecipeSO matchedRecipe;

    public enum ContainerState { Empty, Mixing, Mixed, Baked, Cooling, Finished }
    public ContainerState state = ContainerState.Empty;

    public void AddIngredient(IngredientItemSO ing)
    {
        contents.Add(ing);
        state = ContainerState.Mixing;
    }

    public bool TryMix()
    {
        matchedRecipe = RecipeManager.Instance.GetRecipeFromIngredients(contents);
        if (matchedRecipe == null) return false;

        state = ContainerState.Mixed;
        return true;
    }

    public void Bake()
    {
        if (matchedRecipe == null) return;

        state = matchedRecipe.requiresCooling ? ContainerState.Cooling : ContainerState.Finished;
    }

    public Sprite GetIcon()
    {
        if (matchedRecipe == null) return null;

        return state switch
        {
            ContainerState.Mixed => matchedRecipe.mixedIcon,
            ContainerState.Cooling => matchedRecipe.cooledIcon,
            ContainerState.Finished => matchedRecipe.outputIcon,
            _ => null
        };
    }

    public void Clear()
    {
        contents.Clear();
        matchedRecipe = null;
        state = ContainerState.Empty;
    }
}
