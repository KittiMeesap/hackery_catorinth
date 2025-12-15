using UnityEngine;

public class Fridge : InteractStation
{
    [Header("Animation")]
    public Animator animator;
    private static readonly int Open = Animator.StringToHash("Open");
    private static readonly int Close = Animator.StringToHash("Close");

    private ContainerData currentBowl;
    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    // INTERACT
    public override void Interact(PlayerInventory player)
    {
        // NO BOWL
        if (!player.HasBowl())
        {
            NotificationUI.Instance?.Show(
                "You need a bowl first!",
                NotifyType.Warning
            );
            return;
        }

        var bowl = player.bowl;

        // NOT READY TO COOL
        if (!bowl.CanCool())
        {
            NotificationUI.Instance?.Show(
                "This dish can't be cooled yet",
                NotifyType.Warning
            );
            return;
        }

        // MOVE BOWL INTO FRIDGE
        currentBowl = player.TakeBowl();
        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        // LOCK PLAYER
        LockInteraction();
        currentController.DisableMovement();
        currentController.SetCooking(true);

        animator.SetTrigger(Open);

        // START MASH QTE
        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartMashQTE();
    }

    // QTE RESULT
    private void OnQTEFinished(QTEResult result)
    {
        if (QTEManager.Instance != null)
            QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        animator.SetTrigger(Close);

        currentController.EnableMovement();
        currentController.SetCooking(false);
        UnlockInteraction();

        // FAIL / CANCEL
        if (result != QTEResult.Success)
        {
            currentPlayer.GiveBowl(currentBowl);
            currentBowl = null;

            NotificationUI.Instance?.Show(
                "Cooling failed",
                NotifyType.Warning
            );
            return;
        }

        // SUCCESS
        ApplyCoolingResult();

        currentPlayer.GiveBowl(currentBowl);
        currentBowl = null;

        NotificationUI.Instance?.Show(
            "Cooling complete!",
            NotifyType.Info
        );
    }

    // APPLY RESULT
    private void ApplyCoolingResult()
    {
        var recipe = currentBowl.matchedRecipe;
        if (recipe == null) return;

        switch (recipe.flow)
        {
            case ProcessFlow.CoolOnly:
            case ProcessFlow.BakeThenCool:
                currentBowl.state = ContainerData.ContainerState.Finished;
                break;

            case ProcessFlow.CoolThenBake:
                currentBowl.state = ContainerData.ContainerState.Cooling;
                break;
        }
    }

    private void OnDisable()
    {
        if (QTEManager.Instance != null)
            QTEManager.Instance.OnQTEFinished -= OnQTEFinished;
    }
}
