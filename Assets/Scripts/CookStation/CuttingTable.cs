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
        if (isRunningQTE) return;
        if (!player.HasBowl()) return;
        if (!player.bowl.CanSlice()) return;

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

        if (result == QTEResult.Success)
        {
            currentPlayer.bowl.DoSlice();
            currentPlayer.OnInventoryChanged?.Invoke();
        }
    }
}
