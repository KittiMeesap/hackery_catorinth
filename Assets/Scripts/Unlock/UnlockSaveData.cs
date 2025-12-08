using System;
using System.Collections.Generic;

[Serializable]
public class UnlockSaveData
{
    public int lastUnlockedDay = 1;

    public List<string> unlockedIngredients = new List<string>();
    public List<string> unlockedRecipes = new List<string>();
    public List<string> unlockedAppliances = new List<string>();
}
