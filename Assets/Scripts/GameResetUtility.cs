public static class GameResetUtility
{
    public static void ResetAll()
    {
        // RESET UNLOCK SAVE
        if (UnlockSaveManager.Instance != null)
        {
            UnlockSaveManager.Instance.Data.lastUnlockedDay = 1;
            UnlockSaveManager.Instance.Data.unlockedIngredients.Clear();
            UnlockSaveManager.Instance.Data.unlockedRecipes.Clear();
            UnlockSaveManager.Instance.Data.unlockedAppliances.Clear();
            UnlockSaveManager.Instance.Save();
        }

        // RESET PANTRY
        if (PantryDatabase.Instance != null)
            PantryDatabase.Instance.ResetIngredients();

        // RESET RECIPE
        if (RecipeManager.Instance != null)
            RecipeManager.Instance.ResetRecipes();

        // RESET DAY
        if (DayManager.Instance != null)
            DayManager.Instance.ResetToDayOne();
    }
}
