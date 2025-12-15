using System.Collections.Generic;
using UnityEngine;

public enum ProcessFlow
{
    None,           // No production process (used for sliced variants)

    BakeOnly,       // Mix -> Bake -> Finish
    CoolOnly,       // Mix -> Cool -> Finish
    CoolThenBake,   // Mix -> Cool -> Bake -> Finish
    BakeThenCool,   // Mix -> Bake -> Cool -> Finish
}


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

    [Header("Production Flow")]
    public ProcessFlow flow;

    [Header("Cooling Settings")]
    public float coolingDuration = 5f;

    // ===== MATCH CHECK (SUBSET) =====
    public bool MatchesSelected(List<IngredientItemSO> selected)
    {
        if (selected.Count > ingredients.Count) return false;

        List<IngredientItemSO> temp = new List<IngredientItemSO>(ingredients);

        foreach (var sel in selected)
        {
            if (!temp.Contains(sel)) return false;
            temp.Remove(sel);
        }
        return true;
    }

    // ===== MATCH CHECK (EXACT) =====
    public bool MatchIngredients(List<IngredientItemSO> contents)
    {
        if (contents.Count != ingredients.Count) return false;

        List<IngredientItemSO> temp = new List<IngredientItemSO>(ingredients);

        foreach (var c in contents)
        {
            if (!temp.Contains(c)) return false;
            temp.Remove(c);
        }
        return true;
    }
}
