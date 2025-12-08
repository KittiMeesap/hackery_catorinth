using System.Collections.Generic;
using UnityEngine;

public class PantryDatabase : MonoBehaviour
{
    public static PantryDatabase Instance;

    [Header("Initial Ingredients (start unlocked)")]
    [SerializeField]
    private List<IngredientItemSO> initialIngredients = new();

    private List<IngredientItemSO> availableIngredients = new();

    private void Awake()
    {
        Instance = this;

        // Load starting ingredients
        availableIngredients = new List<IngredientItemSO>(initialIngredients);
    }

    public void AddIngredient(IngredientItemSO item)
    {
        if (!availableIngredients.Contains(item))
            availableIngredients.Add(item);
    }

    public List<IngredientItemSO> GetAllIngredients()
    {
        return new List<IngredientItemSO>(availableIngredients);
    }

    public void ResetIngredients()
    {
        availableIngredients = new List<IngredientItemSO>(initialIngredients);
    }
}
