using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("SFX Keys")]
    public string sfxPickCup = "SFX_PickCup";
    public string sfxPickBowl = "SFX_PickBowl";
    public string sfxTakeBowl = "SFX_TakeBowl";
    public string sfxPlaceBowl = "SFX_PlaceBowl";
    public string sfxConvertServeBox = "SFX_ConvertServeBox";

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

        AudioManager.Instance.PlaySFX(sfxPickBowl);

        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();
    }

    public ContainerData TakeBowl()
    {
        ContainerData temp = bowl;
        bowl = null;

        AudioManager.Instance.PlaySFX(sfxTakeBowl);

        OnInventoryChanged?.Invoke();
        GetComponent<PlayerController>()?.RefreshCarryAnimation();

        return temp;
    }

    public void ReturnBowlToLastCounter(ContainerData bowlData)
    {
        Counter target = null;

        if (lastCounterPlaced != null && lastCounterPlaced.bowlOnCounter == null)
            target = lastCounterPlaced;
        else
            target = CounterUtility.FindBestCounterForReturn(transform.position);

        if (target == null)
        {
            Debug.LogWarning("ReturnBowl: No available counter found!");
            return;
        }

        AudioManager.Instance.PlaySFX(sfxPlaceBowl);

        target.ReceiveReturnedBowl(bowlData);
    }

    public void PickCup()
    {
        bowl = new ContainerData();
        serveBox = null;

        AudioManager.Instance.PlaySFX(sfxPickCup);

        GetComponent<PlayerController>()?.RefreshCarryAnimation();
        OnInventoryChanged?.Invoke();
    }

    public void ConvertToServeBox()
    {
        if (bowl == null || bowl.matchedRecipe == null)
            return;

        RecipeSO finalRecipe = bowl.matchedRecipe;

        if (bowl.state == ContainerData.ContainerState.Sliced)
        {
            if (bowl.matchedRecipe.slicedVariant != null)
                finalRecipe = bowl.matchedRecipe.slicedVariant;
        }

        serveBox = new ServeBoxItem
        {
            resultRecipe = finalRecipe
        };

        bowl = null;

        AudioManager.Instance.PlaySFX(sfxConvertServeBox);

        OnInventoryChanged?.Invoke();
        var pc = GetComponent<PlayerController>();
        if (pc) pc.RefreshCarryAnimation();
    }
}
