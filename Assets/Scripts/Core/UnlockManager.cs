using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance;

    public UnlockDatabaseSO database;
    public PantryDatabase pantryDatabase;
    public RecipeManager recipeManager;

    private UnlockableAppliance[] allAppliances;

    private void Awake()
    {
        Instance = this;

        allAppliances = FindObjectsByType<UnlockableAppliance>(FindObjectsSortMode.None);
    }

    public void ApplyUnlocksForDay(int dayIndex)
    {
        var save = UnlockSaveManager.Instance.Data;

        if (dayIndex <= save.lastUnlockedDay)
        {
            Debug.Log("Unlocks for this day already applied.");
            return;
        }

        foreach (var dayData in database.dayUnlockTable)
        {
            if (dayData.dayIndex != dayIndex)
                continue;

            foreach (var unlock in dayData.unlocks)
                ApplyUnlock(unlock);
        }

        save.lastUnlockedDay = dayIndex;
        UnlockSaveManager.Instance.Save();
    }

    private void ApplyUnlock(UnlockEntrySO unlock)
    {
        var save = UnlockSaveManager.Instance.Data;

        switch (unlock.type)
        {
            case UnlockableItemType.Ingredient:
                if (unlock.ingredient != null && !save.unlockedIngredients.Contains(unlock.ingredient.id))
                {
                    save.unlockedIngredients.Add(unlock.ingredient.id);
                    pantryDatabase.AddIngredient(unlock.ingredient);
                }
                break;

            case UnlockableItemType.Recipe:
                if (unlock.recipe != null && !save.unlockedRecipes.Contains(unlock.recipe.recipeId))
                {
                    save.unlockedRecipes.Add(unlock.recipe.recipeId);
                    recipeManager.AddRecipe(unlock.recipe);
                }
                break;

            case UnlockableItemType.Appliance:
                UnlockAppliance(unlock.applianceID);
                save.unlockedAppliances.Add(unlock.applianceID);
                break;
        }

        UnlockSaveManager.Instance.Save();
    }

    private void UnlockAppliance(string id)
    {
        foreach (var a in allAppliances)
        {
            if (a.unlockID == id)
                a.Unlock();
        }
    }
}
