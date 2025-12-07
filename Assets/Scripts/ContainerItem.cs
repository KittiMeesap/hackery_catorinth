using System.Collections.Generic;
using UnityEngine;

public class ContainerItem : MonoBehaviour
{
    public FoodItemSO containerType;
    public List<FoodItemSO> contents = new();

    public void AddIngredient(FoodItemSO ingredient)
    {
        contents.Add(ingredient);
    }
}
