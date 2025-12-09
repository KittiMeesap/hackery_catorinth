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

    public bool IsAlreadyMixed()
    {
        return state == ContainerState.Mixed ||
               state == ContainerState.Cooling ||
               state == ContainerState.Baked ||
               state == ContainerState.Finished;
    }

    public bool CanBake()
    {
        if (matchedRecipe == null) return false;

        return matchedRecipe.flow switch
        {
            ProcessFlow.BakeOnly => state == ContainerState.Mixed,
            ProcessFlow.BakeThenCool => state == ContainerState.Mixed,
            ProcessFlow.CoolThenBake => state == ContainerState.Cooling,
            ProcessFlow.CoolOnly => false,
            _ => false
        };
    }

    public bool CanCool()
    {
        if (matchedRecipe == null) return false;

        return matchedRecipe.flow switch
        {
            ProcessFlow.CoolOnly => state == ContainerState.Mixed,
            ProcessFlow.CoolThenBake => state == ContainerState.Mixed,
            ProcessFlow.BakeThenCool => state == ContainerState.Baked,
            ProcessFlow.BakeOnly => false,
            _ => false
        };
    }

    public bool IsCoolingCompleted(RecipeSO recipe)
    {
        return currentCoolingTime >= recipe.coolingDuration;
    }

    public bool IsFullyFinished()
    {
        return state == ContainerState.Finished;
    }

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

    public void DoBake()
    {
        if (!CanBake()) return;

        if (matchedRecipe.flow == ProcessFlow.BakeThenCool)
            state = ContainerState.Baked;
        else if (matchedRecipe.flow == ProcessFlow.CoolThenBake)
            state = ContainerState.Finished;
        else if (matchedRecipe.flow == ProcessFlow.BakeOnly)
            state = ContainerState.Finished;
    }

    public void DoCool()
    {
        if (!CanCool()) return;

        if (matchedRecipe.flow == ProcessFlow.CoolOnly)
            state = ContainerState.Finished;
        else if (matchedRecipe.flow == ProcessFlow.CoolThenBake)
            state = ContainerState.Cooling;
        else if (matchedRecipe.flow == ProcessFlow.BakeThenCool)
            state = ContainerState.Finished;
    }

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

    public void Clear()
    {
        contents.Clear();
        matchedRecipe = null;
        state = ContainerState.Empty;
    }
}
