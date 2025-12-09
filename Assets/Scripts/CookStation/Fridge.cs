using UnityEngine;

public class Fridge : InteractStation
{
    [Header("Animation")]
    public Animator animator;
    private static readonly int Open = Animator.StringToHash("Open");
    private static readonly int Close = Animator.StringToHash("Close");

    [Header("UI")]
    public CoolingTimerUI timerUI;
    public FridgeIconUI iconUI;

    private ContainerData coolingBowl = null;
    private bool isCooling = false;

    protected override void Start()
    {
        base.Start();
        timerUI.Hide();
        iconUI.Clear();
    }

    private void Update()
    {
        if (coolingBowl == null) return;
        if (!isCooling) return;

        var recipe = coolingBowl.matchedRecipe;
        coolingBowl.currentCoolingTime += Time.deltaTime;

        float progress = coolingBowl.currentCoolingTime / recipe.coolingDuration;
        timerUI.UpdateUI(progress, recipe.coolingDuration - coolingBowl.currentCoolingTime);

        if (coolingBowl.currentCoolingTime >= recipe.coolingDuration)
        {
            CompleteCooling();
        }
    }

    // PLAYER NEAR — fridge opens — STOP COOLING
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (!collision.CompareTag("Player")) return;

        animator.SetTrigger(Open);

        isCooling = false; // stop cooling when opened
    }

    // PLAYER LEAVES — fridge closes — START COOLING IF POSSIBLE
    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);
        if (!collision.CompareTag("Player")) return;

        animator.SetTrigger(Close);

        if (coolingBowl == null) return;

        if (coolingBowl.state == ContainerData.ContainerState.Mixed ||
            coolingBowl.state == ContainerData.ContainerState.Baked)
        {
            StartCooling();
        }
    }

    public override void Interact(PlayerInventory player)
    {
        if (coolingBowl != null && coolingBowl.IsFullyFinished())
        {
            TakeCooledItem(player);
            return;
        }

        if (coolingBowl != null)
        {
            Debug.Log("Fridge: Already cooling something.");
            return;
        }

        if (!player.HasBowl())
        {
            Debug.Log("Fridge: Need a bowl.");
            return;
        }

        var bowl = player.bowl;

        if (!bowl.CanCool())
        {
            Debug.Log("Fridge: This recipe cannot be cooled now.");
            return;
        }

        coolingBowl = player.TakeBowl();
        coolingBowl.currentCoolingTime = 0f;

        timerUI.Show();
        timerUI.UpdateUI(0f, coolingBowl.matchedRecipe.coolingDuration);

        iconUI.Refresh(coolingBowl);

        Debug.Log("Fridge: Bowl placed inside. Waiting for player to step back...");
    }

    private void StartCooling()
    {
        isCooling = true;
        Debug.Log("Fridge: Cooling started.");
    }

    private void CompleteCooling()
    {
        isCooling = false;

        var recipe = coolingBowl.matchedRecipe;

        if (recipe.flow == ProcessFlow.CoolOnly)
            coolingBowl.state = ContainerData.ContainerState.Finished;
        else if (recipe.flow == ProcessFlow.BakeThenCool)
            coolingBowl.state = ContainerData.ContainerState.Finished;
        else if (recipe.flow == ProcessFlow.CoolThenBake)
            coolingBowl.state = ContainerData.ContainerState.Cooling;

        Debug.Log("Fridge: Cooling completed!");

        iconUI.Refresh(coolingBowl);
        timerUI.Hide();
    }

    private void TakeCooledItem(PlayerInventory player)
    {
        player.GiveBowl(coolingBowl);

        coolingBowl = null;
        isCooling = false;

        timerUI.Hide();
        iconUI.Clear();

        Debug.Log("Fridge: Player took cooled item.");
    }
}
