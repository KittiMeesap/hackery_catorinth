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

    // ===== CHECKERS =====
    public bool HasBowl() => bowl != null;
    public bool HasServeBox() => serveBox != null;

    // ===== GIVE BOWL TO PLAYER =====
    public void GiveBowl(ContainerData data)
    {
        bowl = data;
        serveBox = null;

        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();
    }

    // ===== TAKE BOWL FROM PLAYER =====
    public ContainerData TakeBowl()
    {
        ContainerData temp = bowl;
        bowl = null;

        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();

        return temp;
    }

    // ===== RETURN BOWL TO COUNTER =====
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

    // ===== PICK EMPTY CUP =====
    public void PickCup()
    {
        bowl = new ContainerData();
        serveBox = null;

        GetComponent<PlayerController>()?.RefreshCarryAnimation();
        OnInventoryChanged?.Invoke();
    }

    // ===== CONVERT BOWL TO SERVE BOX (WHOLE OR SLICED) =====
    public void ConvertToServeBox()
    {
        if (bowl == null || bowl.matchedRecipe == null)
            return;

        RecipeSO finalRecipe = bowl.matchedRecipe;

        if (bowl.state == ContainerData.ContainerState.Sliced)
        {
            if (bowl.matchedRecipe.slicedVariant != null)
            {
                finalRecipe = bowl.matchedRecipe.slicedVariant;
            }
            else
            {
                Debug.LogWarning(
                    $"ConvertToServeBox: Container is Sliced but recipe '{bowl.matchedRecipe.name}' has no slicedVariant assigned. Using base recipe."
                );
            }
        }

        serveBox = new ServeBoxItem
        {
            resultRecipe = finalRecipe
        };

        bowl = null;

        OnInventoryChanged?.Invoke();

        var pc = GetComponent<PlayerController>();
        if (pc)
            pc.RefreshCarryAnimation();
    }
}
