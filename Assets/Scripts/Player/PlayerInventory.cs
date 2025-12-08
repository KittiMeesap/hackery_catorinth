using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public ContainerData bowl;
    public ServeBoxItem serveBox;

    public bool HasContainer => bowl != null;
    public bool HasServeBox => serveBox != null;

    public System.Action OnInventoryChanged;

    public void PickCup()
    {
        bowl = new ContainerData();
        serveBox = null;
        OnInventoryChanged?.Invoke();
    }

    public void DropAll()
    {
        bowl = null;
        serveBox = null;
        OnInventoryChanged?.Invoke();
    }

    public void ConvertToServeBox()
    {
        if (bowl == null || bowl.matchedRecipe == null) return;

        serveBox = new ServeBoxItem
        {
            resultRecipe = bowl.matchedRecipe
        };

        bowl = null;
        OnInventoryChanged?.Invoke();
    }
}
