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
        timerUI.UpdateUI(
            progress,
            recipe.coolingDuration - coolingBowl.currentCoolingTime
        );

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
        isCooling = false;
    }

    // PLAYER LEAVES — fridge closes — START COOLING
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

    // INTERACT
    public override void Interact(PlayerInventory player)
    {
        // ---- take finished item ----
        if (coolingBowl != null && coolingBowl.IsFullyFinished())
        {
            TakeCooledItem(player);
            return;
        }

        //  fridge already occupied 
        if (coolingBowl != null)
        {
            NotificationUI.Instance?.Show(
                "The fridge is already in use",
                NotifyType.Info
            );
            return;
        }

        //  no bowl 
        if (!player.HasBowl())
        {
            NotificationUI.Instance?.Show(
                "You need a bowl first!",
                NotifyType.Warning
            );
            return;
        }

        var bowl = player.bowl;

        //  cannot cool yet 
        if (!bowl.CanCool())
        {
            NotificationUI.Instance?.Show(
                "This dish can't be cooled yet",
                NotifyType.Warning
            );
            return;
        }

        //  place bowl in fridge 
        coolingBowl = player.TakeBowl();
        coolingBowl.currentCoolingTime = 0f;

        timerUI.Show();
        timerUI.UpdateUI(0f, coolingBowl.matchedRecipe.coolingDuration);

        iconUI.Refresh(coolingBowl);

        NotificationUI.Instance?.Show(
            "Step back to start cooling",
            NotifyType.Info
        );
    }

    // COOLING FLOW
    private void StartCooling()
    {
        isCooling = true;
    }

    private void CompleteCooling()
    {
        isCooling = false;

        var recipe = coolingBowl.matchedRecipe;

        if (recipe.flow == ProcessFlow.CoolOnly ||
            recipe.flow == ProcessFlow.BakeThenCool)
        {
            coolingBowl.state = ContainerData.ContainerState.Finished;
        }
        else if (recipe.flow == ProcessFlow.CoolThenBake)
        {
            coolingBowl.state = ContainerData.ContainerState.Cooling;
        }

        iconUI.Refresh(coolingBowl);
        timerUI.Hide();
    }

    // TAKE ITEM
    private void TakeCooledItem(PlayerInventory player)
    {
        // optional hint: taking too early
        if (!coolingBowl.IsFullyFinished())
        {
            NotificationUI.Instance?.Show(
                "Cooling is not finished yet",
                NotifyType.Info
            );
            return;
        }

        player.GiveBowl(coolingBowl);

        coolingBowl = null;
        isCooling = false;

        timerUI.Hide();
        iconUI.Clear();
    }
    private void OnDisable()
    {
        isCooling = false;
    }

}
