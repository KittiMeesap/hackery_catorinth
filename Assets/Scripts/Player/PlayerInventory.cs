using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public FoodItemSO currentItem;
    public ContainerItem currentContainer;

    public System.Action OnInventoryChanged;

    public bool HasItem => currentItem != null;
    public bool HasContainer => currentContainer != null;

    public void Pick(FoodItemSO item)
    {
        currentItem = item;
        currentContainer = null;
        OnInventoryChanged?.Invoke();
    }

    public void PickContainer(ContainerItem container)
    {
        currentContainer = container;
        currentItem = container.containerType;
        OnInventoryChanged?.Invoke();
    }

    public void Drop()
    {
        currentItem = null;
        currentContainer = null;
        OnInventoryChanged?.Invoke();
    }
}
