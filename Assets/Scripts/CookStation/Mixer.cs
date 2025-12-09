using UnityEngine;

public class Mixer : InteractStation
{
    [Header("Settings")]
    public string mashKey = "Q";

    private bool isMixing = false;
    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    public override void Interact(PlayerInventory player)
    {
        if (isMixing) return;

        if (!player.HasBowl())
        {
            Debug.Log("Mixer: Need a bowl.");
            return;
        }

        var bowl = player.bowl;

        if (bowl.contents.Count == 0)
        {
            Debug.Log("Mixer: Bowl is empty.");
            return;
        }

        if (bowl.IsAlreadyMixed())
        {
            Debug.Log("Mixer: Already mixed, cannot mix again.");
            return;
        }

        if (!RecipeManager.Instance.CanMix(bowl.contents))
        {
            Debug.Log("Mixer: Invalid recipe.");
            return;
        }

        LockInteraction();
        isMixing = true;

        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        currentController.DisableMovement();
        currentController.SetCooking(true);

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartMashQTE(mashKey);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        currentController.EnableMovement();
        currentController.SetCooking(false);

        if (result == QTEResult.Success)
        {
            var bowl = currentPlayer.bowl;
            if (bowl.TryMix())
            {
                currentPlayer.OnInventoryChanged?.Invoke();
                Debug.Log("Mixer: Mix success");
            }
        }
        else
        {
            Debug.Log("Mixer: QTE failed");
        }

        isMixing = false;
        UnlockInteraction();
    }
}
