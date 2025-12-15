using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ContainerData
{
    public List<IngredientItemSO> contents = new();
    public RecipeSO matchedRecipe;

    public enum ContainerState
    {
        Empty,
        Mixing,
        Mixed,
        Cooling,
        Baked,
        Finished
    }

    public float currentCoolingTime = 0f;

    public ContainerState state = ContainerState.Empty;

    // ===== STATE HELPERS =====
    public bool IsAlreadyMixed()
    {
        return state == ContainerState.Mixed ||
               state == ContainerState.Cooling ||
               state == ContainerState.Baked ||
               state == ContainerState.Finished;
    }

    public bool IsFullyFinished()
    {
        return state == ContainerState.Finished;
    }

    public bool IsProcessed()
    {
        return state == ContainerState.Mixed ||
               state == ContainerState.Baked ||
               state == ContainerState.Cooling ||
               state == ContainerState.Finished;
    }


    // ===== BAKE LOGIC =====
    public bool CanBake()
    {
        if (matchedRecipe == null) return false;

        switch (matchedRecipe.flow)
        {
            case ProcessFlow.BakeOnly:
            case ProcessFlow.BakeThenCool:
                return state == ContainerState.Mixed;

            case ProcessFlow.CoolThenBake:
                return state == ContainerState.Cooling;

            default:
                return false;
        }
    }

    public void DoBake()
    {
        if (!CanBake()) return;

        switch (matchedRecipe.flow)
        {
            case ProcessFlow.BakeOnly:
                state = ContainerState.Finished;
                break;

            case ProcessFlow.BakeThenCool:
                state = ContainerState.Baked;
                break;

            case ProcessFlow.CoolThenBake:
                state = ContainerState.Finished;
                break;
        }
    }

    // ===== COOL LOGIC =====
    public bool CanCool()
    {
        if (matchedRecipe == null) return false;

        switch (matchedRecipe.flow)
        {
            case ProcessFlow.CoolOnly:
            case ProcessFlow.CoolThenBake:
                return state == ContainerState.Mixed;

            case ProcessFlow.BakeThenCool:
                return state == ContainerState.Baked;

            default:
                return false;
        }
    }

    public void DoCool()
    {
        if (!CanCool()) return;

        switch (matchedRecipe.flow)
        {
            case ProcessFlow.CoolOnly:
                state = ContainerState.Finished;
                break;

            case ProcessFlow.CoolThenBake:
                state = ContainerState.Cooling;
                break;

            case ProcessFlow.BakeThenCool:
                state = ContainerState.Finished;
                break;
        }
    }

    public bool IsCoolingCompleted(RecipeSO recipe)
    {
        return currentCoolingTime >= recipe.coolingDuration;
    }

    // ===== INGREDIENT / MIX =====
    public bool AddIngredientSafe(IngredientItemSO ing)
    {
        if (contents.Count >= 4) return false;

        var allowed = RecipeManager.Instance.GetAllowedIngredients(contents);
        if (allowed.Count > 0 && !allowed.Contains(ing))
            return false;

        contents.Add(ing);
        state = ContainerState.Mixing;
        return true;
    }

    public bool TryMix()
    {
        matchedRecipe = RecipeManager.Instance.GetRecipeFromIngredients(contents);
        if (matchedRecipe == null) return false;

        state = ContainerState.Mixed;
        return true;
    }

    // ===== ICON =====
    public Sprite GetIcon()
    {
        if (matchedRecipe == null) return null;

        return state switch
        {
            ContainerState.Mixed => matchedRecipe.mixedIcon,
            ContainerState.Cooling => matchedRecipe.cooledIcon,
            ContainerState.Baked => matchedRecipe.outputIcon,
            ContainerState.Finished => matchedRecipe.outputIcon,

            _ => null
        };
    }

    // ===== RESET =====
    public void Clear()
    {
        contents.Clear();
        matchedRecipe = null;
        currentCoolingTime = 0f;
        state = ContainerState.Empty;
    }
}
