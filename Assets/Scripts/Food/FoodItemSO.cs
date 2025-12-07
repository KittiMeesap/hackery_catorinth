using UnityEngine;

[CreateAssetMenu(menuName = "Food/Ingredient")]
public class FoodItemSO : ScriptableObject
{
    public string id;
    public Sprite icon;
    public bool isContainer = false;
}
