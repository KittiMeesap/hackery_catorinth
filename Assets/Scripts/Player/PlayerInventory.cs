using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public ContainerData bowl;
    public ServeBoxItem serveBox;

    public System.Action OnInventoryChanged;

    private void Awake()
    {
        bowl = null;
        serveBox = null;
    }

    public bool HasBowl() => bowl != null;
    public bool HasServeBox() => serveBox != null;

    public void GiveBowl(ContainerData data)
    {
        bowl = data;
        serveBox = null;
        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();
    }

    public ContainerData TakeBowl()
    {
        ContainerData temp = bowl;
        bowl = null;
        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();
        return temp;
    }

    public void PickCup()
    {
        bowl = new ContainerData();
        serveBox = null;
        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();
    }

    public void ConvertToServeBox()
    {
        if (bowl == null || bowl.matchedRecipe == null)
            return;

        serveBox = new ServeBoxItem
        {
            resultRecipe = bowl.matchedRecipe
        };

        bowl = null;
        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();
    }
}
