using UnityEngine;

public class PackingTable : InteractStation
{
    [Header("QTE Settings")]
    public int arrowCount = 3;
    public float timePerKey = 1.2f;

    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    public override void Interact(PlayerInventory player)
    {
        if (!player.HasBowl())
        {
            Debug.Log("Packing: Need bowl with finished cookie.");
            return;
        }

        var bowl = player.bowl;

        if (bowl.state != ContainerData.ContainerState.Finished)
        {
            Debug.Log("Packing: Cookie not ready.");
            return;
        }

        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        LockInteraction();
        currentController.DisableMovement();
        currentController.SetCooking(true);

        StartSequenceQTE();
    }

    private void StartSequenceQTE()
    {
        string[] seq = GenerateRandomArrowSequence(arrowCount);

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartSequenceQTE(seq, timePerKey);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        currentController.SetCooking(false);
        currentController.EnableMovement();
        UnlockInteraction();

        if (result == QTEResult.Fail)
        {
            Debug.Log("Packing failed.");
            return;
        }

        Debug.Log("Packing success!");

        currentPlayer.ConvertToServeBox();

        ContainerData empty = new ContainerData();
        empty.state = ContainerData.ContainerState.Empty;

        currentPlayer.ReturnBowlToLastCounter(empty);

        currentPlayer.OnInventoryChanged?.Invoke();
    }

    private string[] GenerateRandomArrowSequence(int count)
    {
        string[] pool = { "left", "right", "up", "down" };
        string[] seq = new string[count];

        for (int i = 0; i < count; i++)
            seq[i] = pool[Random.Range(0, pool.Length)];

        return seq;
    }
}
