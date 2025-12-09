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

    private bool isPlayerNear = false;
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

    // Player enters trigger area -> fridge opens -> STOP cooling
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (!collision.CompareTag("Player")) return;

        isPlayerNear = true;
        animator.SetTrigger(Open);

        isCooling = false;
    }

    // Player leaves -> fridge closes -> START cooling
    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);
        if (!collision.CompareTag("Player")) return;

        isPlayerNear = false;
        animator.SetTrigger(Close);

        if (coolingBowl != null && coolingBowl.state == ContainerData.ContainerState.Mixed
            || coolingBowl.state == ContainerData.ContainerState.Baked)
        {
            StartCooling();
        }
    }

    // Player presses Interact
    public override void Interact(PlayerInventory player)
    {
        // CASE 1 — Player wants to TAKE finished item
        if (coolingBowl != null && coolingBowl.IsFullyFinished())
        {
            TakeCooledItem(player);
            return;
        }

        // CASE 2 — Already cooling something
        if (coolingBowl != null)
        {
            Debug.Log("Fridge: Already cooling something.");
            return;
        }

        // CASE 3 — Player has no bowl
        if (!player.HasBowl())
        {
            Debug.Log("Fridge: Need a bowl.");
            return;
        }

        var bowl = player.bowl;

        if (!bowl.CanCool())
        {
            Debug.Log("Fridge: This recipe cannot be cooled right now.");
            return;
        }

        // PLAYER PUTS BOWL INTO FRIDGE
        coolingBowl = player.TakeBowl();
        coolingBowl.currentCoolingTime = 0f;

        timerUI.Show();
        timerUI.UpdateUI(0, coolingBowl.matchedRecipe.coolingDuration);

        // show icon of food inside fridge
        iconUI.Refresh(coolingBowl);

        Debug.Log("Fridge: Bowl placed inside. Waiting for player to step back...");
    }

    // Start cooling
    private void StartCooling()
    {
        isCooling = true;
        Debug.Log("Fridge: Cooling started.");
    }

    // Cooling completed
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

        // update UI icon to cooled icon
        iconUI.Refresh(coolingBowl);

        timerUI.Hide();
    }

    // Player TAKES item out after cooling
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
