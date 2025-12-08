using UnityEngine;

public class PantryInteract : MonoBehaviour, IInteractable
{
    public Transform uiAnchor;

    public void Interact(PlayerInventory player)
    {
        if (!player.HasContainer)
        {
            Debug.Log("You need a bowl before selecting ingredients!");
            return;
        }

        var allItems = PantryDatabase.Instance.GetAllIngredients();
        PantryUI.Instance.Open(allItems.ToArray(), player);
    }

    public Transform GetUIAnchor()
    {
        return uiAnchor;
    }
}
