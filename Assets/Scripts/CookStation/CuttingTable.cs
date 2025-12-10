using UnityEngine;

public class CuttingTable : InteractStation
{
    [Header("Timing QTE Settings")]
    public float speed = 180f;
    public float zoneSize = 40f;

    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    private bool isRunningQTE = false;

    public override void Interact(PlayerInventory player)
    {
        // Prevent slicing while processing QTE
        if (isRunningQTE)
        {
            Debug.Log("Cutting: Already slicing.");
            return;
        }

        // Player must hold a bowl
        if (!player.HasBowl())
        {
            Debug.Log("Cutting: Need a bowl with finished or sliceable recipe.");
            return;
        }

        var bowl = player.bowl;

        if (bowl == null)
        {
            Debug.LogWarning("Cutting: Bowl data missing!");
            return;
        }

        // Prevent slicing for slice-variant recipes (flow == None)
        if (bowl.matchedRecipe != null && bowl.matchedRecipe.flow == ProcessFlow.None)
        {
            Debug.Log("Cutting: This item cannot be sliced (flow None).");
            return;
        }

        // Only allow slicing if recipe supports slicing
        if (!bowl.CanSlice())
        {
            Debug.Log("Cutting: Recipe does not allow slicing.");
            return;
        }

        // Prevent double slicing
        if (bowl.state == ContainerData.ContainerState.Sliced)
        {
            Debug.Log("Cutting: Item already sliced.");
            return;
        }

        // Start QTE
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

        if (result == QTEResult.Fail)
        {
            Debug.Log("Cutting failed.");
            return;
        }

        Debug.Log("Cutting success!");

        // Slice + switch recipe
        currentPlayer.bowl.DoSlice();

        // Refresh animation for sliced state
        currentPlayer.GetComponent<PlayerController>()?.RefreshCarryAnimation();

        currentPlayer.OnInventoryChanged?.Invoke();
    }
}
