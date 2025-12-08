using UnityEngine;

[CreateAssetMenu(menuName = "Unlock/Unlock Entry")]
public class UnlockEntrySO : ScriptableObject
{
    public UnlockableItemType type;

    [Header("Ingredient / Recipe")]
    public IngredientItemSO ingredient;
    public RecipeSO recipe;

    [Header("Appliance Unlock ID (match UnlockableAppliance in scene)")]
    public string applianceID;
}
