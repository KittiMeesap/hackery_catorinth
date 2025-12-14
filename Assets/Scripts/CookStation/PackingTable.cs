using UnityEngine;

public class PackingTable : InteractStation
{
    [Header("QTE Settings")]
    public int arrowCount = 3;
    public float timePerKey = 1.2f;

    [Header("SFX Keys")]
    public string sfxPackingLoop = "SFX_Packing_Loop";
    public string sfxPackingSuccess = "SFX_Packing_Success";

    private PlayerInventory currentPlayer;
    private PlayerController currentController;
    private AudioSource loopSource;
    private bool isPacking;

    protected override void Awake()
    {
        base.Awake();
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.spatialBlend = 1f;
    }

    public override void Interact(PlayerInventory player)
    {
        // ---- spam guard ----
        if (isPacking)
        {
            NotificationUI.Instance?.Show(
                "Already packing!",
                NotifyType.Info
            );
            return;
        }

        // ---- no bowl ----
        if (!player.HasBowl())
        {
            NotificationUI.Instance?.Show(
                "You need a bowl first!",
                NotifyType.Warning
            );
            return;
        }

        var bowl = player.bowl;

        // ---- not finished dish ----
        if (bowl.state != ContainerData.ContainerState.Finished &&
            bowl.state != ContainerData.ContainerState.Sliced)
        {
            NotificationUI.Instance?.Show(
                "This dish is not ready to pack",
                NotifyType.Warning
            );
            return;
        }

        // ---- start packing ----
        isPacking = true;

        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        LockInteraction();
        currentController.DisableMovement();
        currentController.SetCooking(true);

        StartPackingLoop();

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartSequenceQTE(
            GenerateSequence(arrowCount),
            timePerKey
        );
    }

    private LogicalInput[] GenerateSequence(int count)
    {
        LogicalInput[] pool =
        {
            LogicalInput.Left,
            LogicalInput.Right,
            LogicalInput.Up,
            LogicalInput.Down
        };

        LogicalInput[] seq = new LogicalInput[count];
        for (int i = 0; i < count; i++)
            seq[i] = pool[Random.Range(0, pool.Length)];

        return seq;
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopPackingLoop();
        currentController.SetCooking(false);
        currentController.EnableMovement();
        UnlockInteraction();

        isPacking = false;

        // ---- FAIL ----
        if (result == QTEResult.Fail)
        {
            NotificationUI.Instance?.Show(
                "Packing failed!",
                NotifyType.Warning
            );
            return;
        }

        // ---- SUCCESS ----
        currentPlayer.ConvertToServeBox();

        ContainerData empty = new ContainerData
        {
            state = ContainerData.ContainerState.Empty
        };

        currentPlayer.ReturnBowlToLastCounter(empty);
        currentPlayer.OnInventoryChanged?.Invoke();

        AudioManager.Instance?.PlaySFXAt(
            sfxPackingSuccess,
            transform.position,
            true
        );
    }

    private void StartPackingLoop()
    {
        var clip = AudioManager.Instance?.GetClipByKey(sfxPackingLoop);
        if (clip == null) return;

        loopSource.clip = clip;
        loopSource.volume =
            AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        loopSource.Play();
    }

    private void StopPackingLoop()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }
}
