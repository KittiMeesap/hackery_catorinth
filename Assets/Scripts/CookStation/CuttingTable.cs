using UnityEngine;

public class CuttingTable : InteractStation
{
    [Header("Timing QTE Settings")]
    public float speed = 180f;
    public float zoneSize = 40f;

    private PlayerInventory currentPlayer;
    private PlayerController currentController;
    private bool isRunningQTE;

    public override void Interact(PlayerInventory player)
    {
        //  spam guard 
        if (isRunningQTE)
        {
            NotificationUI.Instance?.Show(
                "Already cutting!",
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

        //  cannot slice yet 
        if (!player.bowl.CanSlice())
        {
            NotificationUI.Instance?.Show(
                "This dish can't be sliced yet",
                NotifyType.Warning
            );
            return;
        }

        //  start cutting 
        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();
        isRunningQTE = true;

        LockInteraction();
        currentController.DisableMovement();
        currentController.SetCooking(true);

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartTimingQTE(speed, zoneSize);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;
        isRunningQTE = false;

        currentController.SetCooking(false);
        currentController.EnableMovement();
        UnlockInteraction();

        //  FAIL 
        if (result == QTEResult.Fail)
        {
            NotificationUI.Instance?.Show(
                "Cutting failed!",
                NotifyType.Warning
            );
            return;
        }

        //  SUCCESS
        if (result == QTEResult.Success)
        {
            currentPlayer.bowl.DoSlice();
            currentPlayer.OnInventoryChanged?.Invoke();
        }
    }
}
