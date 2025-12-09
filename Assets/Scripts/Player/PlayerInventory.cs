using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public ContainerData bowl;
    public ServeBoxItem serveBox;

    public Counter lastCounterPlaced;

    public System.Action OnInventoryChanged;

    private void Awake()
    {
        bowl = null;
        serveBox = null;
        lastCounterPlaced = null;
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

    public void ReturnBowlToLastCounter(ContainerData bowlData)
    {
        Counter target = null;

        if (lastCounterPlaced != null && lastCounterPlaced.bowlOnCounter == null)
        {
            target = lastCounterPlaced;
        }
        else
        {
            target = CounterUtility.FindBestCounterForReturn(transform.position);
        }

        if (target == null)
        {
            Debug.LogWarning("ReturnBowl: No available counter found!");
            return;
        }

        target.ReceiveReturnedBowl(bowlData);
    }

    public void PickCup()
    {
        bowl = new ContainerData();
        serveBox = null;

        GetComponent<PlayerController>()?.RefreshCarryAnimation();
        OnInventoryChanged?.Invoke();
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
        var pc = GetComponent<PlayerController>();
        if (pc) pc.RefreshCarryAnimation();
    }

}
