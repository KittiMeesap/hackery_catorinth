using UnityEngine;

public class PantryInteract : MonoBehaviour, IInteractable
{
    public FoodItemSO[] allIngredients;
    public Transform uiAnchor;

    public void Interact(PlayerInventory player)
    {
        if (!player.HasItem || !player.currentItem.isContainer)
        {
            Debug.Log("You didn't have a bowl!");
            return;
        }

        PantryUI.Instance.Open(allIngredients, player);
    }

    public Transform GetUIAnchor()
    {
        return uiAnchor;
    }
}
