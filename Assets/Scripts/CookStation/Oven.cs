using UnityEngine;

public class Oven : InteractStation
{
    public Animator animator;

    private static readonly int Open = Animator.StringToHash("Open");
    private static readonly int Close = Animator.StringToHash("Close");
    private static readonly int IsBaking = Animator.StringToHash("IsBaking");

    [Header("QTE Settings")]
    public int arrowCount = 4;
    public float timePerKey = 1.2f;

    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (other.CompareTag("Player"))
            animator.SetTrigger(Open);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
        if (other.CompareTag("Player"))
            animator.SetTrigger(Close);
    }

    public override void Interact(PlayerInventory player)
    {
        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        if (!player.HasBowl())
        {
            Debug.Log("Oven: Need bowl.");
            return;
        }

        var bowl = player.bowl;

        if (bowl.matchedRecipe == null)
        {
            Debug.Log("Oven: Must mix first.");
            return;
        }

        if (!bowl.CanBake())
        {
            Debug.Log("Oven: Cannot bake at this state.");
            return;
        }

        animator.SetBool(IsBaking, true);

        currentController.SetCooking(true);
        currentController.DisableMovement();
        LockInteraction();

        StartQTE();
    }

    private void StartQTE()
    {
        string[] seq = GenerateRandomArrowSequence(arrowCount);

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartSequenceQTE(seq, timePerKey);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        currentController.SetCooking(false);
        animator.SetBool(IsBaking, false);

        if (result == QTEResult.Success)
        {
            currentPlayer.bowl.DoBake();
            currentPlayer.OnInventoryChanged?.Invoke();
        }

        currentController.EnableMovement();
        UnlockInteraction();
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
